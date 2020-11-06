using CDCSqlMonitor.CDC.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CDCSqlMonitor.CDC.Models
{
    public class Entity
    {
        public List<EntityColumn> Columns { get; set; }
        public ChangeType ChangeType { get; set; }
    }

    public class EntityColumn
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public object OldValue { get; set; }
    }
}
