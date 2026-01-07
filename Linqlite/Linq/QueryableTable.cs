using Linqlite.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using static OneOf.Types.TrueFalseOrNull;

namespace Linqlite.Linq
{
    public class QueryableTable<T> : IQueryable<T>, IQueryableTableDefinition
    {
        private IQueryProvider? _provider;

        public TrackingMode? TrackingModeOverride;
        public Expression Expression { get; private set; }
        public Type ElementType => typeof(T);
        public Type EntityType => typeof(T);
        public IQueryProvider Provider 
        { 
            get => _provider;
            set
            {
                _provider = value;
                TrackingModeOverride = TrackingModeOverride ?? ((QueryProvider)Provider).DefaultTrackingMode;
                Expression = ApplyTrackingMode(Expression);
            } 
        }
        public QueryableTable(TrackingMode? trackingModeOverride = null)
        {
            TrackingModeOverride = trackingModeOverride;
            Expression = Expression.Constant(this);
        }

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
