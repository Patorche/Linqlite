using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.PortableExecutable;

namespace Linqlite.Hydration
{
    public static class HydratorBuilder
    {
        public static T? GetEntity<T>(this SqliteDataReader reader) where T : SqliteEntity, new()
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
                        column.PropertyInfo.SetValue(entity, reader.GetValue(column));
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

        public static void SetPrimaryKey<T>(T entity, long id) where T : SqliteEntity
        {
            var map = EntityMap.Get(typeof(T));
            if (map == null)
                throw new UnreachableException("Entitymap est null.");

            var primary = map.Columns.Single(c => c.IsPrimaryKey);
            primary.PropertyInfo.SetValue(entity, id);
        }

        internal static T HydrateAnonymous<T>(SqliteDataReader reader)
        {
            var type = typeof(T);
            var ctor = type.GetConstructors().Single();
            var parameters = ctor.GetParameters();

            var values = new object[reader.FieldCount];
            reader.GetValues(values);

            var args = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var raw = values[i];

                args[i] = ConvertValue(raw, paramType);
            }

            return (T)ctor.Invoke(args);
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
            var ordered = projection.Columns.ToList(); // ordre garanti par ton visitor

            // 3. Préparer les valeurs converties
            var converted = new object?[ordered.Count];

            for (int i = 0; i < ordered.Count; i++)
            {
                var member = ordered[i].Key;
                var raw = values[i];

                converted[i] = ConvertValue(raw, GetMemberType(member));
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
                var member = ordered[i].Key;
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



    }


}
