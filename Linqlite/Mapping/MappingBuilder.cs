using Linqlite.Attributes;
using Linqlite.Linq.Relations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ZSpitz.Util;
using NotNullAttribute = Linqlite.Attributes.NotNullAttribute;

namespace Linqlite.Mapping
{
    public static class MappingBuilder
    {
        public static List<EntityPropertyInfo> BuildMap(Type type, out List<IUniqueConstraint> groups, out List<IRelation> relations)
        {
            var list = new List<EntityPropertyInfo>();
            groups = new List<IUniqueConstraint>();
            relations = new List<IRelation>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                bool isCol = false;
                // 1. Propriété simple avec attribut
                EntityPropertyInfo propertyInfo;

                var nxn = prop.GetCustomAttribute<NxNAttribute>();
                if (nxn != null)
                {
                    IRelation relation = new NxNRelation(type, prop.PropertyType.GetGenericArguments()[0], nxn.AssociationType, nxn.LeftKey, nxn.RightKey, prop);
                   
                    relations.Add(relation);
                    continue; // on ignore leséventuels autres attributs qui n'ont pas de sens sur une relation nxn
                }

                var onexn = prop.GetCustomAttribute<OnexNAttribute>();
                if (onexn != null)
                {
                    IRelation relation = new OnexNRelation(type, prop.PropertyType.GetGenericArguments()[0], onexn.TargetKey, prop);

                    relations.Add(relation);
                    continue; // on ignore leséventuels autres attributs qui n'ont pas de sens sur une relation nxn
                }

                var colAttr = prop.GetCustomAttribute<ColumnAttribute>(); 
                if (colAttr == null) // Propriété non mappée
                    continue;
                
                propertyInfo = new EntityPropertyInfo() { PropertyInfo = prop };
                propertyInfo.ColumnName = colAttr.ColumnName;
                propertyInfo.PropertyInfo = prop;
                isCol = true;
                
                var primary = prop.GetCustomAttribute<PrimaryKeyAttribute>();
                if (primary != null)
                {
                    propertyInfo.IsPrimaryKey = true;
                    propertyInfo.IsAutoIncrement = primary.AutoIncrement;
                }

                var foreign = prop.GetCustomAttribute<ForeignKeyAttribute>();
                if (foreign != null)
                {
                    (Type Entity, string Key, bool CascadeDelete) f;
                    f.Entity = foreign.Entity;
                    f.Key = foreign.Key;
                    f.CascadeDelete = foreign.CascadeDelete;
                    propertyInfo.ForeignKey = f;
                }

                var notnull = prop.GetCustomAttribute<NotNullAttribute>();
                if(notnull != null)
                {
                    propertyInfo.IsNotNull = true;
                }
                else
                {
                    propertyInfo.IsNotNull = false;
                }

                var unique = prop.GetCustomAttribute<UniqueAttribute>();
                if (unique != null) 
                {
                    propertyInfo.IsUnique = true;
                    propertyInfo.ConflictAction = unique.OnConflict;
                    var uniqueC = new UniqueConstraint()
                    {
                        Name = propertyInfo.ColumnName,
                        IsUpsertKey = unique.IsUpsertKey,
                        OnConflict = unique.OnConflict
                    };
                    groups.Add(uniqueC);
                }
               
                var uniqueGroup = prop.GetCustomAttributes<UniqueGroupAttribute>();
                if(uniqueGroup != null)
                {
                    
                    foreach (var group in uniqueGroup) 
                    {
                        var gr = groups.FirstOrDefault(g => g.Name == group.GroupName);
                        if(gr is null)
                        {
                            gr = new UniqueGroupConstraint() { 
                                Name = group.GroupName, 
                                IsUpsertKey = group.IsUpsertKey,
                                OnConflict = group.OnConflict
                            };
                            groups.Add(gr);
                        }
                        if (gr.OnConflict == ConflictAction.None) // Si action sur conflit précisée
                            gr.OnConflict = group.OnConflict;
                        if(group.IsUpsertKey) // On doit mettre à jour le groupe lorsqu'on rencontre un IsUpsertKey à vrai sur une des colonnes
                            gr.IsUpsertKey = true;
                       
                        gr.Columns.Add(propertyInfo.ColumnName);
                    }
                }

               

                if (isCol)
                    list.Add(propertyInfo);
            }
            int nbUpserts = groups.Where(g => g.IsUpsertKey).Count();
            if (nbUpserts > 1) 
            {
                throw new Exception($"Plusieurs UpsertKeys définies sur {type.Name}");
            }
            return list;
        }

        

    }
}
