using Linqlite.Sqlite;
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
            // Paramètre de la lambda : x =>
            var param = Expression.Parameter(anonType, "x");

            // Extraire toutes les entités du type anonyme final
            var entities = ExtractEntities(anonType).ToList();

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

            // new Anon_Final { ... }
            var newExpr = Expression.MemberInit(
                Expression.New(resultType),
                bindings
            );

            // x => new Anon_Final { ... }
            return Expression.Lambda(newExpr, param);
        }

        public static IEnumerable<(string Path, Type EntityType)> ExtractEntities(Type type, string prefix = "")
        {
            if (!IsAnonymousType(type))
                yield break;

            foreach (var prop in type.GetProperties())
            {
                var propType = prop.PropertyType;
                var path = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                if (IsEntityType(propType))
                {
                    yield return (path, propType);
                }
                else if (IsAnonymousType(propType))
                {
                    foreach (var nested in ExtractEntities(propType, path))
                        yield return nested;
                }
            }
        }

        private static bool IsEntityType(Type propType)
        {
            return typeof(SqliteEntity).IsAssignableFrom(propType);
        }

        private static bool IsAnonymousType(Type type)
        {
            return type.Name.Contains("Anon");
        }
    }
}
