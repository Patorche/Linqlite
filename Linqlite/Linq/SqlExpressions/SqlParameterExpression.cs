using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlParameterExpression : SqlExpression
    {
        public object Value { get; set; }
        internal SqlParameterExpression(object value, Type type) : base(type)
        {
            Value = value;
        }
    }
}
