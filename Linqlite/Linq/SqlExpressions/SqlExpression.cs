using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public class SqlExpression : Expression
    {
        public override Type Type { get; }
        protected SqlExpression(Type type) => Type = type;
        public override ExpressionType NodeType => ExpressionType.Extension;
    }
}
