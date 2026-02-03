using Linqlite.Sqlite;
using Linqlite.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.Relations
{
    internal class ProjectionBuilder
    {
        public static LambdaExpression BuildProjectionLambda(Type anonType)
        {
            if (TypesUtils.IsEntityType(anonType))
            {
                var eparam = Expression.Parameter(anonType, "p"); 
                return Expression.Lambda(eparam, eparam);
            }
            // Paramètre de la lambda : x =>
            var param = Expression.Parameter(anonType, "x");

            // Extraire toutes les entités du type anonyme final
            var entities = TypesUtils.ExtractEntities(anonType).ToList();

            // Construire la liste des propriétés du type anonyme final
            var props = entities
                .Select(e => (Name: e.EntityType.Name, Type: e.EntityType))
                .ToList();

            // Générer le type anonyme final
            var resultType = AnonymousTypeFactory.Create(props);

            var bindings = new List<MemberBinding>();

            foreach (var (path, entityType) in entities)
            {
                // Accéder à x.Left.Left etc.
                Expression access = param;
                foreach (var part in path.Split('.'))
                    access = Expression.PropertyOrField(access, part);

                // Récupérer la propriété correspondante dans le type anonyme final
                var prop = AnonymousTypeFactory.GetPropertyFor(resultType, entityType);

                // Ajouter le binding
                bindings.Add(Expression.Bind(prop, access));
            }
            var ctors = resultType.GetConstructors();
            var withArgsctor = ctors.First(c => c.GetParameters().Length > 0);
            // new Anon_Final { ... }
            var ctor = resultType.GetConstructor(Type.EmptyTypes);
            var ne = Expression.New(ctor);
            var newExpr = Expression.MemberInit(ne, bindings);

            // x => new Anon_Final { ... }
            return Expression.Lambda(newExpr, param);
        }
    }
}
