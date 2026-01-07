using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public sealed class SqlContainsExpression : SqlExpression
    {
        public SqlExpression Source { get; }
        public SqlExpression Value { get; }
        public SqlContainsType ContainsType { get; }

        public SqlContainsExpression(SqlExpression source, SqlExpression value, SqlContainsType type)
            : base(typeof(bool))
        {
            Source = source;
            Value = value;
            ContainsType = type;
        }
    }

    public enum SqlContainsType
    {
        InList,
        Exists,
        Like
    }

}
