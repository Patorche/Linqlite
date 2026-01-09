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

        public List<EntityPropertyInfo> Columns;
        public string TableName { get; }
        public bool IsFromTable { get => !string.IsNullOrWhiteSpace(TableName); }
        public List<IUniqueConstraint> UniqueConstraints;
        private EntityMap(Type type) 
        { 
            TableName = GetTablename(type);
            Columns = MappingBuilder.BuildMap(type, out UniqueConstraints);
        }

        public string Column(string propertyNamePath)
        {
            //EntityPropertyInfo e = propertiesInfo.First(p => p.PropertyInfo.Name.Equals(prop));

            string[] props = propertyNamePath.Split('.');
            List<EntityPropertyInfo> propertiesInfo = Columns;
          
            var e = Columns.First(c => c.PropertyInfo.Name == props[0]);

            if (props.Length > 1)
            {
                var map = EntityMap.Get(e.PropertyType);
                if (map == null)
                    return "";
                return map.Column(string.Join('.', string.Join('.', props.TakeLast(props.Length - 1))));
            }
            return e.ColumnName;
        }


        private string GetTablename(Type type) 
        {
            var tableAttr = type.GetCustomAttribute<TableAttribute>();
            if (tableAttr != null) 
            { 
                return tableAttr.TableName;
            }
            return "";
        }

        public static EntityMap? Get(Type type)
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

        public IUniqueConstraint? GetUpsertKey()
        {
            IUniqueConstraint? constraint = UniqueConstraints.SingleOrDefault(u => u.IsUpsertKey);
            return constraint;
        }
        public EntityPropertyInfo GetPrimaryKey()
        {
            return Columns.Single(c => c.IsPrimaryKey);
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
                    var map = EntityMap.Get(col.PropertyType);
                    if(map != null)
                        cols.AddRange(map.Projection(alias));
                }
            }

            return string.Join(", ", cols.ToArray());
        }
    }

}
