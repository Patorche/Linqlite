using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal static class SqlExpressionHelper
    {
        internal static SqlSelectExpression CreateSqlSelectExpression(MethodCallExpression node, SqlExpression source, SqlTreeBuilderVisitor builder,bool typeChanges)
        {
            SqlSelectExpression selectExpression;

            if (source is SqlSelectExpression s)
            {
                selectExpression = s;
            }
            else if (builder.GetSelectSource() != null)
            {
                return builder.GetSelectSource();
            }
            else
            {
                var resultType = typeChanges ? node.Type : source.Type;
                selectExpression = new SqlSelectExpression(resultType)
                {
                    From = (SqlSourceExpression)source
                };
            }
            if (builder.GetCurrentSource() == null)
                builder.SetCurrentSource(selectExpression);
            builder.SetSelectSource(selectExpression);
            return selectExpression;
        }
    }
}
