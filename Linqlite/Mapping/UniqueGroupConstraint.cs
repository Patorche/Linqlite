using Linqlite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Mapping
{
    public class UniqueGroupConstraint : IUniqueConstraint
    {
        public string Name { get; set; } = "";
        public bool IsUpsertKey { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
        public ConflictAction OnConflict { get ; set ; }
}
}
