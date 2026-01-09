using System.Linq.Expressions;

public class TerminalVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Cas : Single(predicate), First(predicate), Any(predicate), Count(predicate)
        if (HasPredicateOverload(node.Method.Name, node.Arguments.Count))
        {
            // 1. Visiter la source (toujours !)
            var visitedSource = Visit(node.Arguments[0]);

            // 2. Extraire le lambda
            var predicate = (LambdaExpression)StripQuotes(node.Arguments[1]);
            var elementType = predicate.Parameters[0].Type;

            // 3. Construire Expression<Func<T,bool>>
            var funcType = typeof(Func<,>).MakeGenericType(elementType, typeof(bool));
            var typedPredicate = Expression.Lambda(funcType, predicate.Body, predicate.Parameters);

            // 4. Construire Where(source, predicate)
            var whereCall = Expression.Call(
                typeof(Queryable),
                "Where",
                new[] { elementType },
                visitedSource,
                typedPredicate
            );

            // 5. Reconstruire l'appel terminal sans prédicat
            return Expression.Call(
                typeof(Queryable),
                node.Method.Name,
                new[] { elementType },
                whereCall
            );
        }

        // Sinon, comportement normal
        return base.VisitMethodCall(node);
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
            e = ((UnaryExpression)e).Operand;
        return e;
    }

    private static bool HasPredicateOverload(string methodName, int argCount)
    {
        // Méthodes terminales avec prédicat : Single, First, Any, Count
        return (methodName == "Single" ||
                methodName == "First" ||
                methodName == "Any" ||
                methodName == "Count")
               && argCount == 2; // 2 arguments = source + prédicat
    }
}
