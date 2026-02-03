using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
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
            else if (builder.GetSelectSource() is { } ss)
            {
                return ss;
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

        internal static List<SqlColumnExpression> GetFullProjection(Type type, string alias)
        {
            var map = EntityMap.Get(type) ?? throw new InvalidDataException("Entité null retournée");
            var members = new List<SqlColumnExpression>();
            foreach (var col in map.Columns)
            {
                if (string.IsNullOrEmpty(col.ColumnName))
                {
                    var submembers = new List<SqlColumnExpression>();
                    submembers = GetFullProjection(col.PropertyType, alias);
                    members.AddRange(submembers);
                }
                else
                {
                    var column = new SqlColumnExpression(alias, col.ColumnName, col.PropertyType);
                    members.Add(column);
                }
            }
            return members;
        }
    }
}
