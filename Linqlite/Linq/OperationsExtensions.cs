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
            var provider = (QueryProvider)table.Provider;
            QueryableTable<T> query = (QueryableTable<T>)table;
            var mode = query?.TrackingModeOverride ?? provider.DefaultTrackingMode;
            return provider.Insert<T>(entity, mode);
        }
        public static long InsertOrGetId<T>(this IQueryable<T> table, T entity) where T : SqliteEntity, new() 
        {
            var provider = (QueryProvider)table.Provider;
            QueryableTable<T> query = (QueryableTable<T>)table;
            var mode = query?.TrackingModeOverride ?? provider.DefaultTrackingMode;
            return provider.InsertOrGetId<T>(entity, mode);
        }

        public static void Delete<T>(this IQueryable<T> table, T entity) where T : SqliteEntity, new()
        {
            var provider = (QueryProvider)table.Provider;
            provider.Delete<T>(entity);
        }

        public static void Update<T>(this IQueryable<T> table, T entity, string property) where T : SqliteEntity, new()
        {
            var provider = (QueryProvider)table.Provider;
            provider.Update<T>(entity, property);
        }

        public static void Update<T>(this IQueryable<T> table, T entity) where T : SqliteEntity, new()
        {
            var provider = (QueryProvider)table.Provider;
            provider.Update<T>(entity);
        }
    }

}
