using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    public static class ProjectionExtractor
    {
        public static List<ProjectedProperty> ExtractPropertiesRecursively(Expression expr)
        {
            var result = new List<ProjectedProperty>();
            Extract(expr, result);
            return result;
        }

        private static void Extract(Expression expr, List<ProjectedProperty> result)
        {
            switch (expr)
            {
                // Cas principal : new { ... } avec initialisation
                case MemberInitExpression init:
                    foreach (var binding in init.Bindings.OfType<MemberAssignment>())
                        Extract(binding.Expression, result);
                    break;

                // Cas rare : new { ... } sans MemberInit (possible dans certains JOIN)
                case NewExpression nex:
                    foreach (var arg in nex.Arguments)
                        Extract(arg, result);
                    break;

                // Cas d'une propriété simple (Photo, Keyword, etc.)
                case MemberExpression memberExpr:
                    result.Add(new ProjectedProperty
                    {
                        Name = memberExpr.Member.Name,
                        Expression = memberExpr,
                        Type = memberExpr.Type
                    });
                    break;

                // Cas d'un accès direct à un paramètre (ex: left, right)
                case ParameterExpression paramExpr:
                    // On ignore : ce n'est pas une propriété projetée
                    break;

                // Cas d'un accès à un champ (rare mais possible)
          /*      case MemberAccessExpression m:
                    // Même logique que MemberExpression
                    result.Add(new ProjectedProperty
                    {
                        Name = m.Member.Name,
                        Expression = m,
                        Type = m.Type
                    });
                    break;*/

                default:
                    // On ignore les scalaires, constantes, conversions, etc.
                    break;
            }
        }
    }

}
