using Linqlite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Mapping
{
    public class UniqueGroup
    {
        public string Name { get; set; } = "";
        public List<string> Columns { get; set; } = new();
        public ConflictAction OnConflict { get; set; } = ConflictAction.None;
    }
}
