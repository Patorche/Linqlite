using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlInExpression : SqlExpression
    {
        public SqlExpression Item { get; }
        public IReadOnlyList<SqlExpression> Values { get; }

        public SqlInExpression(SqlExpression item, IEnumerable<SqlExpression> values) : base(typeof(bool)) 
        { 
            Item = item; 
            Values = values.ToList(); 
        }
    }
}
