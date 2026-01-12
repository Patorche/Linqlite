using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;


namespace Linqlite.Linq.SqlVisitor
{
    public class SqlTreeBuilderVisitor : ExpressionVisitor
    {
        private SqlExpression? _sqlSource;
        private SqlSelectExpression? _sqlSelect;
        private int _aliasNumber;
        private static readonly Dictionary<string, IMethodCallHandler> _methodBuilders = new()
        {
            ["Where"] = new WhereCallHandler(),
            ["Select"] = new SelectCallHandler(),
            ["OrderBy"] = new OrderByCallHandler(),
            ["OrderByDescending"] = new OrderByDescendingCallHandler(),
            ["ThenBy"] = new ThenByCallHandler(),
            ["ThenByDescending"] = new ThenByDescendingCallHandler(),
            ["Take"] = new TakeCallHandler(),
            ["Skip"] = new SkipCallHandler(),
            ["Contains"] = new ContainsCallHandler(),
            ["Join"] = new JoinCallHandler()
        };

        private static readonly Dictionary<ExpressionType, string> _binaryOperators = new()
        {
            [ExpressionType.Equal] = " = ",
            [ExpressionType.NotEqual] = " <> ",
            [ExpressionType.LessThan] = " < ",
            [ExpressionType.LessThanOrEqual] = " <= ",
            [ExpressionType.GreaterThan] = " > ",
            [ExpressionType.GreaterThanOrEqual] = " >= ",
            [ExpressionType.AndAlso] = " AND ",
            [ExpressionType.OrElse] = " OR "
        };

        public Dictionary<string, object> Parameters = new();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
  
            if (_methodBuilders.TryGetValue(node.Method.Name, out var handler))
                return handler.Handle(node, this);

            return base.VisitMethodCall(node);
        }



        public SqlExpression Build(Expression expression)
        {
            // point d’entrée
            SqlExpression sqlExpr = (SqlExpression)Visit(expression);
            if(_sqlSelect == null)
            {
                _sqlSelect = new SqlSelectExpression(sqlExpr.Type)
                {
                    From = (SqlSourceExpression)sqlExpr
                };
            }
            return _sqlSelect!;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (IsCapturedValue(node))
            {
                var value = EvaluateCapturedValue(node);
                return new SqlParameterExpression(value, node.Type);
            }


            var memberDeclaringType = node.Member.DeclaringType;

            // Cas 1 : la source courante est une table simple
            if (_sqlSource is SqlSelectExpression select)
            {
                // On regarde si on est en train de projjeter une entité complète.
                if (IsMappedEntity(node.Type))
                {
                    return new SqlEntityReferenceExpression(select.From.Alias, node.Type);
                }
                else
                {
                    // On regarde si le membre est mappé à une colonne du select
                    var colName = GetColumnName(_sqlSource.Type, node);
                    //if (select.Type == memberDeclaringType)
                    if (!string.IsNullOrEmpty(colName))
                        return new SqlColumnExpression(((SqlSelectExpression)select).From.Alias, colName, node.Type);
                }
            }

            // Cas 2 : la source courante est une jointure
            if (_sqlSource is SqlJoinExpression join)
            {
                if (join.Left.Type == memberDeclaringType)
                    return new SqlColumnExpression(((SqlSourceExpression)join.Left).Alias, GetColumnName(join.Left.Type, node), node.Type);

                if (join.Right.Type == memberDeclaringType)
                    return new SqlColumnExpression(((SqlSourceExpression)join.Right).Alias, GetColumnName(join.Right.Type, node), node.Type);
            }

            if (_sqlSource is SqlTableExpression table)
            {
                if(table.Type == memberDeclaringType)
                {
                    return new SqlColumnExpression(((SqlTableExpression)table).Alias, GetColumnName(table.Type, node), node.Type);
                }
            }

            if (node.Expression is MemberExpression inner)
            {
                var target = EvaluateMemberExpression(inner);
                var value = GetMemberValue(target, node.Member);
                return new SqlParameterExpression(value, node.Type);
            }

            var projection = GetSelectSource()?.Projection;
            if (projection != null)
            {
                Type projType = projection.Type ?? throw new UnreachableException("Un erreur a été rencontrée lors du parcours de l'arbre");
                if (memberDeclaringType == projType)
                {
                    if (_sqlSelect?.Projection is SqlMemberProjectionExpression proj)
                    {
                        if (proj.Columns.TryGetValue(node.Member, out var exp))
                        {
                            return exp;
                        }
                    }
                }
            }

            return base.VisitMember(node);
        }

        private bool IsMappedEntity(Type type)
        {
            var map = EntityMap.Get(type);
            return map != null;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _sqlSource ?? throw new UnreachableException("Un erreur a été rencontrée lors du parcours de l'arbre : _sqlSource est null");
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q) 
            { 
                var entityType = q.ElementType; 
                var map = EntityMap.Get(entityType) ?? throw new UnreachableException("EnityMap est null");
                var tableName = map.TableName; 
                var alias = GetNextAlias(); 
                return new SqlTableExpression(tableName, alias, entityType); 
            }
            

