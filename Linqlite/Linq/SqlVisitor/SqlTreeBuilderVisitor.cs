using Linqlite.Linq.Relations;
using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Linqlite.Utils;
using OneOf.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;


namespace Linqlite.Linq.SqlVisitor
{
    public class SqlTreeBuilderVisitor : ExpressionVisitor
    {
        private SqlExpression? _sqlSource;
        private SqlSelectExpression? _sqlSelect;
        private int _aliasNumber;
        private readonly Dictionary<ParameterExpression, SqlExpression> _parameterScope = new Dictionary<ParameterExpression, SqlExpression>();
        private readonly LinqliteProvider _provider;
        internal Dictionary<string, (MemberInfo?, SqlExpression)> TempProjection = new Dictionary<string, (MemberInfo?, SqlExpression)>();
        internal SqlExpression? Where { get; set; }


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
            ["Join"] = new InnerJoinCallHandler(),
            ["LeftJoin"] = new LeftJoinCallHandler(),
            ["GroupBy"] = new GroupByCallHandler()
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

        public SqlTreeBuilderVisitor(LinqliteProvider provider)
        {
            _provider = provider;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_methodBuilders.TryGetValue(node.Method.Name, out var handler))
                return handler.Handle(node, this);

            return base.VisitMethodCall(node);
        }

        public override Expression Visit(Expression node)
        {
            var visited = base.Visit(node);
            return visited;
        }

       

        public SqlExpression Build(Expression expression)
        {
            Expression exp = Visit(expression);
            
            SqlExpression sqlExpr = (SqlExpression)exp;
            if (sqlExpr is SqlGroupByExpression)

                return sqlExpr;


            if(_sqlSelect == null)
            {
                _sqlSelect = new SqlSelectExpression(sqlExpr.Type);
            }            
            
            _sqlSelect.From = (SqlSourceExpression)sqlExpr;
            _sqlSelect.Where = Where;

            if(_sqlSelect.Projection == null)
            {
                if (!TempProjection.TryGetValue(sqlExpr.Type.Name, out var en))
                {
                    var members = SqlExpressionHelper.GetFullProjection(sqlExpr.Type, _sqlSelect.From.Alias);
                    var ee = new SqlEntityProjectionExpression(members, sqlExpr.Type) { Alias = _sqlSelect.From.Alias };
                    TempProjection[sqlExpr.Type.Name] = (null, ee);
                }

                var anon = expression.Type.GetGenericArguments()[0];
                var props = new List<(string, Type)>();
                if (typeof(SqliteEntity).IsAssignableFrom(anon)) // On n'est pas sur un type anonyme mais sur une entité
                {
                    props.Add((anon.Name, anon));
                }
                else
                {
                    var entities = TypesUtils.ExtractEntities(anon).ToList();
                    props = entities
                               .Select(e => (Name: e.EntityType.Name, Type: e.EntityType))
                               .ToList();
                }
                // Générer le type anonyme final
                var resultType = AnonymousTypeFactory.Create(props);

                SqlMemberProjectionExpression sqlMemberProjectionExpression = new SqlMemberProjectionExpression(TempProjection, resultType);
                _sqlSelect.DefaultProjection = sqlMemberProjectionExpression;
                // Construction dutype anonyme du select Applatissement du type anonyme
               
            }
            
            return _sqlSelect!;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            // On visite le body dans un nouveau scope
            foreach (var p in node.Parameters)
                _parameterScope[p] = _sqlSource!;

            var body = Visit(node.Body);

            return node.Update(body, node.Parameters);
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

                
                if (IsMappedEntity(node.Type))
                {
                    //string alias = (join.Left.Type == node.Type) ? ((SqlSourceExpression)join.Left).Alias : ((SqlSourceExpression)join.Right).Alias;
                    string alias = GetAliasFromJoin(join, node.Type);
                    return new SqlEntityReferenceExpression(alias, node.Type);
                }
            }

