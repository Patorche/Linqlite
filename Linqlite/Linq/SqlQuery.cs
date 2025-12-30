using Linqlite.Hydration;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    public static class SqlQuery<T> where T : SqliteObservableEntity, new()
    {
        public static Func<SqliteDataReader, T> Hydrator { get; private set; }

        static SqlQuery()
        {
            if (!typeof(T).IsAbstract)
            {
                //Hydrator = HydratorBuilder.CompileHydrator<T>(); 
            }

        }

        public static IEnumerable<T> Execute(string sql, SqliteConnection? sqliteConnection)
        {
            CheckConnection(sqliteConnection);
            

            Console.WriteLine("SQL: " + sql);

            using var command = sqliteConnection.CreateCommand(); 
            command.CommandText = sql; 
            using var reader = command.ExecuteReader();
            while (reader.Read()) 
            {
                //yield return Hydrate<T>(reader); 
                //yield return HydratorBuilder.CompileHydrator<TResult>();
                yield return HydratorBuilder.GetEntity<T>(reader); //Hydrator. Invoke(reader);
            }

        }


        public static void Insert(Type entityType, Dictionary<string, object?> values, SqliteConnection sqliteConnection)
        {
            CheckConnection(sqliteConnection);

            var tableName = entityType.Name.ToLower();

            var columns = string.Join(", ", values.Keys);
            var parameters = string.Join(", ", values.Keys.Select(k => "@" + k));

            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

            Console.WriteLine("SQL: " + sql);

            // TEMPORAIRE : afficher les valeurs
            foreach (var kv in values)
                Console.WriteLine($"  {kv.Key} = {kv.Value}");

            using var command = sqliteConnection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static void CheckConnection(SqliteConnection? sqliteConnection)
        {
            if (sqliteConnection == null)
            {
                throw new Exception("Impossible d'exécuter une requête, aucune connexion n'est définié.");
            }
        }


    }

}
