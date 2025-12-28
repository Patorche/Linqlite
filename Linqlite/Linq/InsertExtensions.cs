using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    using System.Linq;
    public static class InsertExtensions
    {
        public static void Insert<T>(this IQueryable<T> table, T entity)
        {
            var provider = (QueryProvider)table.Provider;
            provider.Insert(entity);
        }
    }

}
