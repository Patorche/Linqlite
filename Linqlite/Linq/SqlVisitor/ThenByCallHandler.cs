using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal class ThenByCallHandler : IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            var source = (SqlExpression)builder.Visit(node.Arguments[0]);
            SqlSelectExpression selectExpression = SqlExpressionHelper.CreateSqlSelectExpression(node, source, builder, false);
            var lambda = (LambdaExpression)builder.StripQuotes(node.Arguments[1]);
            var key = (SqlExpression)builder.Visit(builder.StripConvert(lambda.Body));
            selectExpression.AddOrder(key, true);
           // builder.SetCurrentSource(selectExpression);

            return source;
        }
    }
}
