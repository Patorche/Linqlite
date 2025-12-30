using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    public class OrderedQueryableTable<T> : QueryableTable<T>, IOrderedQueryable<T>
    {
        public OrderedQueryableTable(IQueryProvider provider, Expression expression)
            : base(provider, expression)
        {
        }
    }

}
