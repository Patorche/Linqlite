using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    internal class NullQueryProvider : IQueryProvider
    {
        private readonly Type _type;

        public NullQueryProvider(Type type)
            => _type= type;

        public IQueryable CreateQuery(Expression expression)
            => throw new InvalidOperationException(
                $"The provider for type '{_type.Name}' has not been initialized.");

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => throw new InvalidOperationException(
                $"The provider for table '{_type.Name}' has not been initialized.");

        public object? Execute(Expression expression)
            => throw new InvalidOperationException(
                $"The provider for table '{_type.Name}' has not been initialized.");

        public TResult Execute<TResult>(Expression expression)
            => throw new InvalidOperationException(
                $"The provider for table '{_type.Name}' has not been initialized.");
    }
}
