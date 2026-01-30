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
                $"Le provider n'a pas été initialisé pour le type '{_type.Name}'.");

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => throw new InvalidOperationException(
                $"TLe provider n'a pas été initialisé pour le type '{_type.Name}'.");

        public object? Execute(Expression expression)
            => throw new InvalidOperationException(
                $"Le provider n'a pas été initialisé pour le type '{_type.Name}'.");

        public TResult Execute<TResult>(Expression expression)
            => throw new InvalidOperationException(
                $"Le provider n'a pas été initialisé pour le type '{_type.Name}'.");
    }
}
