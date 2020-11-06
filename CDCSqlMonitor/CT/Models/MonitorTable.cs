using System;
using System.Collections.Generic;
using System.Text;

namespace CDCSqlMonitor.CT.Models
{
    public class MonitorTable
    {
        public string TableName { get; set; }
        public string PrimaryKeyColumnName { get; set; }
        
        internal long LastVersion { get; set; }

    }
}
