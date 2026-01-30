using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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



    }
}
