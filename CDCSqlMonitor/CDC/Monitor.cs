using CDCSqlMonitor.CTC.EventArgs;
using System;
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

        private string ConnectionString;
        private Thread Thread;
        private bool Running = false;

        public Monitor(string connectionString, int pollIntervalSeconds)
        {
            ConnectionString = connectionString;
            PollIntervalSeconds = pollIntervalSeconds;
        }


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
                            args.ChangedEntities = new List<Models.Entity>() { };

                            using (SqlConnection con = new SqlConnection(ConnectionString))
                            {
                                con.Open();

                                var MonitorTableObjectIds = new List<long>();
                                var sql = "SELECT * from [cdc].[change_tables]";
                                using (SqlCommand command = new SqlCommand(sql, con)) 
                                {
                                    SqlDataReader reader = command.ExecuteReader();
                                    while (reader.Read()) 
                                    {
                                        if (!reader.IsDBNull(0))
                                        {
                                            var objectId = reader.GetInt64(0);
                                            if(objectId > 0)
                                                MonitorTableObjectIds.Add(objectId);
                                        }                                       
                                    }
                                }



                                foreach (var objectId in MonitorTableObjectIds)
                                {
                                    sql = $@"
                                                declare @tablename nvarchar(max);
	                                            select @tablename = name from sys.tables where object_id = {objectId};
	                                            declare @sql nvarchar(max) = 'select * from cdc.' + @tablename + ' order by __$seqval desc'; 
                                                exec (@sql);
                                            ";
                                    using (SqlCommand command = new SqlCommand(sql, con))
                                    {
                                        SqlDataReader reader = command.ExecuteReader();
                                        while (reader.Read())
                                        {
                                            var ctc = new Models.CTCModel();
                                            //TODO
                                            var entity = new Models.Entity();
                                            args.ChangedEntities.Add(entity);
                                        }

                                       
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
                    Thread.Sleep(PollIntervalSeconds * 1000);
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
            try
            {
                Thread?.Abort();
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }

        }




    }
}
