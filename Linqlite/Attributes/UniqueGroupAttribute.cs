using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class UniqueGroupAttribute : Attribute
    {
        public string GroupName { get; }
        public ConflictAction OnConflict { get; set; } = ConflictAction.None;

        public UniqueGroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }

}
