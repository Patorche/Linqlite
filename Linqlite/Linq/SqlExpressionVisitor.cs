using Linqlite.Attributes;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using OneOf.Types;
using SQLitePCL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using ZSpitz.Util;

namespace Linqlite.Linq
{
    public class SqlExpressionVisitor : ExpressionVisitor
    {
        private StringBuilder _sb = new();
        private bool _selectGenerated = false;
        private bool _fromGenerated = false;
        private Dictionary<Type, string> _aliases = new();
        private Dictionary<Type, string> _tables= new();
        private Dictionary<Type, EntityMap> _entityMaps = new();
        private int? _take;
        private int? _skip;
        private readonly List<string> _orderBy = new();
        private Dictionary<string, ProjectionMap> _projectionMap = new();
        private TrackingMode? _trackingMode;

        private int _aliasCounter = 0;
        private bool _projectStar = false;
        private static readonly Dictionary<string, Action<MethodCallExpression, SqlExpressionVisitor>> _handlers = new()
        {
            ["Where"] = HandleWhere,
            ["Select"] = HandleSelect,
            ["OrderBy"] = HandleOrderBy,
            ["OrderByDescending"] = HandleOrderByDescending,
            ["ThenBy"] = HandleThenBy,
            ["ThenByDescending"] = HandleThenByDescending,
            ["Take"] = HandleTake,
            ["Skip"] = HandleSkip,
            ["Contains"] = HandleContains,
            ["Join"] = HandleJoin
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

        public TrackingMode? TrackingMode => _trackingMode;

        public string Translate(Expression expression)
        {
            System.Diagnostics.Debug.WriteLine(ExpressionVisualizer.Dump(expression));
            //var visitor = new SqlExpressionVisitor();
            Visit(expression);
            EnsureSelect();
            if(_take.HasValue)
            {
                _sb.Append($" LIMIT {_take}");
            }
            if (_skip.HasValue)
            {
                if(!_take.HasValue)
                    _sb.Append($" LIMIT -1");

                _sb.Append($" OFFSET {_skip}");
            }
            return _sb.ToString();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "WithTrackingMode")
            {
                _trackingMode = (TrackingMode)((ConstantExpression)node.Arguments[1]).Value;
                return Visit(node.Arguments[0]); // on continue sans ce nœud 
            }

            if (_handlers.TryGetValue(node.Method.Name, out var handler))
            {
                handler(node, this);
                return node;
            }
            return node;
        }
        
        private static void HandleWhere(MethodCallExpression node, SqlExpressionVisitor v)
        {

            var elementType = node.Arguments[0].Type.GetGenericArguments()[0];
            if (!(node.Arguments[0] is ConstantExpression))
            {
                v.Visit(node.Arguments[0]);
            }
            var tableName = elementType.Name.ToLower();              
            
            v.EnsureFrom(elementType);
            v._sb.Append(" WHERE ");

            var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
            v.Visit(lambda.Body);
            
        }
        
        private static void HandleContains(MethodCallExpression node, SqlExpressionVisitor v)
        {
            if (node.Object != null && node.Object.Type == typeof(string))
            {
                HandleStringContains(node, v);
                return;
            }

            // CAS 2 : IEnumerable.Contains(p.FileName)
            if (node.Object != null && typeof(IEnumerable<>).IsAssignableFrom(node.Object.Type))
            {
                HandleListContains(node, v);
                return;
            }

            throw new NotSupportedException("Contains non supporté pour ce type");
        }


        private static void HandleStringContains(MethodCallExpression node, SqlExpressionVisitor v)
        {
            v._sb.Append("(");
            v.Visit(node.Object);
            v._sb.Append(" LIKE ");

            var arg = node.Arguments[0];

            if (arg is ConstantExpression c && c.Value is string s)
            {
                v._sb.Append($"'%{s}%'");
            }
            else
            {
                v.Visit(arg);            }

            v._sb.Append(")");
        }

