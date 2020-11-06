using CDCSqlMonitor.CT.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CDCSqlMonitor.CT.EventArgs
{
    public class DataChangedEventArgs: System.EventArgs
    {
        public List<Entity> ChangedEntities { get; set; }
    }
}
