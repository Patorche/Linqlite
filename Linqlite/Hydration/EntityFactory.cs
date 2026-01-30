using Linqlite.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Hydration
{
    internal static class EntityFactory<T> where T : ObservableObject
    {
        // Cache de délégués pour instanciation rapide
        private static readonly Func<T> _creator = CreateConstructor();

        private static Func<T> CreateConstructor()
        {
            var ctor = typeof(T).GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new InvalidOperationException($"Type {typeof(T)}doit avoir un constructeur sans paramètre.");

            // Utilise Expression pour compiler un délégué rapide
            var newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda<Func<T>>(newExpr);
            return lambda.Compile();
        }

        public static T CreateInstance()
        {
            return _creator();
        }
    }
}
