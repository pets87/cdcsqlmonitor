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
using CDCSqlMonitor.CDC.EventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CDCSqlMonitor.ConsoleTest
{
    public class CDCTest
    {
        public void Run()
        {
            var connectionString = "";
            CDC.Monitor monitor = new CDC.Monitor(connectionString, 5);// 5 - Interval in seconds
            monitor.OnError += Monitor_OnError;
            monitor.OnRecordChnaged += Monitor_OnRecordChnaged;
           

            monitor.Start();
            Console.ReadLine();
        }

        private void Monitor_OnRecordChnaged(object sender, DataChangedEventArgs e)
        {
            foreach (var item in e.ChangedEntities)
            {
                Debug.WriteLine("Operation: " + item.ChangeType.ToString() + "  Table: " + item.TableName + "\n");
                foreach (var col in item.Columns) 
                {
                    Debug.WriteLine("Column: " + col.Name + "  Value: " + col.Value+ (col.OldValue != null ? " OldValue: "+col.OldValue :"") +"\n");
                }
                Debug.WriteLine("\n");
            }
        }

        private void Monitor_OnError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception.StackTrace);
        }
    }
}
