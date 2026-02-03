using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    public sealed class WhereCallHandler : IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            var source = (SqlExpression)builder.Visit(node.Arguments[0]);
            builder.SetCurrentSource(source);
            /* var selectExpression = builder.GetSelectSource();
             if (selectExpression == null)
             {
                 selectExpression = SqlExpressionHelper.CreateSqlSelectExpression(node, source, builder, false);
             }*/
            // 2. Extraire le lambda
            var lambda = (LambdaExpression)builder.StripQuotes(node.Arguments[1]);

            // 3. Visiter le corps du lambda → AST SQL
            var predicate = (SqlExpression)builder.Visit(builder.StripConvert(lambda.Body));

            // 4. Ajouter la clause WHERE au SELECT
            builder.AddWhere(predicate);
           // builder.SetSelectSource(selectExpression);
            
            return source;
        }
    }

}
