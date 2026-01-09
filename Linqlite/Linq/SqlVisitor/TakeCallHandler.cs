using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal class TakeCallHandler : IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            var source = (SqlExpression)builder.Visit(node.Arguments[0]);
            SqlSelectExpression selectExpression = SqlExpressionHelper.CreateSqlSelectExpression(node, source, builder, false);
            var o = builder.StripQuotes(node.Arguments[1]) ?? throw new InvalidDataException("Limit ne peut être null");
            var c = o as ConstantExpression;
            if(c?.Value == null) throw new InvalidDataException("Limit ne peut être null");
            var limit = (int)c.Value;
            selectExpression.Limit = limit;
            return source;
        }
    }
}
