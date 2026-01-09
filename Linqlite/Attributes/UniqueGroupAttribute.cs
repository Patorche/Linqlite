using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class UniqueGroupAttribute : Attribute
    {
        public string GroupName { get; }
        public bool IsUpsertKey { get; set; } = false;
        public ConflictAction OnConflict { get; set; } = ConflictAction.None;

        public UniqueGroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }

}
