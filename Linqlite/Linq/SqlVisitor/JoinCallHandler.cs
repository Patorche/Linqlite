using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal class JoinCallHandler : AbstractSourceHandler, IMethodCallHandler
    {
        /*  public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
          {
              var left = (SqlExpression)builder.Visit(node.Arguments[0]);
              var right = (SqlExpression)builder.Visit(node.Arguments[1]);

              var alias = builder.GetNextAlias();
              var join = new SqlJoinExpression(left, right, "INNER", alias);
              builder.SetCurrentSource(join);

              var outerKeyLambda = (LambdaExpression)builder.StripQuotes(node.Arguments[2]);
              var innerKeyLambda = (LambdaExpression)builder.StripQuotes(node.Arguments[3]);



              var leftKey = (SqlExpression)builder.Visit(builder.StripConvert(outerKeyLambda.Body));
              var rightKey = (SqlExpression)builder.Visit(builder.StripConvert(innerKeyLambda.Body));

              var on = new SqlBinaryExpression(leftKey, "=", rightKey);
              join.SetOn(on);


              // 6. Extraire le resultSelector
              var resultSelector = (LambdaExpression)builder.StripQuotes(node.Arguments[4]);

              // 7. Visiter la projection
              var projection = HandleProjection(builder.StripConvert(resultSelector.Body), builder);

              // 8. Appliquer la projection à la jointure
              //join.SetProjection(projection);

              return join;
          }*/

        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            // arguments : source, inner, outerKeySelector, innerKeySelector, resultSelector
            var outerSource = (SqlSourceExpression)builder.Visit(node.Arguments[0]);
            var innerSource = (SqlSourceExpression)builder.Visit(node.Arguments[1]);

            var outerKeyLambda = (LambdaExpression)builder.StripQuotes(node.Arguments[2]);
            var innerKeyLambda = (LambdaExpression)builder.StripQuotes(node.Arguments[3]);
            //var resultSelector = (LambdaExpression)builder.StripQuotes(node.Arguments[4]);

            // clé gauche
            builder.SetCurrentSource(outerSource);
            var outerKey = (SqlExpression)builder.Visit(builder.StripConvert(outerKeyLambda.Body));

            // clé droite
            builder.SetCurrentSource(innerSource);
            var innerKey = (SqlExpression)builder.Visit(builder.StripConvert(innerKeyLambda.Body));

            // ON clause
            var on = new SqlBinaryExpression(outerKey, "=", innerKey);

            // join
            var alias = outerSource.Alias; // builder.GetNextAlias();
            /*var joinElementType = resultSelector.Parameters.Count == 2
                ? typeof(ValueTuple<,>).MakeGenericType(
                    resultSelector.Parameters[0].Type,
                    resultSelector.Parameters[1].Type)
                : typeof(object); // ou autrem
            */

            var join = new SqlJoinExpression(
                outerSource,
                innerSource,
                on,
                SqlJoinType.Inner,
                alias,
                outerSource.Type);

            // SELECT avec projection du resultSelector
            builder.SetCurrentSource(join);
           
            return join;
        }
    }
}
