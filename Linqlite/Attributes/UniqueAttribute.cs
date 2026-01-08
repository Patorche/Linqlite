using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class UniqueAttribute : Attribute
    {
        public ConflictAction OnConflict { get; set; } = ConflictAction.None;
        public bool IsUpsertKey { get; set; } = false;
    }

}
