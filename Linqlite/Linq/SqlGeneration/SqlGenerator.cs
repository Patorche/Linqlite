using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Linq.SqlGeneration
{
    public sealed class SqlGenerator 
    { 
        private StringBuilder _sb = new();
        private int _aliasParam = 0;
        private Dictionary<string, object> _parameters = new();
        private bool _hasProjection = false;
        private List<SqlTableExpression> _tables = new();

        public IReadOnlyDictionary<string, object> Parameters { get => _parameters; }
        public string Generate(SqlExpression source) 
        {
            Visit(source);
            EnsureSelect();
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
               /* case SqlWithRelationsExpression :
                    Visit((SqlExpression)((SqlWithRelationsExpression)source).Source);
                    break;*/
                default: throw new NotSupportedException($"Source non supportée : {source.GetType()}" );
            }
        }


        private void VisitExpression(SqlExpression? expr)
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
                    throw new NotSupportedException($"Expression non supportée : {expr?.GetType()}");
            }
        }

        private void VisitContains(SqlContainsExpression con)
        {
            _sb.Append('(');
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
        }



        /*
         * Sources et Select
         */

        private void VisitTable(SqlTableExpression source)
        {
            _tables.Add(source);
            _sb.Append($"{source.TableName} {source.Alias}");
        }

        private void VisitSelect(SqlSelectExpression source)
        {
            // Projection
            if (source.Projection != null)
            {
                _hasProjection = true;
                _sb.Append("SELECT ");
                VisitExpression(source.Projection);
            }

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
            if (source.JoinType == SqlJoinType.LeftOuter)
            {
                _sb.Append(" LEFT");
            }
            _sb.Append(" JOIN ");
            Visit(source.Right);
            _sb.Append(" ON ");
            VisitExpression(source.On);
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
                if (col.Value.Item2 is SqlColumnExpression)
                {
                    SqlColumnExpression colExpr = (SqlColumnExpression)col.Value.Item2;
                    _sb.Append($"{colExpr.Alias}.{colExpr.Column} AS {colExpr.Alias}_{colExpr.Column}");
                }
                else if(col.Value.Item2 is SqlEntityProjectionExpression)
                {
                    VisitEntityProjection((SqlEntityProjectionExpression)col.Value.Item2);
                }
                else if(col.Value.Item2 is SqlEntityReferenceExpression r)
                {
                    _sb.Append(EntityMap.Get(r.Type).Projection(r.Alias));
                    
                }
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
                _sb.Append($"{col.Alias}.{col.Column} AS {col.Alias}_{col.Column}");
            }
        }

        private void VisitIn(SqlInExpression inExpr)
        {
            throw new NotImplementedException();
        }

        private void VisitBinary(SqlBinaryExpression bin)
        {
            _sb.Append('(');
            VisitExpression(bin.Left);
            _sb.Append($" {bin.Operator} ");
            VisitExpression(bin.Right);
            _sb.Append(')');
        }

        private void VisitUnary(SqlUnaryExpression un)
        {
            _sb.Append('(');
            _sb.Append($"{un.Operator} ");
            VisitExpression(un.Operand);
            _sb.Append(')');
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

            if (par.Value != null && par.Value is string  valueString) 
            {
                valueString = valueString.Replace("'", "''");
                _parameters.Add(name, valueString);
            }
            else
                _parameters.Add(name, par.Value ?? DBNull.Value);
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

        private void EnsureSelect()
        {
            if (_hasProjection)
                return;

            if (_tables.Count == 0)
                throw new InvalidOperationException("Impossibkle de générer la projection de la requête. ");

            StringBuilder select = new StringBuilder();
            select.Append("SELECT ");
            var i = 0;
            foreach (var j in _tables)
            {
                if (i > 0) 
                {  
                    select.Append(", "); 
                }
                var p = EntityMap.Get(j.Type).Projection(j.Alias);
                select.Append(p);
                i++;
            }
            select.Append(_sb);
            _sb = select;
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

            Type? type = item.GetType();
            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    DateTime? date = item as DateTime?;
                    object? strDate = date?.ToString("yyyy-MM-dd HH:mm:ss");
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