        private static void HandleListContains(MethodCallExpression node, SqlExpressionVisitor v)
        {
            var listExpr = (ConstantExpression)node.Object;
            var listObj = listExpr.Value;
            var elementType = listObj.GetType().GetGenericArguments()[0];
            var enumerable = (IEnumerable)listObj;

            v._sb.Append("("); 
            v.Visit(node.Arguments[0]); 
            v._sb.Append(" IN ("); 
            bool first = true; 
            foreach (var item in enumerable) 
            { 
                if (!first) 
                    v._sb.Append(", "); 
                first = false;
                //v._sb.Append($"'{item}'"); 
                v._sb.Append(FormatValue(item));
            } v._sb.Append("))");
        }

      

        private static void HandleSelect(MethodCallExpression node, SqlExpressionVisitor v)
        {
            // 2. Récupérer le lambda p => ...
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);

            // 3. Construire la projection
            v._sb.Append(" SELECT ");
            
            v._selectGenerated = true;

            v.HandleProjection(lambda.Body);

            // 1. Récupérer la source (ex: photos)
            v.Visit(node.Arguments[0]);
        }

        private void HandleProjection(Expression body)
        {
            switch (body)
            {
                case MemberExpression member:
                    HandleMemberProjection(member);
                    break;

                case NewExpression nex:
                    HandleNewProjection(nex);
                    break;

                default:
                    throw new NotSupportedException($"Unsupported select expression: {body.NodeType}");
            }
        }

        private void HandleMemberProjection(MemberExpression member)
        {
            var type = member.Expression.Type;
            /*if (IsAnonymousType(type))
            {
                _projectStar = true;
                Visit(member.Expression);
                return;
            }*/

            var mappedType = EntityMap.Get(member.Type);
            if (mappedType != null)
            {
                var columns = mappedType.Columns; // ou GetColumns(member.Type)

                var alias = GetAlias(member.Type);
                _sb.Append(mappedType.Projection(alias));
                //_sb.Append(string.Join(", ", columns.Select(c => $"{alias}.{mappedType.ColumnName(c.)}")));
                return;
            }
            var map = EntityMap.Get(type);

            if (map == null)
                throw new InvalidOperationException($"Type {type.Name} is not mapped.");

            var column = GetColumnName(type, member);
            var table = map.TableName;

            _sb.Append($"{table}.{column}");

        }
      
        private void HandleNewProjection(NewExpression nex)
        {
            var parts = new List<string>();

            //foreach (var arg in nex.Arguments)
            for(int i = 0; i< nex.Arguments.Count; i++)
            {
                var arg = nex.Arguments[i];
                if (arg is MemberExpression member)
                {
                    var type = member.Expression.Type;

                    var projectedName = nex.Members[i].Name;
                    var fullPath = GetExpressionPath(member); 
                    var rootType = member.Expression.Type; 
                    var propertyName = member.Member.Name; 
                    _projectionMap[projectedName] = new ProjectionMap { 
                            ProjectedName = projectedName, 
                            FullPath = fullPath, 
                            RootType = rootType, 
                        PropertyName = propertyName 
                    };

                    var column = GetColumnName(type, member);
                    var alias = GetAlias(type);
                    parts.Add($"{alias}.{column}");
                }
                else
                {
                    throw new NotSupportedException("Unsupported projection element");
                }
            }

            _sb.Append(string.Join(", ", parts));
        }


        public static string GetExpressionPath(Expression expr)
        {
            expr = StripConvert(expr);

            var parts = new List<string>();

            while (expr is MemberExpression m)
            {
                parts.Add(m.Member.Name);
                expr = m.Expression;
            }

            parts.Reverse();
            return string.Join(".", parts);
        }



        private static void HandleOrderBy(MethodCallExpression expression, SqlExpressionVisitor v)
        {
            v.Visit(expression.Arguments[0]);
            var lambda = (LambdaExpression)StripQuotes(expression.Arguments[1]);
            v._sb.Append(" ORDER BY ");
            v.Visit(lambda.Body);
        }

