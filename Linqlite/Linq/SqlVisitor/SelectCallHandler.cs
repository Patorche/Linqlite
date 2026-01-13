using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal class SelectCallHandler : AbstractSourceHandler, IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            var source = (SqlExpression)builder.Visit(node.Arguments[0]);
            SqlSelectExpression selectExpression = SqlExpressionHelper.CreateSqlSelectExpression(node, source, builder, true);
            var oldSource = builder.GetCurrentSource();
            builder.SetCurrentSource(source);
            
            var lambda = (LambdaExpression)builder.StripQuotes(node.Arguments[1]); 
            var projection = HandleProjection(lambda.Body, builder, selectExpression); 
            selectExpression.SetProjection(projection); 
            builder.SetCurrentSource(oldSource); 
            return source;
        }
    }
}
