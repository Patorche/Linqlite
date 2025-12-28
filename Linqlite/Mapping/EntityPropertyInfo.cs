using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Mapping
{
    public class EntityPropertyInfo
    {
        public string ColumnName { get; set; } = "";
        public PropertyInfo[] PropertyPath { get; set; } = Array.Empty<PropertyInfo>();

        public Type PropertyType => PropertyPath.Last().PropertyType;
    }

}
