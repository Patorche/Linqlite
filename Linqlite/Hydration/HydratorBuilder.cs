using Linqlite.Linq;
using Linqlite.Linq.Relations;
using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.PortableExecutable;
using ZSpitz.Util;

namespace Linqlite.Hydration
{
    public static class HydratorBuilder
    {
        private static readonly Dictionary<(Type, object), object> _identityMap = new();

        public static T? GetEntity<T>(this SqliteDataReader reader, string alias) where T : SqliteEntity, new()
        {
            try
            {
                T entity = EntityFactory<T>.CreateInstance();
                var map = EntityMap.Get(typeof(T));
                if (map == null)
                    return null;
                foreach (var column in map.Columns)
                {
                    try
                    {
                        column.PropertyInfo.SetValue(entity, reader.GetValue(alias, column.ColumnName, column.PropertyType));
                    }
                    catch (Exception) { }
                }
                entity.IsNew = false;
                return entity;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static object GetEntity(LinqliteProvider provider, Type t, SqliteDataReader reader, string alias)
        {
            try
            {
                var entity = (SqliteEntity)Activator.CreateInstance(t)!;
                var map = EntityMap.Get(t);
                if (map == null)
                    return entity;
                foreach (var column in map.Columns)
                {
                    try
                    {
                        column.PropertyInfo.SetValue(entity, reader.GetValue(alias, column.ColumnName, column.PropertyType));
                    }
                    catch (Exception) 
                    {
                        int i = 0;
                    }
                }
                var table = provider.GetTable(t);
                provider.Attach(entity, table.TrackingModeOverride);
                entity.IsNew = false;
                return entity;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SetPrimaryKey<T>(T entity, long id) where T : SqliteEntity
        {
            var map = EntityMap.Get(typeof(T));
            if (map == null)
                throw new UnreachableException("Entitymap est null.");

            var primary = map.Columns.Single(c => c.IsPrimaryKey);
            primary.PropertyInfo.SetValue(entity, id);
        }

        internal static T HydrateLinqliteAnonymous<T>(SqliteDataReader reader, LinqliteProvider provider, SqlMemberProjectionExpression infos)
        {
            /*var type = typeof(T);
            var ctor = type.GetConstructors().Single();
            var parameters = ctor.GetParameters();*/

            T instance = (T)Activator.CreateInstance(typeof(T));

            var props = typeof(T).GetProperties();
            var entities = new object?[props.Length];
            int i = 0;
            foreach (var prop in props)
            {
                var memberInfo = infos.Columns.Single(c => c.Key == prop.Name);

                object value;
                if (IsEntity(prop.PropertyType))
                {
                    string alias = "";
                    if(memberInfo.Value.Item2 is SqlEntityReferenceExpression)
                    {
                        SqlEntityReferenceExpression re = memberInfo.Value.Item2 as SqlEntityReferenceExpression;
                        alias = re.Alias;
                    }
                    else if(memberInfo.Value.Item2 is SqlEntityProjectionExpression)
                    {
                        var ep = memberInfo. Value.Item2 as SqlEntityProjectionExpression;
                        alias = ep.Alias;
                    }


                    var pkey = EntityMap.Get(prop.PropertyType).GetPrimaryKey();

                    var key = reader.GetValue(alias, pkey.ColumnName, pkey.PropertyType);
                    var identityKey = (prop.PropertyType, key);

                    if (_identityMap.TryGetValue(identityKey, out var existing))
                    {
                        value = (SqliteEntity)existing;
                    }
                    else
                    {
                        value = GetEntity(provider,prop.PropertyType, reader, alias);
                    }
                    _identityMap[identityKey] = value;
                    prop.SetValue(instance, value);
                    entities[i] = value;
                    i++;
                }
            }

            
            BuildRelations(entities, instance);
            return instance;
        }

        internal static T HydrateAnonymous<T>(SqliteDataReader reader, LinqliteProvider provider, SqlMemberProjectionExpression infos)
        {
            var type = typeof(T);
            var ctor = type.GetConstructors().Single();
            var parameters = ctor.GetParameters();

            // Toutes les valeurs SQL
            var values = new object[reader.FieldCount];
            reader.GetValues(values);

            var args = new object?[parameters.Length];

            int offset = 0;
            int i = 0;
            foreach (var (member, sqlExpr) in infos.Columns)
            {

                var paramType = parameters[i].ParameterType;

                // Cas 1 : entité Linqlite
                if (typeof(SqliteEntity).IsAssignableFrom(paramType))
                {

                    // Hydrater l'entité
                    SqliteEntity? entity = null;
                    SqlEntityProjectionExpression p = sqlExpr.Item2 as SqlEntityProjectionExpression;
                    string alias = p.Columns[i].Alias;

                    var pkey = EntityMap.Get(paramType).GetPrimaryKey();
                    var key = reader.GetValue(alias, pkey.ColumnName, pkey.PropertyType);
                    var identityKey = (paramType, key);

                    if (_identityMap.TryGetValue(identityKey, out var existing))
                    {
                        entity = (SqliteEntity)existing;
                    }
                    else
                    {
                        entity = (SqliteEntity)GetEntity(provider, paramType, reader, alias);
                        var columns = EntityMap.Get(paramType).Columns;
                        /*entity = (SqliteEntity)Activator.CreateInstance(paramType)!;
                        foreach (var c in columns)
                        {
                            c.PropertyInfo.SetValue(entity, reader.GetValue(alias, c.ColumnName, c.PropertyType));
                        }*/
                        if (key != null)
                        {
                            _identityMap[identityKey] = entity;
                            // Attacher l'entité au provider (tracking)
                            
                        }
                    }
                    args[i] = entity;
                    i++;

                    //offset += propCount;
                }
                else
                {
                    // Cas 2 : scalaire
                    var raw = values[offset];
                    var t = Nullable.GetUnderlyingType(paramType) ?? paramType;
                    args[i] = raw == DBNull.Value ? null : Convert.ChangeType(raw, t);
                    offset++;
                }
                i++;
            }

            var item = (T)ctor.Invoke(args);
            return item;
        }


        private static void BuildRelations<T>(object?[] entities, T item)
        {
            if (entities.Any(e => e == null))
                return;
            foreach (var entity in entities) 
            {
                if (entity == null)
                    return;
                Type t = entity.GetType();
                List<IRelation> relations = EntityMap.Get(t).Relations;
                BuildRelations(entity,entities,relations, item);
            }

        }

     

        private static void BuildRelations<T>(object entity, object?[] entities, List<IRelation> relations, T item)
        {
            foreach (var relation in relations)
            {
                if (relation is NxNRelation nxn)
                {
                    BuildNxNRelation(nxn, entity, entities, item);
                }
                else if (relation is OnexNRelation onex)
                {
                    BuildOnexNRelation(onex, entity, entities, item);
                }
                
            }
        }

        private static void BuildOnexNRelation<T>(OnexNRelation relation, object entity, object?[] entities, T? item)
        {
            var left = entities.FirstOrDefault(e => e.GetType() == relation.LeftType);
            var right = entities.FirstOrDefault(e => e.GetType() == relation.TargetType);

            if (left == null || right == null) return;

            var leftPKey = EntityMap.Get(left.GetType()).GetPrimaryKey();
            var leftId = left.GetType().GetProperty(leftPKey.PropertyInfo.Name).GetValue(left);

            var rightId = right.GetType().GetProperty(relation.TargetKey).GetValue(right);

            if (leftId == null || rightId == null) return;

            if (Equals(leftId, rightId))
            {
                var prop = entity.GetType().GetProperty(relation.Property.Name);
                var collection = prop.GetValue(entity);
                if (collection is null)
                {
                    var collectionType = prop.PropertyType;

                    if (collectionType.IsInterface)
                    {
                        var elementType = collectionType.GetGenericArguments()[0];
                        collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    }
                    else
                    {
                        collection = Activator.CreateInstance(collectionType);
                    }

                    prop.SetValue(entity, collection);
                }
                var addMethod = collection.GetType().GetMethod("Add");
                addMethod.Invoke(collection, new[] { right });
            }
        }

        private static void BuildNxNRelation<T>(NxNRelation relation, object entity, object?[] entities, T? item)
        {
            var left = entities.FirstOrDefault(e => e.GetType() == relation.LeftType);
            var right = entities.FirstOrDefault(e => e.GetType() == relation.RightType);

            if (left == null || right == null) return;

            var leftPKey = EntityMap.Get(left.GetType()).GetPrimaryKey();
            var rightPKey = EntityMap.Get(right.GetType()).GetPrimaryKey();

            var leftId = left.GetType().GetProperty(leftPKey.PropertyInfo.Name).GetValue(left);
            var rightId = right.GetType().GetProperty(rightPKey.PropertyInfo.Name).GetValue(right);

            if (leftId == null || rightId == null) return;

            var props = item.GetType().GetProperties();
            var join = props.FirstOrDefault(p => p.PropertyType == relation.AssociationType)?.GetValue(item);
            if (join != null)
            {
                var joinLeftId = join.GetType().GetProperty(relation.AssociationLeftKey).GetValue(join);
                var joinRightId = join.GetType().GetProperty(relation.AssociationRightKey).GetValue(join);

                if (Equals(joinLeftId, leftId) && Equals(joinRightId, rightId))
                {
                    // On ajoute la relation
                    var prop = entity.GetType().GetProperty(relation.Property.Name);
                    var collection = prop.GetValue(entity);

                    if (collection is null)
                    {
                        var collectionType = prop.PropertyType;

                        if (collectionType.IsInterface)
                        {
                            var elementType = collectionType.GetGenericArguments()[0];
                            collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                        }
                        else
                        {
                            collection = Activator.CreateInstance(collectionType);
                        }

                        prop.SetValue(entity, collection);
                    }
                    //((IList)collection).Add(right);
                    var addMethod = collection.GetType().GetMethod("Add");
                    addMethod.Invoke(collection, new[] { right });

                }
            }
        }

        internal static object? ConvertValue(object raw, Type targetType)
        {
            if (raw == DBNull.Value)
                return targetType.IsValueType ? Activator.CreateInstance(targetType)! : null;

            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                var converted = Convert.ChangeType(raw, underlying);
                return Activator.CreateInstance(targetType, converted)!;
            }

            if (targetType.IsEnum)
                return Enum.ToObject(targetType, raw);

            return Convert.ChangeType(raw, targetType);
        }

        internal static T HydrateDto<T>(DbDataReader reader, SqlMemberProjectionExpression projection)
        {
            // 1. Lire toutes les valeurs SQL
            var values = new object[reader.FieldCount];
            reader.GetValues(values);

            // 2. Récupérer les membres projetés dans l’ordre du SELECT
            var ordered = projection.Columns.ToList(); // ordre garanti par le visitor

            // 3. Préparer les valeurs converties
            var converted = new object?[ordered.Count];

            for (int i = 0; i < ordered.Count; i++)
            {
                var member = ordered[i].Key;
                var raw = values[i];

                //converted[i] = ConvertValue(raw, GetMemberType(member));
                converted[i] = ConvertValue(raw, GetMemberType(ordered[i].Value.Item1));
            }

            var type = typeof(T);

            // 4. Essayer d'utiliser un constructeur compatible
            var ctors = type.GetConstructors();

            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length != converted.Length)
                    continue;

                bool compatible = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!IsAssignableTo(converted[i], parameters[i].ParameterType))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible)
                {
                    return (T)ctor.Invoke(converted);
                }
            }

            // 5. Sinon, fallback : instanciation + setters
            var instance = Activator.CreateInstance<T>();

            for (int i = 0; i < ordered.Count; i++)
            {
                var member = ordered[i].Value.Item1;
                var value = converted[i];

                SetMemberValue(instance, member, value);
            }

            return instance;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => throw new NotSupportedException("Membre non supporté")
            };
        }

        private static void SetMemberValue(object instance, MemberInfo member, object? value)
        {
            switch (member)
            {
                case PropertyInfo p when p.CanWrite:
                    p.SetValue(instance, value);
                    break;

                case FieldInfo f:
                    f.SetValue(instance, value);
                    break;

                default:
                    throw new InvalidOperationException($"Impossible d'affecter {member.Name}");
            }
        }

        private static bool IsAssignableTo(object? value, Type targetType)
        {
            if (value == null)
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;

            return targetType.IsAssignableFrom(value.GetType())
                || Nullable.GetUnderlyingType(targetType) != null;
        }
        private static bool IsEntity(Type t) => typeof(SqliteEntity).IsAssignableFrom(t);


    }


}
