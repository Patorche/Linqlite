using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    public static class SqlQuery
    {
        public static TResult Execute<TResult>(string sql)
        {
            Console.WriteLine("SQL: " + sql);

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = typeof(TResult).GetGenericArguments()[0]; 
                // Enumerable.Empty<T>()
                var empty = typeof(Enumerable) .GetMethod("Empty") .MakeGenericMethod(elementType) .Invoke(null, null); 
                return (TResult)empty!; 
            } 
            return default!;
        }

        public static void Insert(Type entityType, Dictionary<string, object?> values)
        {
            var tableName = entityType.Name.ToLower();

            var columns = string.Join(", ", values.Keys);
            var parameters = string.Join(", ", values.Keys.Select(k => "@" + k));

            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            Console.WriteLine("SQL: " + sql);

            // TEMPORAIRE : afficher les valeurs
            foreach (var kv in values)
                Console.WriteLine($"  {kv.Key} = {kv.Value}");

            // Plus tard : exécuter la commande SQLite
        }

    }

}
