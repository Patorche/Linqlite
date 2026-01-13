using Linqlite.Linq.SqlExpressions;
using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal abstract class AbstractSourceHandler
    {
        protected AbstractSqlProjectionExpression HandleProjection(Expression body, SqlTreeBuilderVisitor builder, SqlSelectExpression select) 
        { 
            switch (body)
            {
                case MemberExpression member:
                    return HandleMemberProjection(member, builder, select);

                case NewExpression nex:
                    return HandleNewProjection(nex, builder, select);
                
                case MemberInitExpression mie:
                    return HandleInitProjection(mie, builder, select);

                default:
                    throw new NotSupportedException($"Expression select non supportée: {body.NodeType}");
            }
        }

        private SqlMemberProjectionExpression HandleInitProjection(MemberInitExpression init, SqlTreeBuilderVisitor builder, SqlSelectExpression select)
        {
            if (init == null)
                throw new UnreachableException("Tentative d'ajout d'une projection null !");

            var members = new Dictionary<MemberInfo, SqlExpression>();

            foreach (var binding in init.Bindings)
            {
                if (binding is MemberAssignment assign)
                {
                    var sqlExpr = builder.Visit(assign.Expression) as SqlExpression;
                    if (sqlExpr == null)
                        throw new UnreachableException("Tentative d'ajout d'une projection null !");
                    members.Add(assign.Member, sqlExpr);
                }
                else
                {
                    throw new NotSupportedException($"Binding non supporté : {binding.BindingType}");
                }
            }

            return new SqlMemberProjectionExpression(members, init.Type);
        }


        private AbstractSqlProjectionExpression HandleMemberProjection(MemberExpression member, SqlTreeBuilderVisitor builder, SqlSelectExpression select)
        {
            var sqlExpr = (SqlExpression)builder.Visit(member);
            if(sqlExpr is SqlEntityReferenceExpression er)
            {
                var map = EntityMap.Get(er.Type);
                var members = new List<SqlColumnExpression>();
                members = GetFullProjection(er.Type, er.Alias);
                return new SqlEntityProjectionExpression(members, er.Type);
            }

            MemberInfo info = member.Member as MemberInfo;
            Dictionary<System.Reflection.MemberInfo, SqlExpression> ms = new Dictionary<System.Reflection.MemberInfo, SqlExpression>();
            ms.Add(info, sqlExpr);
            return new SqlMemberProjectionExpression(ms, member.Type);
        }
        private List<SqlColumnExpression> GetFullProjection(Type type, string alias)
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

        private SqlMemberProjectionExpression HandleNewProjection(NewExpression nex, SqlTreeBuilderVisitor builder, SqlSelectExpression select)
        {
            if (nex == null)
                throw new UnreachableException("Tentative d'ajout d'une projection null !");

            var columns = nex.Arguments.Select(arg => (SqlExpression)builder.Visit(arg)).ToList();

            var members = new Dictionary<MemberInfo, SqlExpression>();
            for (int i = 0; i < nex.Arguments.Count; i++)
            {
                var member = nex.Members?[i];
                var argument = nex.Arguments[i];
                var sqlExpr = builder.Visit(argument) as SqlExpression;

                if (member == null || sqlExpr == null)
                    throw new UnreachableException("Tentative d'ajout d'une projection null !");
                members.Add(member, sqlExpr);
            }

            return new SqlMemberProjectionExpression(members, nex.Type);
        }

     /*   private SqlMemberProjectionExpression HandleCallProjection(MethodCallExpression call, SqlTreeBuilderVisitor builder)
        {
            var members = new Dictionary<MemberInfo, SqlExpression>();

            // On récupère les paramètres de la méthode
            var parameters = call.Method.GetParameters();

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                var arg = call.Arguments[i];
                var sqlExpr = builder.Visit(arg) as SqlExpression;
                if (sqlExpr == null)
                    throw new UnreachableException("Projection null dans un appel de méthode");

                var member = parameters[i];
                members.Add(member, sqlExpr);
            }

            return new SqlMemberProjectionExpression(members, call.Type);
        }
     */


    }
}
