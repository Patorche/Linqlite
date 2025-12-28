using Linqlite.Attributes;
using Linqlite.Sqlite;
using SQLitePCL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Linqlite.Linq
{
    public class SqlExpressionVisitor : ExpressionVisitor
    {
        private StringBuilder _sb = new();
        private bool _selectGenerated = false;
        private bool _fromGenerated = false;
        private Dictionary<Type, string> _aliases = new();
        private Dictionary<Type, string> _tables= new();
        private int _aliasCounter = 0;

        private static readonly Dictionary<string, Action<MethodCallExpression, SqlExpressionVisitor>> _handlers = new()
        {
            ["Where"] = HandleWhere,
            ["Select"] = HandleSelect,
            ["OrderBy"] = HandleOrderBy,
            ["OrderByDescending"] = HandleOrderByDescending,
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



        public static string Translate(Expression expression)
        {
            System.Diagnostics.Debug.WriteLine(ExpressionVisualizer.Dump(expression));
            var visitor = new SqlExpressionVisitor();
            visitor.Visit(expression);
            visitor.EnsureSelect();
            return visitor._sb.ToString();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            //foreach (var arg in node.Arguments) Visit(arg);


            if (_handlers.TryGetValue(node.Method.Name, out var handler))
            {
                handler(node, this);
                return node;
             //   return visited;
            }
            return node;
            //return visited;
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
        
        /*
        private static void HandleWhere(MethodCallExpression node, SqlExpressionVisitor v)
        {
            var lambda = (LambdaExpression)StripQuotes(node.Arguments[1]);

            v._sb.Append(" WHERE ");

            // Remplacer le paramètre par l’alias correct
            var outerEntityType = GetOuterEntityType(lambda.Body);
            var alias = v.GetAlias(outerEntityType);

            var expr = new AliasReplacer(lambda.Parameters[0], alias)
                .Visit(lambda.Body);

            v.Visit(expr);
        }
        */
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
            // TODO
            //v.EnsureFrom(elementType);
        }

        private static void HandleOrderBy(MethodCallExpression node, SqlExpressionVisitor v)
        {
            // TODO
        }

        private static void HandleOrderByDescending(MethodCallExpression node, SqlExpressionVisitor v)
        {
            // TODO
        }

        private static void HandleTake(MethodCallExpression node, SqlExpressionVisitor v)
        {
            // TODO
        }

        private static void HandleSkip(MethodCallExpression node, SqlExpressionVisitor v)
        {
            // TODO
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
            var outerEntityType = v.GetEntityType(outerKeyLambda.Body);

            // 3. Déterminer le type de la table "inner"
            var innerEntityType = v.GetEntityType(innerKeyLambda.Body); //node.Arguments[1].Type.GetGenericArguments()[0];

            v.EnsureFrom(outerEntityType);

            // 4. Récupérer les alias
            var outerAlias = v.GetAlias(outerEntityType);
            var innerAlias = v.GetAlias(innerEntityType);

            // 5. Écrire le JOIN
            var tableName = v.GetTableName(innerEntityType);
            v._sb.Append($" JOIN {tableName} {innerAlias} ON ");

            // 6. Générer la clé outer
            var outerExpr = new AliasReplacer(outerKeyLambda.Parameters[0], outerAlias)
                .Visit(outerKeyLambda.Body);

            v.Visit(outerExpr);

            v._sb.Append(" = ");

            // 7. Générer la clé inner
            var innerExpr = new AliasReplacer(innerKeyLambda.Parameters[0], innerAlias)
                .Visit(innerKeyLambda.Body);

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
            // Si l’expression est déjà un alias SQL, on ne descend pas dedans
            /*    if (node.Expression is ConstantExpression c &&
                    c.Value is string s &&
                    s.Contains("."))
                {
                    _sb.Append(s);
                    return node;
                }
            */
            // Sinon, comportement normal
            //var lambda = (LambdaExpression)StripQuotes(node);
            var type = GetEntityType(node);
            var alias = GetAlias(type);
            _sb.Append($"{alias}.{node.Member.Name.ToLower()}");
            return node;
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q)
            {
                var entityType = q.ElementType; // <== Photo, GpsLocalisation, etc.
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
            _sb.Append($"FROM {tablename} {alias} ");
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
            while (expr is MemberExpression m &&  string.IsNullOrEmpty(GetTableName(m.Type)))
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

        private  string GetTableName(Type type)
        {
            if (_tables.TryGetValue(type, out var tablename))
                return tablename;

            tablename = "";
            List<Attribute> attributes = [.. type.GetCustomAttributes()];
            foreach (Attribute attribute in attributes)
            {
                if (attribute.GetType() == typeof(SqliteTableAttribute))
                {
                    SqliteTableAttribute sqliteTableAttribute = (SqliteTableAttribute)attribute;
                    tablename = sqliteTableAttribute.TableName;
                }
            }
            tablename = tablename.ToUpper();
            _tables[type] = tablename;
            return tablename;
        }

        private static bool IsAnonymousType(Type t)
        {
            return Attribute.IsDefined(t, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute))
                   && t.Name.Contains("AnonymousType")
                   && t.IsGenericType;
        }


     /*   private static string GetTableName(Type t)
        {
            return t.Name;
        }*/



        private void v(Expression e) => Visit(e);
    }

}
