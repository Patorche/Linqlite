using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Linqlite.Linq
{
    internal sealed class AliasReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _param;
        private readonly string _alias;

        public AliasReplacer(ParameterExpression param, string alias)
        {
            _param = param;
            _alias = alias;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _param)
            {
                // On ne met pas l’alias tout de suite ici,
                // on laisse VisitMember construire "alias.colonne"
                return base.VisitParameter(node);
            }

            return base.VisitParameter(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            // Cas : q.p.Focal
            // On veut reconnaître quand la racine est _param
            var chain = GetMemberChain(node);

            if (chain.RootIsParameter)
            {
                // chain.Members = [p, Focal]
                // Le dernier membre est la colonne réelle
                var last = chain.Members.Last();
                var lastName = EntityMap.Get(node.Expression.Type).Column(last.Name);

                var sql = $"{_alias}.{lastName.ToLower()}";
                return Expression.Constant(sql);
            }

            return base.VisitMember(node);
        }

        private static (bool RootIsParameter, List<MemberInfo> Members) GetMemberChain(MemberExpression node)
        {
            var members = new List<MemberInfo>();
            Expression current = node;

            bool rootIsParam = false;

            while (current is MemberExpression m)
            {
                members.Insert(0, m.Member);
                current = m.Expression;
            }

            if (current is ParameterExpression p)
                rootIsParam = true;

            return (rootIsParam, members);
        }
    }

}
