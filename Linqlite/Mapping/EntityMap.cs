using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Mapping
{
    public static class EntityMap<T>
    {
        public static readonly List<EntityPropertyInfo> Columns;

        static EntityMap()
        {
            Columns = MappingBuilder.BuildMap(typeof(T));
        }
    }

}
