using Linqlite.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using static OneOf.Types.TrueFalseOrNull;

namespace Linqlite.Linq
{
    
    internal class TableLite<T> : IQueryable<T>, IQueryableTableDefinition
    {
        private IQueryProvider _provider = new NullQueryProvider(typeof(T));
        private TrackingMode? _trackingmode = null;
        public TrackingMode? TrackingModeOverride => _trackingmode;
        public Expression Expression { get; private set; }
        public Type ElementType => typeof(T);
        public Type EntityType => typeof(T);
        public IQueryProvider Provider 
        { 
            get => _provider;
            set => _provider = value;

        }


        public TableLite(TrackingMode? trackingModeOverride = null)
        {
            _trackingmode = trackingModeOverride;
            Expression = Expression.Constant(this);
        }

        public TableLite(IQueryProvider provider, TrackingMode? trackingModeOverride = null)
        {
            Expression = Expression.Constant(this);
            _provider = provider;
            _trackingmode = trackingModeOverride;
            
        }

        public TableLite(IQueryProvider provider, Expression expression)
        {
            _provider = provider;
            Expression = expression;
        }

        internal void AttachEntity(T entity) 
        { 
            if (entity is SqliteEntity obj) 
                ((LinqLiteProvider)Provider).Attach(obj, TrackingModeOverride); 
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

}
