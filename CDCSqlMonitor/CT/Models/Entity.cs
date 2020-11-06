using CDCSqlMonitor.CT.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CDCSqlMonitor.CT.Models
{
    public class Entity
    {
        /// <summary>
        /// Primary key of the table
        /// </summary>
        public object PrimaryKeyValue { get; set; }
        public string TableName { get; set; }
        public ChangeType ChangeType { get; set; }
        public long SYS_CHANGE_VERSION { get; set; }
        public long SYS_CHANGE_CREATION_VERSION { get; set; }
    }
}
