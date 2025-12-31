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
        public PropertyInfo PropertyInfo { get; set; } 

        public Type PropertyType => PropertyInfo.PropertyType;
        public bool IsKey { get; set; }
        public bool IsOnconflict { get; internal set; }
    }

}
