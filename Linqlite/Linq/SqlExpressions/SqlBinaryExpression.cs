using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public sealed class SqlBinaryExpression : SqlExpression
    {
        public SqlExpression Left { get; }
        public string Operator { get; }
        public SqlExpression Right { get; }

        public SqlBinaryExpression(SqlExpression left, string op, SqlExpression right)
            : base(typeof(bool))
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

}
