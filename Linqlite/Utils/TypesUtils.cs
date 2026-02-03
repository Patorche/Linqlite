using Linqlite.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Utils
{
    internal static class TypesUtils
    {
        internal static bool TypeContainsEntity(Type resultType)
        {
            Type type;

            if (IsEnumerable(resultType))
            {
                type = resultType.GetGenericArguments()[0];
            }
            else
            {
                type = resultType;
            }

            if (IsEntityType(type))
                return true;

            if (TypesUtils.IsCSharpAnonymousType(type))
            {
                var p = type.GetProperties();
                if (p.Any(p => TypeContainsEntity(p.PropertyType)))
                    return true;
            }

            return false;

        }

        internal static bool IsEntityType(Type t) => typeof(SqliteEntity).IsAssignableFrom(t);

     /*   internal static bool IsAnonymousType(Type t)
        {
            return Attribute.IsDefined(t, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
                   && t.Name.Contains("AnonymousType")
                   && t.IsGenericType;
        }
     */
        internal static bool IsCSharpAnonymousType(Type t) => (t.Name.Contains("AnonymousType") && t.IsSealed && t.IsNotPublic);

        internal static bool IsEnumerable(Type t)
        {
            if (t == typeof(string))
                return false;

            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        internal static bool IsLinqliteAnonymousType(Type t)
        {
            if (t.Name.Contains("Anon_"))
                return true;
            return false;

        }

        internal static bool IsBaseType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t.IsPrimitive ||
                   t.IsEnum ||
                   t == typeof(string) ||
                   t == typeof(decimal) ||
                   t == typeof(DateTime) ||
                   t == typeof(Guid) ||
                   t == typeof(byte[]);
        }

        internal static bool IsDto(Type t) => !IsBaseType(t) && !TypesUtils.IsCSharpAnonymousType(t);

        public static IEnumerable<(string Path, Type EntityType)> ExtractEntities(Type type, string prefix = "")
        {

            if (!TypesUtils. IsCSharpAnonymousType(type) & !TypesUtils.IsLinqliteAnonymousType(type))
                yield break;

            foreach (var prop in type.GetProperties())
            {
                var propType = prop.PropertyType;
                var path = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                if (IsEntityType(propType))
                {
                    yield return (path, propType);
                }
                else
                {
                    foreach (var nested in ExtractEntities(propType, path))
                        yield return nested;
                }
            }
        }
    }
}
