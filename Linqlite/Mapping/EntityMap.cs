using Linqlite.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Mapping
{
    public class EntityMap
    {
        private static ConcurrentDictionary<Type, EntityMap> EntityMaps = new();
        private Dictionary<string, EntityPropertyInfo> _columnLookup;

        public List<EntityPropertyInfo> Columns;
        public string TableName { get; }

        private EntityMap(Type type) 
        { 
            TableName = GetTablename(type);
            Columns = MappingBuilder.BuildMap(type);
        }

        public string Column(string propertyNamePath)
        {
            string[] props = propertyNamePath.Split('.');

            List<EntityPropertyInfo> propertiesInfo = Columns;
            int prof = 0;
            foreach(string prop in props)
            {
                // On regarde si la propriété est bien là.
                if (prof.Equals(props.Length-1))
                {
                    EntityPropertyInfo e = propertiesInfo.First(p => p.PropertyPath[prof].Name.Equals(prop));
                    if (propertiesInfo.First(p => p.PropertyPath[prof].Name.Equals(prop)) != null)
                    {
                        return e.ColumnName;
                    }
                    else throw new Exception($"Colonne introuvable pour {propertyNamePath}");
                }
                
                propertiesInfo = propertiesInfo.Where(p => p.PropertyPath[prof].Name.Equals(prop) && p.PropertyPath.Length > prof + 1).ToList();
                prof++;
            }

            return "";
        }


        private string GetTablename(Type type) 
        {
            string tablename = "";
            List<Attribute> attributes = [.. type.GetCustomAttributes()];
            var tableAttr = type.GetCustomAttribute<TableAttribute>(); return tableAttr?.TableName?.ToUpper() ?? type.Name.ToUpper();
        }
        public static EntityMap Get(Type type)
        {
            if (EntityMaps.TryGetValue(type, out var map)) 
                return map;
            
            var tableAttr = type.GetCustomAttribute<TableAttribute>(); 
            if (tableAttr == null) 
                return null;

            map = new EntityMap(type);
            EntityMaps.TryAdd(type, map); 
            
            return map;
        }

        
    }

}
