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
        private EntityMap(Type type) 
        { 
            TableName = GetTablename(type);
            Columns = MappingBuilder.BuildMap(type);
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
            //int prof = 0;
            //foreach(string prop in props)
            //{
            //    // On regarde si la propriété est bien là.
            //    //if (prof.Equals(props.Length-1))
            //    {
            //        EntityPropertyInfo e = propertiesInfo.First(p => p.PropertyInfo.Name.Equals(prop));
            //        /*if (propertiesInfo.First(p => p.PropertyPath[prof].Name.Equals(prop)) != null)
            //        {
            //            return e.ColumnName;
            //        }*/
            //        return e.ColumnName;
            //        throw new Exception($"Colonne introuvable pour {propertyNamePath}");
            //    }
                
            //    propertiesInfo = propertiesInfo.Where(p => p.PropertyInfo.Name.Equals(prop)).ToList();
            //    prof++;
            //}

            //return "";
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
            if(!type.IsSubclassOf(typeof(SqliteObservableEntity))) 
            {
                return null;
            }
            map = new EntityMap(type);
            EntityMaps.TryAdd(type, map); 
            
            return map;
        }

        
    }

}
