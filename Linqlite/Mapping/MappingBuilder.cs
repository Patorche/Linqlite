using Linqlite.Attributes;
using System.Reflection;

namespace Linqlite.Mapping
{
    public static class MappingBuilder
    {
        public static List<EntityPropertyInfo> BuildMap(Type type)
        {
            var list = new List<EntityPropertyInfo>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // 1. Propriété simple avec attribut
                var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (colAttr != null)
                {
                    list.Add(new EntityPropertyInfo
                    {
                        ColumnName = colAttr.ColumnName,
                        PropertyInfo = prop
                    });
                }

                // 2. Propriété complexe : on cherche des colonnes dans ses sous-propriétés
                /*if (IsComplexType(prop.PropertyType))
                {
                    foreach (var sub in prop.PropertyType.GetProperties())
                    {
                        var subAttr = sub.GetCustomAttribute<ColumnAttribute>();
                        if (subAttr != null)
                        {
                            list.Add(new EntityPropertyInfo
                            {
                                ColumnName = subAttr.ColumnName,
                                PropertyPath = new[] { prop, sub }
                            });
                        }
                    }
                }*/
            }

            return list;
        }

        private static bool IsComplexType(Type t)
        {
            return t.IsClass && t != typeof(string);
        }
    }

}
