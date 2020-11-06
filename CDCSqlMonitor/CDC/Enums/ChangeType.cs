using System;
using System.Collections.Generic;
using System.Text;

namespace CDCSqlMonitor.CDC.Enums
{
    public enum ChangeType
    {
        DELETE,
        INSERT,
        UPDATE_OLD_VALUE, 
        UPDATE_NEW_VALUE, 
        
    }
}
