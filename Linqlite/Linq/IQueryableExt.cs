using Linqlite.Linq.SqlExpressions;
using OneOf.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ZSpitz.Util;

namespace Linqlite.Linq
{
    public static class IQueryableExt
    {
        public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
                    this IQueryable<TOuter> outer,
                    IQueryable<TInner> inner,
                    Expression<Func<TOuter, TKey>> outerKeySelector,
                    Expression<Func<TInner, TKey>> innerKeySelector,
                    Expression<Func<TOuter, TInner?, TResult>> resultSelector)
        {
            return outer.Provider.CreateQuery<TResult>(
                Expression.Call(
                    null,
                    ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(
                        typeof(TOuter), typeof(TInner), typeof(TKey), typeof(TResult)),
                    outer.Expression,
                    inner.Expression,
                    Expression.Quote(outerKeySelector),
                    Expression.Quote(innerKeySelector),
                    Expression.Quote(resultSelector)
                )
            );
        }

        public static IQueryable<TOuter> WithRelations<TOuter>(this IQueryable<TOuter> source)
        {
            var expr = new SqlWithRelationsExpression(source.Expression, source.Provider);
            return source.Provider.CreateQuery<TOuter>(expr);
        }

        public static List<T> ToRootList<T>(this IQueryable query)
        {
            // 1. Exécuter la requête LINQ → liste d’anonymes
            Type anonType = query.ElementType;

            var enumerable = (IEnumerable)query.Provider.Execute(query.Expression);

            // 2. Convertir en liste d’objets
            //var anonList = enumerable.Cast<object>().ToList();

            // 2. Extraire les entités racines
            //var entities = RootEntityExtractor.ExtractRootEntities(anonList);

            //var enumerable = ((IEnumerable)anonList).Cast<object>();
            var res = RootEntityExtractor.ExtractRootEntities(enumerable);

            var objectList = ((IEnumerable)res).Cast<T>().ToList();

            // 3. Retourner la liste propre
            return objectList;
        }


    }
}
