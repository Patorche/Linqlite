using Linqlite.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using static OneOf.Types.TrueFalseOrNull;

namespace Linqlite.Linq
{
    public class QueryableTable<T> : IQueryable<T>
    {
        public TrackingMode TrackingModeOverride;
        public Expression Expression { get; }
        public Type ElementType => typeof(T);
        public IQueryProvider Provider { get; }

        public QueryableTable(IQueryProvider provider, TrackingMode? trackingModeOverride = null)
        {
            Provider = provider;
            TrackingModeOverride = trackingModeOverride ?? ((QueryProvider)Provider).DefaultTrackingMode;
            Expression = Expression.Constant(this);
            Expression = ApplyTrackingMode(Expression); // Expression.Constant(this);
        }

        /*public QueryableTable(QueryProvider provider, TrackingMode? trackingModeOverride = null) 
        {   
            Provider = provider; 
            _trackingModeOverride = trackingModeOverride ?? ((QueryProvider)Provider).DefaultTrackingMode; 
        }
        */
        public QueryableTable(IQueryProvider provider, Expression expression)
        {
            Provider = provider;
            Expression = expression;
        }


        internal void AttachEntity(T entity) 
        { 
            if (entity is SqliteEntity obj) 
                ((QueryProvider)Provider).Attach(obj, TrackingModeOverride); 
        }

        internal Expression ApplyTrackingMode(Expression source)
        {
            var method = typeof(TrackingExpressionExtensions).GetMethod(nameof(TrackingExpressionExtensions.WithTrackingMode))!.MakeGenericMethod(typeof(T)); 
            return Expression.Call(method, source, Expression.Constant(TrackingModeOverride, typeof(TrackingMode)));
        }



        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class TrackingExpressionExtensions { 
        public static IQueryable<T> WithTrackingMode<T>(this IQueryable<T> source, TrackingMode ex) 
            => throw new NotSupportedException("This method should never be executed directly."); }
}
