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
using System.Text;

namespace CDCSqlMonitor.CDC.Models
{
    public class CDCModel
    {        
        public byte[] __start_Lsn { get; set; }
        public byte[] __end_Lsn { get; set; }
        public byte[] __seqval { get; set; }
        public int __operation { get; set; }
        public short __update_mask { get; set; }
        public List<EntityColumn> Columns { get; set; }
        public int __command_id { get; set; }
        public string TableName { get; set; }
    }
}
