using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal class GroupByCallHandler : IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            var source = builder.Visit(node.Arguments[0]) as SqlExpression;

            if (builder.GetSelectSource() == null)
            {
                SqlSelectExpression _sqlSelect = new SqlSelectExpression(source.Type)
                {
                    From = (SqlSourceExpression)source
                };
                builder.SetSelectSource(_sqlSelect);
            }
            builder.SetCurrentSource(source);
            var originalKeySelector = (LambdaExpression)builder.StripQuotes(node.Arguments[1]);
            bool originalTypeIsEntity = false;
            LambdaExpression keySelector = originalKeySelector;
            if (builder.IsMappedEntity(keySelector.ReturnType))
            {
                keySelector = ReplaceEntityKeySelector(originalKeySelector);
                originalTypeIsEntity = true;
            }


            LambdaExpression? elementSelector = null;
            if (node.Arguments.Count > 2)
            {
                elementSelector = (LambdaExpression)builder.StripQuotes(node.Arguments[2]);
            }
            //var groupingInterface = typeof(IGrouping<,>).MakeGenericType(originalKeySelector.ReturnType, builder.GetSelectSource().ElementType);
            //var returnType =  // typeof(IEnumerable<>).MakeGenericType(groupingInterface);

            return new SqlGroupByExpression(builder.GetSelectSource(), keySelector, elementSelector, originalTypeIsEntity, originalKeySelector, builder.GetSelectSource().ElementType);
        }

        private LambdaExpression ReplaceEntityKeySelector(LambdaExpression lambda)
        {
            var param = lambda.Parameters[0];
            var entityExpr = lambda.Body; // x.Photo

            // Récupérer la propriété PK
            var pkProp = EntityMap.Get(entityExpr.Type).GetPrimaryKey().PropertyInfo;

            // Construire x.Photo.Id
            var pkAccess = Expression.Property(entityExpr, pkProp);

            return Expression.Lambda(pkAccess, param);
        }

    }
}
