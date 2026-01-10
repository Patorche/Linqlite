using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    using Linqlite.Sqlite;
    using System.Linq;
    public static class OperationsExtensions
    {
        public static long Insert<T>(this IQueryable<T> table, T entity) where T : SqliteEntity, new()
        {
            var provider = (LinqliteProvider)table.Provider;
            TableLite<T> query = (TableLite<T>)table ?? throw new InvalidOperationException("Tentative d'insert sur un table non définie");
            return provider.Insert<T>(entity, query.TrackingModeOverride);
        }
        public static long InsertOrGetId<T>(this IQueryable<T> table, T entity) where T : SqliteEntity, new() 
        {
            var provider = (LinqliteProvider)table.Provider;
            TableLite<T> query = (TableLite<T>)table ?? throw new InvalidOperationException("Tentative d'insert sur un table non définie");
            return provider.InsertOrGetId<T>(entity, query.TrackingModeOverride);
        }

        public static void Delete<T>(this IQueryable<T> table, T entity) where T : SqliteEntity, new()
        {
            var provider = (LinqliteProvider)table.Provider;
            provider.Delete<T>(entity);
        }

        public static void Update<T>(this IQueryable<T> table, T entity, string property) where T : SqliteEntity, new()
        {
            var provider = (LinqliteProvider)table.Provider;
            provider.Update<T>(entity, property);
        }

        public static void Update<T>(this IQueryable<T> table, T entity) where T : SqliteEntity, new()
        {
            var provider = (LinqliteProvider)table.Provider;
            provider.Update<T>(entity);
        }
    }

}
