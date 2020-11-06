using CDCSqlMonitor.CT.Models;
using System;
using System.Diagnostics;

namespace CDCSqlMonitor.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "";
            CT.Monitor monitor = new CT.Monitor(connectionString, 5);// 5 - Interval in seconds
            monitor.OnError += Monitor_OnError;
            monitor.OnRecordChnaged += Monitor_OnRecordChnaged;
            monitor.AddTable("dbo.AMytable", "ID");
            monitor.AddTable("dbo.AMytable2", "PRIMARYKEYCOL");

            monitor.Setup();

            monitor.Start();
            Console.ReadLine();
        }

        private static void Monitor_OnRecordChnaged(object sender, CDCSqlMonitor.CT.EventArgs.DataChangedEventArgs e)
        {
            foreach (var item in e.ChangedEntities) 
            {
                Debug.WriteLine("Operation: "+ item.ChangeType.ToString()+"  Table: " +item.TableName +" ID: "+ item.PrimaryKeyValue + " ChangeVersion: "+item.SYS_CHANGE_VERSION + "\n");
            }            
        }

        private static void Monitor_OnError(object sender, CDCSqlMonitor.CT.EventArgs.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception.StackTrace);
        }
    }
}