        private static void HandleThenByDescending(MethodCallExpression expression, SqlExpressionVisitor v)
        {
            v.Visit(expression.Arguments[0]);
            var lambda = (LambdaExpression)StripQuotes(expression.Arguments[1]);
            v._sb.Append(", ");
            v.Visit(lambda.Body);
            v._sb.Append(" DESC");
        }

        private static void HandleThenBy(MethodCallExpression expression, SqlExpressionVisitor v)
        {
            v.Visit(expression.Arguments[0]);
            var lambda = (LambdaExpression)StripQuotes(expression.Arguments[1]);
            v._sb.Append(", ");
            v.Visit(lambda.Body);
        }

        private static void HandleOrderByDescending(MethodCallExpression node, SqlExpressionVisitor v)
        {
            HandleOrderBy(node, v);
            v._sb.Append(" DESC");
        }

        private static void HandleTake(MethodCallExpression node, SqlExpressionVisitor v)
        {
            v._take = (int)((ConstantExpression)node.Arguments[1]).Value;
            v.Visit(node.Arguments[0]);
        }

        private static void HandleSkip(MethodCallExpression node, SqlExpressionVisitor v)
        {
            v._skip = (int)((ConstantExpression)node.Arguments[1]).Value;
            v.Visit(node.Arguments[0]);
        }


        private static void HandleJoin(MethodCallExpression node, SqlExpressionVisitor v)
        {
            if (node.Arguments.Count > 0 && !(node.Arguments[0] is ConstantExpression))
                v.Visit(node.Arguments[0]);

            // 1. Récupérer les lambdas
            var outerKeyLambda = (LambdaExpression)StripQuotes(node.Arguments[2]);
            var innerKeyLambda = (LambdaExpression)StripQuotes(node.Arguments[3]);

            // 2. Déterminer le type réel de la table "outer"
            //    Ce n'est PAS le type du paramètre (q), mais le type du premier membre accédé (q.p)
            var outerEntityType = v.GetEntityType(StripConvert(outerKeyLambda.Body));

            // 3. Déterminer le type de la table "inner"
            var innerEntityType = v.GetEntityType(StripConvert(innerKeyLambda.Body)); //node.Arguments[1].Type.GetGenericArguments()[0];

            v.EnsureFrom(outerEntityType);

            // 4. Récupérer les alias
            var outerAlias = v.GetAlias(outerEntityType);
            var innerAlias = v.GetAlias(innerEntityType);

            // 5. Écrire le JOIN
            var tableName = v.GetTableName(innerEntityType);
            v._sb.Append($" JOIN {tableName} {innerAlias} ON ");

            // 6. Générer la clé outer
            var outerExpr = new AliasReplacer(outerKeyLambda.Parameters[0], outerAlias)
                .Visit(StripConvert(outerKeyLambda.Body));

            v.Visit(outerExpr);

            v._sb.Append(" = ");

            // 7. Générer la clé inner
            var innerExpr = new AliasReplacer(innerKeyLambda.Parameters[0], innerAlias)
                .Visit(StripConvert(innerKeyLambda.Body));

            v.Visit(innerExpr);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            // Parenthèses pour éviter les ambiguïtés
            _sb.Append("(");

            Visit(node.Left);

            if (_binaryOperators.TryGetValue(node.NodeType, out var op))
                _sb.Append(op);
            else
                throw new NotSupportedException($"Opérateur binaire non supporté : {node.NodeType}");

            Visit(node.Right);

            _sb.Append(")");

            return node;
        }


        protected override Expression VisitMember(MemberExpression node)
        {
            string column = "";
            Type type;
            node = (MemberExpression)StripConvert(node);
            var localPath = GetExpressionPath(node);
            if (_projectionMap.TryGetValue(localPath, out var projMap))
            {
                type = projMap.RootType;
                column = GetColumnName(type, projMap.PropertyName);
            }
            else
            {

                type = GetEntityType(node);
                column = GetColumnName(type, node);
            }
            var alias = GetAlias(type);           
            _sb.Append($"{alias}.{column}");
            return node;

        }


        protected override Expression VisitParameter(ParameterExpression expression)
        {
            return base.VisitParameter(expression);
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q)
            {
                var entityType = q.ElementType; // <== Photo, GpsLocalisation, etc.
                EnsureFrom(entityType);
                return node; 
            }
            
