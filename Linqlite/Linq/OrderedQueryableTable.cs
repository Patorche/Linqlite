using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    internal class OrderedQueryableTable<T> : TableLite<T>, IOrderedQueryable<T>
    {
        public OrderedQueryableTable(IQueryProvider provider, Expression expression)
            : base(provider, expression)
        {
        }
    }

}
