using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Linqlite.Hydration
{
    public static class SqliteDataReaderExt
    {
        private static readonly ConditionalWeakTable<SqliteDataReader, Dictionary<string, int>> _ordinalCache = new();

        private static int GetCachedOrdinal(SqliteDataReader reader, string columnName)
        {
            if (!_ordinalCache.TryGetValue(reader, out var cache))
            {
                cache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                _ordinalCache.Add(reader, cache);
            }

            if (!cache.TryGetValue(columnName, out int ordinal))
            {
                ordinal = reader.GetOrdinal(columnName);
                cache[columnName] = ordinal;
            }

            return ordinal;
        }

        public static object? GetValue(this SqliteDataReader reader, EntityPropertyInfo property)
        {

            Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            return type switch
            {
                Type t when t == typeof(string) => reader.GetString(property.ColumnName),
                Type t when t == typeof(int) => reader.GetInt(property.ColumnName),
                Type t when t == typeof(long) => reader.GetLong(property.ColumnName),
                Type t when t == typeof(double) => reader.GetDouble(property.ColumnName),
                Type t when t == typeof(bool) => reader.GetBool(property.ColumnName),
                Type t when t == typeof(DateTime) => reader.GetDate(property.ColumnName),
                Type t when t.IsSubclassOf(typeof(ObservableObject)) => reader.GetEntity(property),
                _ => throw new Exception($"Type incompatible : {type.FullName}")
            };
        }

        
        
        
        public static object GetEntity(this SqliteDataReader reader, EntityPropertyInfo property)
        {
            Type t = property.PropertyType;
            var o = EntityDynamicFactory.CreateInstance(t);

            //Type genericType = typeof(SqliteOEntity<>).MakeGenericType(t);
            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(genericType.TypeHandle);
            //Dictionary<string, EntityPropertyInfo> cols = (Dictionary<string, EntityPropertyInfo>)genericType.GetProperty("Columns", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            

            foreach (var column in EntityMap.Get(property.PropertyType).Columns)
            {
                column.PropertyInfo.SetValue(o, reader.GetValue(column));
            }
            return o;
        }
        
        public static string GetString(this SqliteDataReader reader, string columnName)
        {
            try
            {
                int index = GetCachedOrdinal(reader, columnName);
                return reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static int GetInt(this SqliteDataReader reader, string columnName)
        {
            try
            {
                int index = GetCachedOrdinal(reader, columnName);
                return reader.IsDBNull(index) ? 0 : reader.GetInt32(index);
            }
            catch
            {
                return 0;
            }
        }

        public static bool GetBool(this SqliteDataReader reader, string columnName)
        {
            try
            {
                int index = GetCachedOrdinal(reader, columnName);
                return reader.IsDBNull(index) ? false : reader.GetBoolean(index);
            }
            catch
            {
                return false;
            }
        }

        public static long GetLong(this SqliteDataReader reader, string columnName)
        {
            try
            {
                int index = GetCachedOrdinal(reader, columnName);
                return reader.IsDBNull(index) ? 0 : reader.GetInt64(index);
            }
            catch
            {
                return 0;
            }
        }

        public static double GetDouble(this SqliteDataReader reader, string columnName)
        {
            try
            {
                int index = GetCachedOrdinal(reader, columnName);
                return reader.IsDBNull(index) ? 0.0 : reader.GetDouble(index);
            }
            catch
            {
                return 0.0;
            }
        }

        public static DateTime? GetDate(this SqliteDataReader reader, string columnName)
        {
            try
            {
                int index = GetCachedOrdinal(reader, columnName);
                if (reader.IsDBNull(index)) return null;

                var value = reader.GetString(index);
                if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture,
                                           DateTimeStyles.None, out var date))
                {
                    return date;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class EntityDynamicFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> _cache = new();

        public static object CreateInstance(Type type)
        {
            if (!_cache.TryGetValue(type, out var creator))
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    throw new InvalidOperationException($"Type {type} must have a parameterless constructor.");

                var newExpr = Expression.New(ctor);
                var lambda = Expression.Lambda<Func<object>>(newExpr);
                creator = lambda.Compile();
                _cache[type] = creator;
            }

            return creator();
        }

    }

    internal static class EntityFactory<T> where T : ObservableObject
    {
        // Cache de délégués pour instanciation rapide
        private static readonly Func<T> _creator = CreateConstructor();

        private static Func<T> CreateConstructor()
        {
            var ctor = typeof(T).GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException($"Type {typeof(T)} must have a parameterless constructor.");

            // Utilise Expression pour compiler un délégué rapide
            var newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda<Func<T>>(newExpr);
            return lambda.Compile();
        }

        public static T CreateInstance()
        {
            return _creator();
        }
    }

}
