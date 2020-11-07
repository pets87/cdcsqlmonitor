﻿/*
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
using CDCSqlMonitor.CT.Models;
using System;
using System.Diagnostics;

namespace CDCSqlMonitor.ConsoleTest
{
    class Program
    {
        /*
         Links
        https://stackoverflow.com/questions/5288434/how-to-monitor-sql-server-table-changes-by-using-c
        https://www.c-sharpcorner.com/uploadfile/satisharveti/introduction-to-cdc-change-data-capture-of-sql-server/
        https://docs.microsoft.com/en-us/sql/relational-databases/system-tables/cdc-capture-instance-ct-transact-sql?view=sql-server-ver15
         */
        static void Main(string[] args)
        {
            new CDCTest().Run();

            new CTTest().Run();
        }

        

       
    }
}
