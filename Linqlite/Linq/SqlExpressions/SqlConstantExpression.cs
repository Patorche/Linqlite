using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public sealed class SqlConstantExpression : SqlExpression
    {
        public object Value { get; }

        public SqlConstantExpression(object value, Type type)
            : base(type)
        {
            Value = value;
        }
    }

}
