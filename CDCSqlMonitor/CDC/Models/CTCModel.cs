using System;
using System.Collections.Generic;
using System.Text;

namespace CDCSqlMonitor.CDC.Models
{
    public class CTCModel
    {
        public long __start_Lsn { get; set; }
        public long __end_Lsn { get; set; }
        public long __seqval { get; set; }
        public long __operation { get; set; }
        public long __update_mask { get; set; }
        public List<EntityColumn> Columns { get; set; }
        public long __command_id { get; set; }
    }
}
