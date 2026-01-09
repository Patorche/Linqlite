using Linqlite.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Mapping
{
    public class EntityPropertyInfo
    {
        public string ColumnName { get; set; } = "";
        //public PropertyInfo[] PropertyPath { get; set; } = Array.Empty<PropertyInfo>();
        public required PropertyInfo PropertyInfo { get; set; } 

        public Type PropertyType => PropertyInfo.PropertyType;
        public bool IsPrimaryKey { get; internal set; }
        public bool IsAutoIncrement { get; internal set; }
        public (Type Entity, string Key, bool CascadeDelete)? ForeignKey { get; internal set; }
        public bool IsNotNull {  get; internal set; }
        public bool IsUnique { get; internal set; }
        public ConflictAction? ConflictAction { get; internal set; }
    }

}
