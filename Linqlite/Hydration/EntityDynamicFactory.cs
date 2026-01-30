using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Hydration
{
    internal static class EntityDynamicFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<object>> _cache = new();

        public static object CreateInstance(Type type)
        {
            if (!_cache.TryGetValue(type, out var creator))
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                    throw new InvalidOperationException($"Type {type} doit avoir un constructeur sans paramètre.");

                var newExpr = Expression.New(ctor);
                var lambda = Expression.Lambda<Func<object>>(newExpr);
                creator = lambda.Compile();
                _cache[type] = creator;
            }

            return creator();
        }

    }
}
