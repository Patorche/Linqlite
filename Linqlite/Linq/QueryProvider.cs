using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    public class QueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments()[0];
            var tableType = typeof(QueryableTable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(tableType, this, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new QueryableTable<TElement>(this, expression);

        public object Execute(Expression expression)
            => Execute<object>(expression);

        public TResult Execute<TResult>(Expression expression)
        {
            var sql = SqlExpressionVisitor.Translate(expression);
            return (TResult)SqlQuery.Execute<TResult>(sql);
        }

        public void Insert<T>(T entity)
        {
            // 1. Extraire les colonnes et valeurs
            var map = EntityMap<T>.Columns;

            // 2. Construire un dictionnaire { columnName → value }
            var values = new Dictionary<string, object?>();

            foreach (var col in map)
            {
                var value = ExtractValue(entity, col);
                values[col.ColumnName] = value;
            }

            // 3. Déléguer à SqlQuery
            SqlQuery.Insert(typeof(T), values);
        }

        private static object? ExtractValue<T>(T entity, EntityPropertyInfo col)
        {
            object? current = entity;

            foreach (var prop in col.PropertyPath)
                current = prop.GetValue(current);

            return current;
        }


    }

}