            if (_sqlSource is SqlTableExpression table)
            {
                if(table.Type == memberDeclaringType)
                {
                    string alias = ((SqlTableExpression)table).Alias;
                    if(!TempProjection.TryGetValue(table.Type.Name, out var exp))
                    {
                        var members = SqlExpressionHelper.GetFullProjection(table.Type, ((SqlTableExpression)table).Alias);
                        var ee = new SqlEntityProjectionExpression(members, memberDeclaringType) { Alias = ((SqlTableExpression)table).Alias };
                        
                        TempProjection[table.Type.Name] = (node.Member, ee);
                    }
                    
                    return new SqlColumnExpression(alias, GetColumnName(memberDeclaringType, node), node.Type);
                }
            }

            if (node.Expression is MemberExpression inner)
            {
                if(typeof(SqliteEntity).IsAssignableFrom(node.Expression.Type))
                {
                    var colName = GetColumnName(_sqlSource.Type, node);
                    //if (select.Type == memberDeclaringType)
                    // if (!string.IsNullOrEmpty(colName))
                    return new SqlColumnExpression(((SqlSourceExpression)_sqlSource).Alias, colName, node.Expression.Type);
                }
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
                        //if (proj.Columns.TryGetValue(node.Member, out var exp))
                        if (proj.Columns.TryGetValue(node.Member.Name, out var exp))
                        {
                            return exp.Item2;
                        }
                    }
                }
            }

            return base.VisitMember(node);
        }

        private string GetAliasFromJoin(SqlJoinExpression join, Type type)
        {
            if (type == join.Right.Type)
                return ((SqlTableExpression)(join.Right)).Alias;
            if(type == join.Left.Type)
                return ((SqlSourceExpression)join.Left).Alias;
            // Ici, le Left est autre chose qu'un Table => join
            if (join.Left is SqlJoinExpression j)
                return GetAliasFromJoin(j, type);
            throw new UnreachableException($"Alias non trouvé pour {type}");
        }

        internal bool IsMappedEntity(Type type)
        {
            var map = EntityMap.Get(type);
            return map != null;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (_parameterScope.TryGetValue(node, out var sql))
                return sql;

            throw new UnreachableException(
                $"Paramètre LINQ non résolu : {node.Name}. " +
                $"Le scope ne contient pas de SqlExpression pour ce paramètre."
            );
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q) 
            { 
                var entityType = q.ElementType; 
                var map = EntityMap.Get(entityType) ?? throw new UnreachableException("EnityMap est null");
                var tableName = map.TableName; 
                var alias = GetNextAlias();
                var table = new SqlTableExpression(tableName, alias, entityType);

                return table;
            }
            

            return new SqlConstantExpression(node.Value ?? DBNull.Value, node.Type);
              
        }

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

        protected override Expression VisitExtension(Expression node)
        {
            if(node is SqlWithRelationsExpression with)
            {
                var joined =  RelationsBuilder.BuildWithRelations(with.Source, _provider);
                return Visit(joined);
            }

            return base.VisitExtension(node);
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

        public void AddWhere(SqlExpression predicate)
        {
            if (Where == null)
                Where = predicate;
            else Where = new SqlBinaryExpression(Where, "AND", predicate);
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
                    throw new NotSupportedException($"Type non supporté {m.GetType()}");
            }

            return value;
        }

        private object? EvaluateMemberExpression(MemberExpression node)
        {
            if (node.Expression is ConstantExpression c)
                return GetMemberValue(c.Value, node.Member);

            if (node.Expression is MemberExpression inner)
            {
                var target = EvaluateMemberExpression(inner);
                return GetMemberValue(target, node.Member);
            }
            if(node.Expression is ParameterExpression)
            {
                var colName = GetColumnName(_sqlSource.Type, node);
                //if (select.Type == memberDeclaringType)
               // if (!string.IsNullOrEmpty(colName))
                    return new SqlColumnExpression(((SqlSourceExpression)_sqlSource).Alias, colName, node.Type);
                int i = 0;
            }

                throw new NotSupportedException($"Structure de variable non supportée {node?.Expression?.Type}");
        }
        private static object? GetMemberValue(object? target, MemberInfo member)
        {
            return member switch
            {
                FieldInfo f => f.GetValue(target),
                PropertyInfo p => p.GetValue(target),
                _ => throw new NotSupportedException($"Membre non supporté: {member}")
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

    public sealed class LinqExpressionNormalizer : ExpressionVisitor
    {

        public LinqExpressionNormalizer()
        {
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;

            var visited = base.Visit(node);
            if (visited == null)
                return null;

            if (NeedsSelect(visited))
                visited = AddSelect(visited);

            return visited;
        }

        // -----------------------
        // 1. Décider si on a besoin d’un Select auto
        // -----------------------
        private bool NeedsSelect(Expression expr)
        {
            // On ne traite que IQueryable<T>
            if (!IsQueryable(expr.Type))
                return false;

            // Projection explicite ?
            var projection = FindFinalProjection(expr);
            if (projection == null)
            {
                // Projection implicite = x => x
                var elementType = GetSequenceElementType(expr.Type);
                return TypesUtils.IsEntityType(elementType);
            }

            // Projection explicite : on regarde si elle contient une entité
            return ProjectionContainsEntity(projection.Body);
        }

        private bool IsQueryable(Type t)
        {
            return t.IsGenericType &&
                   typeof(IQueryable<>).IsAssignableFrom(t.GetGenericTypeDefinition());
        }

        private Type GetSequenceElementType(Type t)
        {
            return t.GetGenericArguments()[0];
        }

     

        private LambdaExpression FindFinalProjection(Expression expr)
        {
            if (expr is MethodCallExpression mce &&
                mce.Method.Name == "Select" &&
                mce.Arguments.Count == 2)
            {
                return (LambdaExpression)StripQuotes(mce.Arguments[1]);
            }

            return null;
        }

        private Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }

        private bool ProjectionContainsEntity(Expression body)
        {
            switch (body.NodeType)
            {
                case ExpressionType.MemberInit:
                    return ((MemberInitExpression)body)
                        .Bindings
                        .OfType<MemberAssignment>()
                        .Any(b => ExpressionContainsEntity(b.Expression));

                case ExpressionType.New:
                    return ((NewExpression)body)
                        .Arguments
                        .Any(ExpressionContainsEntity);

                default:
                    return ExpressionContainsEntity(body);
            }
        }

        private bool ExpressionContainsEntity(Expression expr)
        {
            if (expr == null)
                return false;

            var type = expr.Type;

            if (TypesUtils.IsEntityType(type))
                return true;

            if (type.IsGenericType &&
                typeof(IEnumerable<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                var elementType = type.GetGenericArguments()[0];
                return TypesUtils.IsEntityType(elementType);
            }

            return false;
        }

        

        // -----------------------
        // 2. Ajouter le Select auto
        // -----------------------
        private Expression AddSelect(Expression source)
        {
            // source : IQueryable<TSource>
            var sourceType = GetSequenceElementType(source.Type);

            // lambda de projection : TSource -> TSource (ou ton anonyme si tu veux)
            var parameter = Expression.Parameter(sourceType, "x");
            var body = parameter; // ici projection identité, tu peux brancher ton builder

            var selector = Expression.Lambda(body, parameter);

            var selectCall = Expression.Call(
                typeof(Queryable),
                "Select",
                new[] { sourceType, body.Type },
                source,
                selector);

            // Ici, tu peux soit :
            // - laisser la projection telle quelle et gérer l’anonyme dans ton hydrateur
            // - soit forcer le retour à l’entité racine via Cast

            // Exemple simple : on force la séquence à être IQueryable<rootType>
            var rootType = sourceType; // si ton root est connu autrement, adapte
            var casted = RewriteSelectReturnType(selectCall, rootType);

            return casted;
        }

        // -----------------------
        // 3. Corriger le type générique via Cast
        // -----------------------
        private Expression RewriteSelectReturnType(Expression expr, Type rootType)
        {
            // expr : IQueryable<quelque chose>
            // on enveloppe dans un Cast<rootType>()

            return Expression.Call(
                typeof(Queryable),
                "Cast",
                new[] { rootType },
                expr);
        }
    }

}
