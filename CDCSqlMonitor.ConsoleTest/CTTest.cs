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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CDCSqlMonitor.ConsoleTest
{
    public class CTTest
    {
        public void Run()
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

        private void Monitor_OnRecordChnaged(object sender, CDCSqlMonitor.CT.EventArgs.DataChangedEventArgs e)
        {
            foreach (var item in e.ChangedEntities)
            {
                Debug.WriteLine("Operation: " + item.ChangeType.ToString() + "  Table: " + item.TableName + " ID: " + item.PrimaryKeyValue + " ChangeVersion: " + item.SYS_CHANGE_VERSION + "\n");
            }
        }

        private void Monitor_OnError(object sender, CDCSqlMonitor.CT.EventArgs.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception.StackTrace);
        }
    }
}