            return new SqlConstantExpression(node.Value ?? DBNull.Value, node.Type);
              
        }

        /*private SqlExpression? CreateSelectForTable(Type elementType)
        {
            var map = EntityMap.Get(elementType);
            SqlTableExpression table = new SqlTableExpression(map.TableName, GetNextAlias(), elementType);
            return new SqlSelectExpression(elementType) { From = table };
        }*/

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
            { 
                return Visit(node.Operand); 
            }

            if (node.NodeType == ExpressionType.Not)
                return new SqlUnaryExpression("NOT", (SqlExpression)Visit(node.Operand));

            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = (SqlExpression)Visit(node.Left);
            var right = (SqlExpression)Visit(node.Right);
            var op = GetSqlOperator(node.NodeType);

            return new SqlBinaryExpression(left, op, right);
        }

        internal Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }

        internal Expression StripConvert(Expression expr)
        {
            while (expr.NodeType == ExpressionType.Convert ||
                   expr.NodeType == ExpressionType.ConvertChecked)
            {
                expr = ((UnaryExpression)expr).Operand;
            }
            return expr;
        }

        private static string GetSqlOperator(ExpressionType type)
        {
            var op = type switch
            {
                ExpressionType.Not => "NOT",
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.And => "AND",
                ExpressionType.OrElse => "OR",
                ExpressionType.Or => "OR",
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                _ => throw new InvalidOperationException($"Opérateur non géré : {type}")
            };
            return op;
        }

        private string GetColumnName(Type type, MemberExpression expression)
        {
            // récupération du path 
            string path = "";
            Expression? exp = expression;
            while (exp is MemberExpression m)
            {
                path = m.Member.Name + (string.IsNullOrEmpty(path) ? "" : ".") + path;
                if (m.Expression != null && (IsAnonymousType(m.Expression.Type) || IsTable(m.Expression.Type)))
                    break;
                exp = m?.Expression;
            }
            var map = EntityMap.Get(type);
            if (map == null)
            {
                return ""; 
            }
            return map.Column(path);
            
        }

        internal string GetNextAlias()
        {
            return $"t{_aliasNumber++}";
        }

        internal void SetCurrentSource(SqlExpression? join)
        {
            _sqlSource = join;
        }

        private static bool IsAnonymousType(Type t)
        {
            return Attribute.IsDefined(t, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
                   && t.Name.Contains("AnonymousType")
                   && t.IsGenericType;
        }

        private bool IsTable(Type type)
        {
            var map = EntityMap.Get(type);
            if (map == null || !map.IsFromTable) return false;
            return true;
        }

        internal SqlExpression? GetCurrentSource()
        {
            return _sqlSource;
        }

        internal void SetSelectSource(SqlSelectExpression selectExpression)
        {
            _sqlSelect = selectExpression;
        }

        internal SqlSelectExpression? GetSelectSource()
        {
            return _sqlSelect;
        }

        private static bool IsCapturedValue(MemberExpression node)
        {
            return GetRootExpression(node) is ConstantExpression;
        }

        private static Expression GetRootExpression(Expression expr)
        {
            while (expr is MemberExpression me)
                expr = me.Expression ?? throw new UnreachableException("Expression null.");

            return expr;
        }


        private static bool IsClosureAccess(MemberExpression node)
        {
            return node.Expression is ConstantExpression c &&
                   node.Member is FieldInfo f &&
                   c.Value != null &&
                   IsClosureClass(c.Value.GetType());
        }

        private static bool IsClosureClass(Type type)
        {
            return type.IsNestedPrivate && type.Name.Contains("DisplayClass");
        }

        private static object? EvaluateCapturedValue(MemberExpression node)
        {
            // On remonte jusqu'à la racine
            var stack = new Stack<MemberExpression>();
            Expression? expr = node;

            while (expr is MemberExpression me)
            {
                stack.Push(me);
                expr = me?.Expression;
            }

            // expr est maintenant un ConstantExpression
            var constant = expr as ConstantExpression;
            object? value = constant?.Value;

            // On applique chaque MemberInfo dans l'ordre
            while (stack.Count > 0)
            {
                var m = stack.Pop().Member;

                if (m is FieldInfo fi)
                    value = fi.GetValue(value);
                else if (m is PropertyInfo pi)
                    value = pi.GetValue(value);
                else
                    throw new NotSupportedException("Unsupported member type");
            }

            return value;
        }

        private static object? EvaluateMemberExpression(MemberExpression node)
        {
            if (node.Expression is ConstantExpression c)
                return GetMemberValue(c.Value, node.Member);

            if (node.Expression is MemberExpression inner)
            {
                var target = EvaluateMemberExpression(inner);
                return GetMemberValue(target, node.Member);
            }

            throw new NotSupportedException("Unsupported captured variable structure");
        }
        private static object? GetMemberValue(object? target, MemberInfo member)
        {
            return member switch
            {
                FieldInfo f => f.GetValue(target),
                PropertyInfo p => p.GetValue(target),
                _ => throw new NotSupportedException($"Unsupported member: {member}")
            };
        }

        private static bool HasPredicateOverload(string methodName, int argCount)
        {
            // Méthodes terminales qui ont une version avec prédicat
            return argCount == 2 && methodName switch
            {
                "Single" => true,
                "SingleOrDefault" => true,
                "First" => true,
                "FirstOrDefault" => true,
                "Any" => true,
                "Count" => true,
                _ => false
            };
        }

    }
}
