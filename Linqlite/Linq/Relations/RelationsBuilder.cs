using Linqlite.Linq.SqlVisitor;
using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.Relations
{
    public static class RelationsBuilder
    {
        public static Expression BuildWithRelations(Expression source, LinqliteProvider _provider)
        {
            var entityType = source.Type.GetGenericArguments()[0];
            HashSet<Type> visited = new HashSet<Type>();
            List<IRelation> relations = new();

            ExploreRelations(entityType, visited, relations);

            var ctx = new BuildContext(source, _provider);

            foreach (var relation in relations)
            {
                relation.ApplyJoins(ctx);
            }
            
            var anonType = ctx.CurrentExpression.Type.GetGenericArguments()[0];
            var projection = ProjectionBuilder.BuildProjectionLambda(anonType);


            var selectCall = Expression.Call(
                                typeof(Queryable), 
                                "Select", 
                                new[] { anonType, projection.Body.Type }, 
                                ctx.CurrentExpression, projection); 
            
            return selectCall;
        }

        private static void ExploreRelations(Type type, HashSet<Type> visited, List<IRelation> result)
        {
            if (!visited.Add(type))
                return;
            var relations = EntityMap.Get(type).Relations;
            foreach (var relation in relations)
            {
                result.Add(relation);
                ExploreRelations(relation.TargetType, visited, result);
            }
        }

    

    }
}
