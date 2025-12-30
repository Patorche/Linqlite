using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    public class QueryProvider : IQueryProvider, IDisposable
    {
        private string _connectionString = "";
        private SqliteConnection? _connection = null;

        public string ConnectionString 
        { 
            get => _connectionString; 
            private set => _connectionString = value;
        }

       

        public QueryProvider()
        {
            
        }

        public QueryProvider(string connectionString)
        {
            ConnectionString = connectionString;
            Connect();
        }

        private void Connect()
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
            _connection = new SqliteConnection(@$"Data Source={ConnectionString}");
            _connection.Open();
            Optimize();
        }

        private void Optimize()
        {
            using (var command = _connection?.CreateCommand())
            {
                if (command == null) return;
                command.CommandText = @"
                    PRAGMA synchronous = OFF;
                    PRAGMA journal_mode = MEMORY;
                    PRAGMA temp_store = MEMORY;";
                command?.ExecuteNonQuery();
            }
        }

        public IQueryable CreateQuery(Expression expression)
        {
            /*  var elementType = expression.Type.GetGenericArguments()[0];
              var tableType = typeof(QueryableTable<>).MakeGenericType(elementType);
              return (IQueryable)Activator.CreateInstance(tableType, this, expression)!;
            */

            if (expression.Type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOrderedQueryable<>)))
            {
                var elementType = expression.Type.GetGenericArguments()[0];
                var orderedType = typeof(OrderedQueryableTable<>).MakeGenericType(elementType);
                return (IQueryable)Activator.CreateInstance(orderedType, this, expression);
            }
            else
            {
                var elementType = expression.Type.GetGenericArguments()[0];
                var normalType = typeof(QueryableTable<>).MakeGenericType(elementType);
                return (IQueryable)Activator.CreateInstance(normalType, this, expression);
            }

        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (typeof(IOrderedQueryable<TElement>).IsAssignableFrom(expression.Type))
            {
                return new OrderedQueryableTable<TElement>(this, expression);
            }

            return new QueryableTable<TElement>(this, expression);
        }

        


        public void Insert<T>(T entity) where T : SqliteObservableEntity, new()
        {
            // 1. Extraire les colonnes et valeurs
            var map = EntityMap.Get(entity.GetType()).Columns;

            // 2. Construire un dictionnaire { columnName → value }
            var values = new Dictionary<string, object?>();

            foreach (var col in map)
            {
                var value = ExtractValue(entity, col);
                values[col.ColumnName] = value;
            }

            // 3. Déléguer à SqlQuery
            SqlQuery<T>.Insert(typeof(T), values, _connection);
        }

        private static object? ExtractValue<T>(T entity, EntityPropertyInfo col)
        {
            object? current = entity;

            //foreach (var prop in col.PropertyPath)
                current = col.PropertyInfo.GetValue(current);

            return current;
        }

        public async void Dispose()
        {
            if(_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose(); 
            }
        }

        public object? Execute(Expression expression)
        {
            var sql = Translate(expression);

            // Détecter le type élémentaire
            var elementType = TypeSystem.GetElementType(expression.Type);

            // Appeler SqlQuery<elementType>.Execute(sql, connection)
            var method = typeof(SqlQuery<>)
                .MakeGenericType(elementType)
                .GetMethod(nameof(SqlQuery<SqliteObservableEntity>.Execute));

            var result = method.Invoke(null, new object[] { sql, _connection });

            return result;
        }

        private string Translate(Expression expression)
        {
            return SqlExpressionVisitor.Translate(expression);
        }

        public TResult? Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);
        }
    }

}
