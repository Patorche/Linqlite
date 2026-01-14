using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Linqlite.Linq.SqlExpressions
{
    public sealed class SqlConstantExpression : SqlExpression
    {
        public object? Value { get; }

        public SqlConstantExpression(object value, Type type)
            : base(type)
        {
            if (value != null && value is string valueString)
            {
                valueString = valueString.Replace("'", "''");
                Value = valueString;
            }
            else
                Value = value;
        }
    }

}
