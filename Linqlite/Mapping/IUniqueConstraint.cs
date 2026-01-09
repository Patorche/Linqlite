using Linqlite.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Mapping
{
    public interface IUniqueConstraint
    {
        bool IsUpsertKey { get; set ; }
        List<string> Columns { get; } 
        string? Name { get; }
        ConflictAction OnConflict {  get; set ; }
    }

}
