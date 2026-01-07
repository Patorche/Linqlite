using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public sealed class SqlUnaryExpression : SqlExpression
    {
        public string Operator { get; }
        public SqlExpression Operand { get; }

        public SqlUnaryExpression(string op, SqlExpression operand)
            : base(typeof(bool))
        {
            Operator = op;
            Operand = operand;
        }
    }
}
