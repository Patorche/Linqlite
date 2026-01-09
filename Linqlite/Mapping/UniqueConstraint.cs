using Linqlite.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Mapping
{
    public class UniqueConstraint : IUniqueConstraint
    {
        public bool IsUpsertKey { get; set; }
        public required string Name{ get; set; }
        public List<string> Columns => new List<string> { Name };
        public string? GroupName => null;

        public ConflictAction OnConflict { get ; set ; }
    }

}
