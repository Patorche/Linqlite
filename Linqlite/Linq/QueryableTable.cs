using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    public class QueryableTable<T> : IQueryable<T>
    {
        public Expression Expression { get; }
        public Type ElementType => typeof(T);
        public IQueryProvider Provider { get; }

        public QueryableTable(IQueryProvider provider)
        {
            Provider = provider;
            Expression = Expression.Constant(this);
        }

        public QueryableTable(IQueryProvider provider, Expression expression)
        {
            Provider = provider;
            Expression = expression;
        }

        

        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
