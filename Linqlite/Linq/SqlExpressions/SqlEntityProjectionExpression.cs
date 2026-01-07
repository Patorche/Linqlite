using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlEntityProjectionExpression : AbstractSqlProjectionExpression
    {
        public List<SqlColumnExpression> Columns { get; set; }
        internal SqlEntityProjectionExpression(List<SqlColumnExpression> columns,  Type type): base(type)
        {
            Columns = columns;
        }
    }
}
