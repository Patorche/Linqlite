using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlEnumerableExpression : SqlExpression
    {
        public IReadOnlyList<SqlExpression> Values { get; }

        public SqlEnumerableExpression(IEnumerable<SqlExpression> list, Type type) : base(type)
        {
            Values = list.ToList();
        }
    }
}
