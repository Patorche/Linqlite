using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Attributes
{
    public enum ConflictAction
    { 
        Rollback, 
        Abort, 
        Fail, 
        Ignore, 
        Replace,
        None
    }
}