            if (node.Value is string s && s.Contains('.'))
            {
                // C’est un alias SQL, on l’écrit tel quel
                _sb.Append(s);
                return node;
            }

            // Sinon, comportement normal
            if (node.Type == typeof(string))
                _sb.Append($"'{node.Value}'");
            else
                _sb.Append(node.Value);

            return node;
        }


        protected override Expression VisitExtension(Expression node) 
        { 
            if (node is SqlExpression sql) 
            { 
                _sb.Append(sql.Sql); 
                return node; 
            }
            return base.VisitExtension(node); 
        }

        private static string FormatValue(object item)
        {
            if (item == null) return "NULL";

            return item switch
            {
                string s => $"'{s.Replace("'", "''")}'",
                Guid g => $"'{g}'",
                DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'",
                bool b => b ? "1" : "0",
                _ when item.GetType().IsEnum => Convert.ToInt32(item).ToString(),
                _ => $"{item}"
            };
        }

        private string GetAlias(Type t)
        {
            if (_aliases.TryGetValue(t, out var alias))
                return alias;

            alias = "t" + _aliasCounter++;
            _aliases[t] = alias;
            return alias;
        }

        private void EnsureSelect()
        {
            if (_selectGenerated)
                return;

            _sb.Insert(0, "SELECT * ");
            _selectGenerated = true;
        }

        private void EnsureFrom(Type tableType)
        {
            if (_fromGenerated)
                return;

            var alias = GetAlias(tableType);
            var tablename = GetTableName(tableType);
            _sb.Append($" FROM {tablename} {alias} ");
            _fromGenerated = true;
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }

        private Type GetEntityType(Expression expr)
        {
            // 1. Remonter jusqu'au paramètre racine
            // Ici. On peut avoir qqchose comme p.q.Property
            // Deux cas : soit q correspond à une propriété de la classe asociée à p qui n'est pas associée à un table et on doit remonter jusqu'à p.
            // Soit, qu correspond bien à une table  et on ss'arrête là.
            while (expr is MemberExpression m && !IsTable(m.Type)) // string.IsNullOrEmpty(GetTableName(m.Type)))
                expr = m.Expression;

            if (expr == null)
                throw new InvalidOperationException("Impossible de trouver le paramètre racine.");

            var t = expr.Type;

            // 2. Si c'est un type anonyme, on prend le premier type générique
            if (IsAnonymousType(t) && t.IsGenericType)
                return t.GetGenericArguments()[0];

            // 3. Sinon, on retourne le type tel quel
            return t;
        }

        private bool IsTable(Type type)
        {
            if(EntityMap.Get(type) == null || !EntityMap.Get(type).IsFromTable) return false;
            return true;
        }

        private  string GetTableName(Type type)
        {
            return EntityMap.Get(type).TableName;
        }


        private string GetColumnName(Type type, MemberExpression expression)
        {
            // récupération du path 
            string path = "";
            Expression exp = expression;
            while (exp is MemberExpression m)
            {
                path = m.Member.Name + (string.IsNullOrEmpty(path) ? "" : ".") + path;
                if (IsAnonymousType(m.Expression.Type) || IsTable(m.Expression.Type))
                    break;
                exp = m.Expression;
            }
            return EntityMap.Get(type).Column(path);
        }

        private string GetColumnName(Type type, string propertyName)
        {
            // récupération du path 
            return EntityMap.Get(type).Column(propertyName);
        }

        private static bool IsAnonymousType(Type t)
        {
            return Attribute.IsDefined(t, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
                   && t.Name.Contains("AnonymousType")
                   && t.IsGenericType;
        }

        private static Expression StripConvert(Expression expr)
        {
            while (expr.NodeType == ExpressionType.Convert ||
                   expr.NodeType == ExpressionType.ConvertChecked)
            {
                expr = ((UnaryExpression)expr).Operand;
            }
            return expr;
        }


        private void v(Expression e) => Visit(e);
    }

}
