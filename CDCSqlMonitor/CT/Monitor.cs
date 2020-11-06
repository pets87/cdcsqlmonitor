using CDCSqlMonitor.CT.Enums;
using CDCSqlMonitor.CT.EventArgs;
using CDCSqlMonitor.CT.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace CDCSqlMonitor.CT
{
    public class Monitor:IDisposable
    {
        
        public event EventHandler<DataChangedEventArgs> OnRecordChnaged;
        public event EventHandler<ErrorEventArgs> OnError;
        public int PollIntervalSeconds;
        public List<MonitorTable> MonitorTables;

        private string ConnectionString;
        private Thread Thread;
        private bool Running = false;
       
        public Monitor(string connectionString, int pollIntervalSeconds, List<MonitorTable> monitorTables = null) 
        {
            ConnectionString = connectionString;
            PollIntervalSeconds = pollIntervalSeconds;
            MonitorTables = monitorTables;
        }

        /// <summary>
        /// Add table that is need to be monitored
        /// </summary>
        /// <param name="tableName">Name of the table with schema</param>
        /// <param name="primaryKeyColumnName">Primary key column name. Example: ID or MyID</param>
        public void AddTable(string tableName, string primaryKeyColumnName) 
        {
            if (MonitorTables == null)
                MonitorTables = new List<MonitorTable>();

            var existing = MonitorTables.FirstOrDefault(x => x.TableName.ToLower() == tableName.ToLower());
            if (existing != null)
                return;

            var table = new MonitorTable();
            table.PrimaryKeyColumnName = primaryKeyColumnName;
            table.TableName = tableName;
            MonitorTables.Add(table);
        }

        public void RemoveTable(string tableName)
        {
            if (MonitorTables != null) 
            {
                var existing = MonitorTables.FirstOrDefault(x => x.TableName == tableName);
                if (existing != null)
                    MonitorTables.Remove(existing);
            }           
        }

        public void Setup() 
        {
            if (MonitorTables == null || MonitorTables.Count == 0)
                throw new Exception($"At least one table need to be added for setup. Call {nameof(AddTable)}() method for add tables that needs to be monitored.");

            using (SqlConnection con = new SqlConnection(ConnectionString)) 
            {
                con.Open();
                foreach (var table in MonitorTables)
                {
                    var sql = $@"IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('{table.TableName}'))
                            BEGIN
                                ALTER TABLE {table.TableName} ENABLE CHANGE_TRACKING WITH(TRACK_COLUMNS_UPDATED = OFF)
                            END";
                    using (SqlCommand command = new SqlCommand(sql, con))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }


        public void Start()
        {
            Running = true; 
            Thread = new Thread(()=> {
                while (Running) 
                {
                    try 
                    {
                        if (OnRecordChnaged != null && MonitorTables != null && MonitorTables.Count > 0)
                        {
                            var args = new DataChangedEventArgs();
                            args.ChangedEntities = new List<Models.Entity>() { };

                            using (SqlConnection con = new SqlConnection(ConnectionString))
                            {
                                con.Open();

                                foreach (var table in MonitorTables) 
                                {
                                    var sql = $"SELECT * FROM CHANGETABLE(CHANGES {table.TableName}, {table.LastVersion}) AS TableChanges order by SYS_CHANGE_VERSION DESC";
                                    using (SqlCommand command = new SqlCommand(sql, con))
                                    {
                                        SqlDataReader reader = command.ExecuteReader();
                                        while (reader.Read())
                                        {
                                            var entity = new Models.Entity();
                                            entity.TableName = table.TableName;

                                            if (!reader.IsDBNull(reader.GetOrdinal(table.PrimaryKeyColumnName)))
                                            {
                                                entity.PrimaryKeyValue = reader.GetValue(reader.GetOrdinal(table.PrimaryKeyColumnName));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("SYS_CHANGE_OPERATION"))) 
                                            {
                                                entity.ChangeType = (ChangeType)Enum.Parse(typeof(ChangeType), reader.GetString(reader.GetOrdinal("SYS_CHANGE_OPERATION")));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("SYS_CHANGE_CREATION_VERSION"))) 
                                            {
                                                entity.SYS_CHANGE_CREATION_VERSION = reader.GetInt64(reader.GetOrdinal("SYS_CHANGE_CREATION_VERSION"));
                                            }
                                            if (!reader.IsDBNull(reader.GetOrdinal("SYS_CHANGE_VERSION"))) 
                                            {
                                                entity.SYS_CHANGE_VERSION = reader.GetInt64(reader.GetOrdinal("SYS_CHANGE_VERSION"));
                                            }   
                                            
                                            args.ChangedEntities.Add(entity);                                            
                                        }
                                        var items = args.ChangedEntities.Where(x => x.TableName == table.TableName);
                                        table.LastVersion = items.Count() > 0 ? items.Max(x => x.SYS_CHANGE_VERSION) : table.LastVersion;
                                    }
                                }
                                
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
                    Thread.Sleep(PollIntervalSeconds*1000);
                }
                
            });
            Thread.Start();
        }
                

        public void Stop() 
        {
            Dispose();
        }

        public void Dispose()
        {
            Running = false;
            try {
                Thread?.Abort();
            }
            catch (Exception e) 
            { 
                Debug.Write(e.Message);
            }
            
        }
    }
}
