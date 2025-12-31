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
        private readonly TrackingMode? _trackingModeOverride;
        public Expression Expression { get; }
        public Type ElementType => typeof(T);
        public IQueryProvider Provider { get; }

        public QueryableTable(IQueryProvider provider, TrackingMode? trackingModeOverride = null)
        {
            Provider = provider;
            _trackingModeOverride = trackingModeOverride ?? ((QueryProvider)Provider).DefaultTrackingMode;
            Expression = Expression.Constant(this);
            Expression = ApplyTrackingMode(Expression, trackingModeOverride); // Expression.Constant(this);
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
            if (entity is SqliteObservableEntity obj) 
                ((QueryProvider)Provider).Attach(obj, _trackingModeOverride); 
        }

        internal Expression ApplyTrackingMode(Expression source, TrackingMode? mode)
        {
            if (mode == null)
                return source;

            return Expression.Call(
                typeof(TrackingExpressionExtensions),
                nameof(TrackingExpressionExtensions.WithTrackingMode),
                new[] { typeof(T) },
                source,
                Expression.Constant(mode)
            );
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
        public static IQueryable<T> WithTrackingMode<T>(this IQueryable<T> source, TrackingMode mode) 
            => throw new NotSupportedException("This method should never be executed directly."); }
}
