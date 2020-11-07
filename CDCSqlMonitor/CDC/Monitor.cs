/*
 MIT License

    Copyright (c) 2020 Peeter K. All rights reserved.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sub-license, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE
 */
using CDCSqlMonitor.CDC.Enums;
using CDCSqlMonitor.CDC.EventArgs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace CDCSqlMonitor.CDC
{
    public class Monitor
    {
        public event EventHandler<DataChangedEventArgs> OnRecordChnaged;
        public event EventHandler<ErrorEventArgs> OnError;
        public int PollIntervalSeconds;
        public byte[] LastSequenceValue;

        private string ConnectionString;
        private Thread Thread;
        private bool Running = false;

        public Monitor(string connectionString, int pollIntervalSeconds, byte[] lastSeq = null)
        {
            ConnectionString = connectionString;
            PollIntervalSeconds = pollIntervalSeconds;
            LastSequenceValue = lastSeq ?? new byte[10]{0x00, 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 };
        }

        /// <summary>
        /// Start monitoring. Starts separate thread with polling.
        /// </summary>
        public void Start()
        {
            Running = true;
            Thread = new Thread(() => {
                while (Running)
                {
                    try
                    {
                        if (OnRecordChnaged != null)
                        {
                            var args = new DataChangedEventArgs();
                            args.ChangedEntities = new List<Models.Entity>();

                            using (SqlConnection con = new SqlConnection(ConnectionString))
                            {
                                con.Open();

                                var TableObjects = new List<Tuple<long, string>>();
                                var sql = " SELECT ct.object_id, t.name from [cdc].[change_tables] ct join sys.tables t on ct.source_object_id = t.object_id";
                                using (SqlCommand command = new SqlCommand(sql, con)) 
                                {
                                    SqlDataReader reader = command.ExecuteReader();
                                    while (reader.Read()) 
                                    {
                                        long objectId = 0;
                                        string tableName = string.Empty;
                                        if (!reader.IsDBNull(0))
                                        {
                                            objectId = reader.GetInt32(0);
                                        }
                                        if (!reader.IsDBNull(1))
                                        {
                                            tableName = reader.GetString(1);                                               
                                        }
                                        if (objectId > 0 && !string.IsNullOrWhiteSpace(tableName)) 
                                        {
                                            TableObjects.Add(new Tuple<long, string>(objectId, tableName));
                                        }
                                    }
                                }


                                var resultList = new List<Models.CDCModel>();
                                foreach (var tableObject in TableObjects)
                                {
                                    sql = $@"
                                                declare @tablename nvarchar(max);
	                                            select @tablename = name from sys.tables where object_id = {tableObject.Item1};
	                                            declare @sql nvarchar(max) = 'select * from cdc.' + @tablename + ' WHERE __$seqval > 0x{ByteArrayToString(LastSequenceValue)} order by __$seqval desc'; 
                                                exec (@sql);
                                            ";
                                    using (SqlCommand command = new SqlCommand(sql, con))
                                    {
                                        SqlDataReader reader = command.ExecuteReader();
                                        while (reader.Read())
                                        {
                                            var ctc = new Models.CDCModel();
                                            ctc.TableName = tableObject.Item2;

                                            if (!reader.IsDBNull(0))
                                            {
                                                ctc.__start_Lsn = (byte[])reader[0];
                                            }
                                            if (!reader.IsDBNull(1))
                                            {
                                                ctc.__end_Lsn = (byte[])reader[1];
                                            }
                                            if (!reader.IsDBNull(2))
                                            {
                                                ctc.__seqval = (byte[])reader[2];
                                            }
                                            if (!reader.IsDBNull(3))
                                            {
                                                ctc.__operation = reader.GetInt32(3);
                                            }
                                            if (!reader.IsDBNull(4))
                                            {
                                                ctc.__update_mask = (short)((byte[])reader[4])[0];
                                            }


                                            ctc.Columns = new List<Models.EntityColumn>();                                           
                                            for (int i = 5; i < reader.FieldCount-1; i++)
                                            {
                                                var col = new Models.EntityColumn();
                                                col.Name = reader.GetName(i);
                                                if (!reader.IsDBNull(i))
                                                {
                                                    
                                                    col.Value = reader.GetValue(i);
                                                }
                                                ctc.Columns.Add(col);
                                            }

                                            if (!reader.IsDBNull(reader.FieldCount-1))
                                            {
                                                ctc.__command_id = reader.GetInt32(reader.FieldCount - 1);
                                            }


                                            resultList.Add(ctc);
                                        }

                                       
                                    }
                                }

                                args.RawData = resultList;

                                var keys = resultList.Select(x => x.__seqval).Distinct();
                                var skipchanges = new List<byte[]>();
                                foreach (var seqval in keys) 
                                {
                                    if (skipchanges.Any(x => AreEqual(seqval, x)))
                                        continue;
                                    Models.Entity entity = null;  

                                    var changes = resultList.Where(x => AreEqual(x.__seqval ,seqval));
                                    if (changes.Count() > 1)
                                    {
                                        //update changes comes with old and new values. 
                                        var newValue = changes.FirstOrDefault(x => x.__operation == (int)ChangeType.UPDATE_NEW_VALUE);
                                        var oldValue = changes.FirstOrDefault(x => x.__operation == (int)ChangeType.UPDATE_OLD_VALUE);

                                        entity = new Models.Entity();
                                        entity.TableName = newValue.TableName;
                                        entity.ChangeType = ChangeType.UPDATE_NEW_VALUE;
                                        entity.Columns = newValue.Columns;
                                        foreach (var col in entity.Columns) 
                                        {
                                            col.OldValue = oldValue.Columns.FirstOrDefault(x => x.Name == col.Name)?.Value;
                                        }
                                        skipchanges.Add(seqval);
                                    }
                                    else if(changes.Any()) 
                                    {
                                        var change = changes.FirstOrDefault();
                                        entity = new Models.Entity();
                                        entity.TableName = change.TableName;
                                        entity.ChangeType = (ChangeType)change.__operation;
                                        entity.Columns = change.Columns;
                                    }

                                    
                                    if(entity != null)
                                        args.ChangedEntities.Add(entity);
                                }
                            }


                            foreach (var data in args.RawData) 
                            {
                                // returns negative value, if LastSequenceValue < data.__seqval
                                var result = ((IStructuralComparable)LastSequenceValue).CompareTo(data.__seqval, Comparer<byte>.Default); 
                                if (result < 0)
                                    args.LastSequenceValue = LastSequenceValue = data.__seqval;
                            }

                            OnRecordChnaged(this, args);
                        }
                    }
                    catch (Exception e)
                    {
                        if (OnError != null)
                        {
                            var args = new ErrorEventArgs();
                            args.Exception = e;
                            OnError(this, args);
                        }
                    }
                    Thread.Sleep(PollIntervalSeconds * 1000);
                }

            });
            Thread.Start();
        }

        /// <summary>
        /// Stop monitoring. Stops thread and dispose it.
        /// </summary>
        public void Stop()
        {
            Dispose();
        }

        public void Dispose()
        {
            Running = false;
            try
            {
                Thread?.Abort();
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }

        }

        private bool AreEqual(byte[] arr1, byte[] arr2)
        {
            return ((IStructuralComparable)arr1).CompareTo(arr2, Comparer<byte>.Default) == 0;
        }

        private string ByteArrayToString(byte[] ba)
        {
            if (ba == null)
                return "00";
            System.Text.StringBuilder hex = new System.Text.StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            var result = hex.ToString();
            return result.ToUpper();
        }


    }
}
