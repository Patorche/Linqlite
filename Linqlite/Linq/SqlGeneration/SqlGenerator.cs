using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Linq.SqlGeneration
{
    public sealed class SqlGenerator 
    { 
        private readonly StringBuilder _sb = new();
        private int _aliasParam = 0;
        private Dictionary<string, object> _parameters = new();

        public IReadOnlyDictionary<string, object> Parameters { get => _parameters; }
        public string Generate(SqlExpression source) 
        {
            Visit(source);
            return _sb.ToString(); 
        }

        private void Visit(SqlExpression source)
        {
            switch (source)
            {
                case SqlSelectExpression:
                    VisitSelect((SqlSelectExpression)source);
                    break;
                case SqlTableExpression : 
                    VisitTable((SqlTableExpression)source); 
                    break;
                case SqlJoinExpression :
                    VisitJoin((SqlJoinExpression)source);
                    break;
                default: throw new NotSupportedException($"Source non supportée : {source.GetType()}" );
            }
        }


        private void VisitExpression(SqlExpression expr)
        {
            switch (expr)
            {
                case SqlColumnExpression col:
                    VisitColumn(col);
                    break;

                case SqlConstantExpression c:
                    VisitConstant(c);
                    break;

                case SqlBinaryExpression bin:
                    VisitBinary(bin);
                    break;

                case SqlUnaryExpression un:
                    VisitUnary(un);
                    break;

                case SqlInExpression inExpr:
                    VisitIn(inExpr);
                    break;

                case SqlMemberProjectionExpression proj:
                    VisitMemberProjection(proj);
                    break;

                case SqlEntityProjectionExpression eproj:
                    VisitEntityProjection(eproj);
                    break;

                case SqlContainsExpression con:
                    VisitContains(con);
                    break;

                case SqlParameterExpression par:
                    VisitParameter(par);
                    break;

                case SqlEnumerableExpression enu:
                    VisitEnumerable(enu);
                    break;

                default:
                    throw new NotSupportedException($"Expression non supportée : {expr.GetType()}");
            }
        }

        private void VisitContains(SqlContainsExpression con)
        {
            _sb.Append("(");
            VisitExpression(con.Source);
            var op = con.ContainsType switch
            {
                SqlContainsType.Like => "LIKE",
                SqlContainsType.InList => "IN",
                _ => throw new NotSupportedException($"Opérateur non supporté : {con.ContainsType}")
            };
            _sb.Append($" {op} ");
            VisitExpression(con.Value);
            _sb.Append(")");
            int i = 0;
        }



        /*
         * Sources et Select
         */

        private void VisitTable(SqlTableExpression source)
        {
            _sb.Append($"{source.TableName} {source.Alias}");
        }

        private void VisitSelect(SqlSelectExpression source)
        {
            _sb.Append("SELECT ");

            // Projection
            if (source.Projection != null)
                VisitExpression(source.Projection);
            else
                _sb.Append(source.From.Alias).Append(".*");

            _sb.AppendLine();
            _sb.Append("FROM ");
            Visit(source.From);

            // WHERE
            if (source.Where != null)
            {
                _sb.AppendLine();
                _sb.Append(" WHERE ");
                VisitExpression(source.Where);
            }

            // ORDER BY
            if (source.Orders.Any())
            {
                _sb.AppendLine();
                _sb.Append(" ORDER BY ");
                VisitOrders(source.Orders);
            }

            // LIMIT / OFFSET
            if (source.Limit != null || source.Offset != null)
            {
                _sb.AppendLine();
                VisitLimitOffset(source);
            }
        }

        

        private void VisitJoin(SqlJoinExpression source)
        {
            Visit(source.Left);
            _sb.Append(" JOIN ");
            Visit(source.Right);
            _sb.Append(" ON ");
            VisitExpression(source.On);
            int i = 0;
        }

        /*
         * Expressions
         */

        private void VisitColumn(SqlColumnExpression col)
        {
            _sb.Append($"{col.Alias}.{col.Column}");
        }
        private void VisitMemberProjection(SqlMemberProjectionExpression proj)
        {
            int i = 0;

            foreach (var col in proj.Columns)
            {
                if (i > 0)
                { 
                    _sb.Append(", ");
                }
                SqlColumnExpression colExpr = (SqlColumnExpression)col.Value;
                _sb.Append($"{colExpr.Alias}.{colExpr.Column}");
                i++;
            }
        }

        private void VisitEntityProjection(SqlEntityProjectionExpression proj)
        {
            for(int i = 0; i< proj.Columns.Count ; i++)
            {
                if (i > 0)
                {
                    _sb.Append(", ");
                }
                var col = proj.Columns[i];
                _sb.Append($"{col.Alias}.{col.Column}");
            }
        }

        private void VisitIn(SqlInExpression inExpr)
        {
            throw new NotImplementedException();
        }

        private void VisitBinary(SqlBinaryExpression bin)
        {
            _sb.Append("(");
            VisitExpression(bin.Left);
            _sb.Append($" {bin.Operator} ");
            VisitExpression(bin.Right);
            _sb.Append(")");
        }

        private void VisitUnary(SqlUnaryExpression un)
        {
            _sb.Append("(");
            _sb.Append($"{un.Operator} ");
            VisitExpression(un.Operand);
            _sb.Append(")");
        }

        private void VisitConstant(SqlConstantExpression c)
        {
            //throw new NotImplementedException();
            if (c.Type == typeof(string))
            {
                _sb.Append($"'{c.Value}'");
            }
            else
            {
                _sb.Append(GetValue(c.Value));
            }
        }

        private void VisitLimitOffset(SqlSelectExpression source)
        {
            if (source.Limit != null)
            {
                _sb.Append($" LIMIT {source.Limit}");
            }
            if (source.Offset != null)
            {
                if (source.Limit== null)
                    _sb.Append($" LIMIT -1");

                _sb.Append($" OFFSET {source.Offset}");
            }
        }

        private void VisitOrders(IReadOnlyList<SqlOrderByExpression> orders)
        {
            for(int i = 0; i < orders.Count; i++)
            {
                if (i > 0)
                    _sb.Append(", ");
                VisitExpression(orders[i].Key);
                _sb.Append(orders[i].Ascending ? " ASC" : " DESC");
            }
        }

        private void VisitParameter(SqlParameterExpression par)
        {
            var name = GetNextAliasParameter();
            _parameters.Add(name, par.Value);
            _sb.Append(name);
        }

        private void VisitEnumerable(SqlEnumerableExpression enu)
        {
            _sb.Append("(");
            for(int i = 0; i<enu.Values.Count; i++)
            {
                if (i > 0)
                    _sb.Append(", ");
                //_sb.Append(GetValue(enu.Values[i]));
                VisitExpression(enu.Values[i]);
            }
            _sb.Append(")");
        }


        private string GetNextAliasParameter()
        {
            return $"@v{_aliasParam++}"; 
        }

        private static object GetValue(object item)
        {
            if(item == null)
            {
                return "NULL";
            }

            Type type = item.GetType();
            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    DateTime? date = (DateTime)item;
                    object strDate = date?.ToString("yyyy-MM-dd HH:mm:ss");
                    return "'" + (strDate ?? DBNull.Value) + "'";
                case TypeCode.Boolean:
                    return (bool)item ? "TRUE" : "FALSE";
                case TypeCode.String:
                    return "'" + item + "'";

                default:
                    return item ?? DBNull.Value;

            }

        }
    }
}
