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
            return provider.Insert<T>(entity);
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
    }

}
