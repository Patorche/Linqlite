using Linqlite.Attributes;
using Linqlite.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ZSpitz.Util;

namespace Linqlite.Mapping
{
    public class EntityMap
    {
        private static ConcurrentDictionary<Type, EntityMap> EntityMaps = new();
        private Dictionary<string, EntityPropertyInfo> _columnLookup;

        public List<EntityPropertyInfo> Columns;
        public string TableName { get; }
        public bool IsFromTable { get => !string.IsNullOrWhiteSpace(TableName); }
        public List<UniqueGroup> UniqueGroups;
        private EntityMap(Type type) 
        { 
            TableName = GetTablename(type);
            Columns = MappingBuilder.BuildMap(type, out UniqueGroups);
        }

        public string Column(string propertyNamePath)
        {
            //EntityPropertyInfo e = propertiesInfo.First(p => p.PropertyInfo.Name.Equals(prop));

            string[] props = propertyNamePath.Split('.');
            List<EntityPropertyInfo> propertiesInfo = Columns;
          
            var e = Columns.First(c => c.PropertyInfo.Name == props[0]);

            if (props.Length > 1)
            {
                return EntityMap.Get(e.PropertyType).Column(string.Join('.', string.Join('.', props.TakeLast(props.Length - 1))));
            }
            return e.ColumnName;
        }


        private string GetTablename(Type type) 
        {
            string tablename = "";
            //List<Attribute> attributes = [.. type.GetCustomAttributes()];
            //var tableAttr = type.GetCustomAttribute<TableAttribute>(); return tableAttr?.TableName?.ToUpper() ?? type.Name.ToUpper();
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null) 
            { 
                return tableAttr.TableName;
            }
            return "";
        }
        public static EntityMap Get(Type type)
        {
            if (EntityMaps.TryGetValue(type, out var map)) 
                return map;
            if(!type.IsSubclassOf(typeof(SqliteEntity))) 
            {
                return null;
            }
            map = new EntityMap(type);
            EntityMaps.TryAdd(type, map); 
            
            return map;
        }

        internal string Projection(string alias)
        {
            List<string> cols = new List<string>();

            foreach (var col in Columns) {
                if (!string.IsNullOrEmpty(col.ColumnName))
                {
                    cols.Add($"{alias}.{col.ColumnName}");
                }
                else
                {
                    cols.AddRange(EntityMap.Get(col.PropertyType).Projection(alias));
                }
            }

            return string.Join(", ", cols.ToArray());
        }
    }

}
