using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public sealed class SqlColumnExpression : SqlExpression
    {
        public string Alias { get; }
        public string Column { get; }

        public SqlColumnExpression(string alias, string column, Type type)
            : base(type)
        {
            Alias = alias;
            Column = column;
        }
    }
}
