using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ZSpitz.Util;

namespace Linqlite.Linq.SqlVisitor
{
    internal class ContainsCallHandler : IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            var source = node.Object ?? node.Arguments[0];

            if(source.Type is IQueryable)
            {
                return HandleExistsContains(source, builder);
            }
            else if (source.Type == typeof(string))
            {
                return HandleStringContains(node, builder);
            }
            else if (IsGenericEnumerable(source.Type))
            {
                return HandleEnumerableContains(node, builder);
            }
            
           
            throw new NotSupportedException("Contains non supporté pour ce type");
        }

        private SqlExpression HandleExistsContains(Expression source, SqlTreeBuilderVisitor builder)
        {
           
            // 1. Visiter la sous-requête
            var subquery = (SqlSelectExpression)builder.Visit(source);

            // 2. Résoudre la clé primaire de la sous-requête
            var subKey = ResolvePrimaryKey(subquery.From);

            // 3. Résoudre la clé primaire de la valeur comparée
            var valueExpr = builder.Visit(source);
            var valueKey = ResolvePrimaryKey((SqlSourceExpression)valueExpr);

            // 4. Ajouter la condition dans la sous-requête
            subquery.AddWhere(new SqlBinaryExpression(subKey,"=",valueKey));

            // 5. Retourner un EXISTS
            return new SqlContainsExpression(subquery, null, SqlContainsType.Exists);

        }

        private SqlExpression ResolvePrimaryKey(SqlSourceExpression from)
        {
            return from;
        }

        private SqlExpression HandleEnumerableContains(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            var lstObj = EvaluateMemberExpression(node.Object);
            var list = (IEnumerable)lstObj;

            var constList = new List<SqlExpression>();

            foreach (var item in list)
            {
                constList.Add(new SqlConstantExpression(item, item?.GetType() ?? typeof(object)));
            }

            var source = (SqlExpression)builder.Visit(node.Arguments[0]);

            return new SqlContainsExpression(source, new SqlEnumerableExpression(constList, constList.GetType()), SqlContainsType.InList);
        }

        private SqlExpression HandleStringContains(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            SqlExpression source = (SqlExpression)builder.Visit(node.Object);
            var arg = node.Arguments[0];
            var value = "";
            if (arg is ConstantExpression c && c.Value is string s)
            {
                value = s;
            }
            return new SqlContainsExpression(source, new SqlConstantExpression("%" + value + "%", source.Type), SqlContainsType.Like);
        }

        public bool IsGenericEnumerable(Type type)
        {
            if (type == null)
                return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;

            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private static object? EvaluateMemberExpression(Expression expr)
        {
            switch (expr)
            {
                case ConstantExpression c:
                    return c.Value;

                case MemberExpression m:
                    var target = EvaluateMemberExpression(m.Expression);
                    return m.Member switch
                    {
                        FieldInfo f => f.GetValue(target),
                        PropertyInfo p => p.GetValue(target),
                        _ => throw new NotSupportedException($"Unsupported member: {m.Member}")
                    };

                default:
                    throw new NotSupportedException($"Unsupported expression: {expr.NodeType}");
            }
        }


    }
}
