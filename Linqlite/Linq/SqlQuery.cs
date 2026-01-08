using Linqlite.Hydration;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Principal;
using System.Text;
using System.Transactions;

namespace Linqlite.Linq
{
    public static class SqlQuery<T> where T : SqliteEntity, new()
    {
        public static Func<SqliteDataReader, T> Hydrator { get; private set; }

        static SqlQuery()
        {
            if (!typeof(T).IsAbstract)
            {
                //Hydrator = HydratorBuilder.CompileHydrator<T>(); 
            }

        }

        public static IEnumerable<T> Execute(string sql, QueryProvider provider, TrackingMode trackingMode, IReadOnlyDictionary<string, object> parameters)
        {
            CheckConnection(provider.Connection);
            

            Console.WriteLine("SQL: " + sql);

            var command = provider.Connection.CreateCommand(); 
            command.CommandText = sql;
            foreach (var parameter in parameters)
            { 
                command.Parameters.AddWithValue(parameter.Key, parameter.Value); 
            }
            var reader = command.ExecuteReader();
            try
            {
                while (reader.Read())
                {
                    //yield return Hydrate<T>(reader); 
                    //yield return HydratorBuilder.CompileHydrator<TResult>();
                    T entity = HydratorBuilder.GetEntity<T>(reader);
                    provider.Attach(entity, trackingMode);
                    yield return entity;
                }
            }
            finally 
            { 
                reader.Dispose(); 
                command.Dispose(); 
            }
        }


        public static long InsertOrGetId(T entity, QueryProvider provider, TrackingMode trackingMode)
        {
            CheckConnection(provider.Connection);

            var map = EntityMap.Get(typeof(T));
            IUniqueConstraint? upsertKey = map.GetUpsertKey();
            if(upsertKey == null)
                throw new Exception("InsertOrGetId requiert un groupe unique. Vous devez définir une propriété Groupe. Utilisez Insert sinon");

            var tableName = map.TableName;
            var sql = $"INSERT INTO {tableName}";
            string[] head = GetColumnsInsertQuery(entity);
            sql += "(" + head[0] + ")";
            sql += " VALUES(" + head[1] + ")";

            var cols = string.Join(", ", upsertKey.Columns.Select(c => $"\"{c}\""));
            sql += " ON CONFLICT (" + cols + ")"; //  DO NOTHING";
            sql += $" DO UPDATE SET {upsertKey.Columns[0]} = excluded.{upsertKey.Columns[0]}";
            sql += " RETURNING " + map.Columns.Single(c => c.IsPrimaryKey).ColumnName;

            sql += ";";

            using var cmd = provider.Connection.CreateCommand();
            PopulateInsertCommand(cmd, entity);

            Console.WriteLine("SQL: " + sql);

            cmd.CommandText = sql;
            var res = cmd.ExecuteScalar();
            long id = Convert.ToInt64(res);
            HydratorBuilder.SetPrimaryKey(entity, id);
            return id;
        }

        public static long Insert(T entity, QueryProvider provider, TrackingMode trackingMode)
        {
            CheckConnection(provider.Connection);

            var map = EntityMap.Get(typeof(T));

            var tableName = map.TableName;
            var sql = $"INSERT INTO {tableName}";
            string[] head = GetColumnsInsertQuery(entity);
            sql += "(" + head[0] + ")";
            sql += " VALUES(" + head[1] + ");";

            using var cmd = provider.Connection.CreateCommand();
            PopulateInsertCommand(cmd, entity);

            cmd.CommandText = sql;
            var res = cmd.ExecuteNonQuery();

            if (res == 1)
            {
                var idQuery = $"SELECT last_insert_rowid();";
                var q = new SqliteCommand(idQuery, provider.Connection);
                provider.Attach(entity, trackingMode);
                var id = Convert.ToInt64(q.ExecuteScalar()!);
                HydratorBuilder.SetPrimaryKey(entity, id);
                return id;
            }

            return -1;
        }



        public static void Delete(T entity, QueryProvider provider)
        {
            CheckConnection(provider.Connection);

            var table = EntityMap.Get(typeof(T)).TableName;
            var keys = EntityMap.Get(typeof(T)).Columns.Where(c => c.IsPrimaryKey);


            using var cmd = provider.Connection.CreateCommand();
            var query = $"DELETE FROM {table} WHERE ";
            var where = string.Empty;

            foreach(var k in keys)
            {
                if (!string.IsNullOrEmpty(where))
                    where += " AND ";
                where += $"{k.ColumnName} = @{k.ColumnName}"; 
                cmd.Parameters.AddWithValue($"@{k.ColumnName}", GetSqliteValue(k.PropertyInfo, entity));
            }
            cmd.CommandText = query + where;
            cmd.ExecuteNonQuery();
        }

        internal static void Update(T entity, string property, QueryProvider provider)
        {
            CheckConnection(provider.Connection);
            if (string.IsNullOrEmpty(property)) return;

            var map = EntityMap.Get(entity.GetType());

            var columns = map.Columns;
            var column = columns.SingleOrDefault(c => c.PropertyInfo.Name == property);
            if (column == default(EntityPropertyInfo))
                return;

            string tableName = map.TableName;
            using var cmd = provider.Connection.CreateCommand();

            cmd.CommandText = $"UPDATE {tableName} SET ";

            if (!string.IsNullOrEmpty(column.ColumnName))
            {
                cmd.CommandText += $" {column.ColumnName} = @{column.ColumnName}";
                cmd.Parameters.AddWithValue($"@{column.ColumnName}", GetSqliteValue(column.PropertyInfo, entity));
            }
            else
            {
                object v = column.PropertyInfo.GetValue(entity);
                string updtString = GetUpdatesFromEntity(v, cmd.Parameters);
                cmd.CommandText += updtString;
            }
            // Where
            cmd.CommandText += " WHERE ";

            var keys = columns.Where(c => c.IsPrimaryKey);
            string where = "";
            foreach (var key in keys) 
            {
                if (!string.IsNullOrEmpty(where))
                    where += " AND ";
                where += $"{key.ColumnName} = @{key.ColumnName}";
                cmd.Parameters.AddWithValue($"@{key.ColumnName}", GetSqliteValue(key.PropertyInfo, entity));
            }

            cmd.CommandText += where;
            cmd.ExecuteNonQuery();
        }

        private static string GetUpdatesFromEntity(object o, SqliteParameterCollection parameters)
        {
            if (!(o is SqliteEntity))
                throw new Exception("o doit être de type SqliteObservableEntity");
            // On parcours toutes les colonnes de o et on ajoutes au paramètres et on construite la chaine d'UPDATE xx = @xx AND yy = @ yy etc
            var columns = EntityMap.Get(o.GetType()).Columns;

            string updateString = "";
            foreach(var col in columns)
            {
                if (col.IsPrimaryKey)
                    continue;
                if (!string.IsNullOrEmpty(col.ColumnName))
                {
                    if(!string.IsNullOrEmpty(updateString)) { updateString += ", "; }
                    updateString += $" {col.ColumnName} = @{col.ColumnName}";
                    parameters.AddWithValue($"@{col.ColumnName}", GetSqliteValue(col.PropertyInfo, o));
                }
                else
                {
                    object v = col.PropertyInfo.GetValue(o);
                    string subString = GetUpdatesFromEntity(v, parameters);
                    if (subString != null) 
                    {
                        if (!string.IsNullOrEmpty(updateString)) { updateString += ", "; }
                        updateString += subString;
                    }
                }
            }
            return updateString;
        }

        public static void Update(T entity, QueryProvider provider)
        {
            CheckConnection(provider.Connection);
             
            var map = EntityMap.Get(entity.GetType());

            var columns = map.Columns;
            
            string tableName = map.TableName;
            using var cmd = provider.Connection.CreateCommand();

            cmd.CommandText = $"UPDATE {tableName} SET ";

            string updtString = GetUpdatesFromEntity(entity, cmd.Parameters);
            cmd.CommandText += updtString;
            
            // Where
            cmd.CommandText += " WHERE ";

            var keys = columns.Where(c => c.IsPrimaryKey);
            string where = "";
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(where))
                    where += " AND ";
                where += $"{key.ColumnName} = @{key.ColumnName}";
                cmd.Parameters.AddWithValue($"@{key.ColumnName}", GetSqliteValue(key.PropertyInfo, entity));
            }

            cmd.CommandText += where;
            cmd.ExecuteNonQuery();
        }


        private static void CheckConnection(SqliteConnection? sqliteConnection)
        {
            if (sqliteConnection == null)
            {
                throw new Exception("Impossible d'exécuter une requête, aucune connexion n'est définié.");
            }
        }

        private static string[] GetColumnsInsertQuery(object entity)
        {
            string columnsList = "";
            string parametersList = "";
            string onConflictList = "";
            bool first = true;
            bool firstConflict = true;

            foreach (var column in EntityMap.Get(entity.GetType()).Columns)
            {
                if (column.IsPrimaryKey) continue;
                columnsList += !first ? "," : "";
                parametersList += !first ? "," : "";
                if (string.IsNullOrEmpty(column.ColumnName))
                {
                    // on doit aller récupérer les colonnes liées au type de l'objet
                    string[] objParts = GetColumnsInsertQuery(column.PropertyInfo.GetValue(entity));
                    columnsList += objParts[0];
                    parametersList += objParts[1];
                }
                else
                {
                    columnsList += column.ColumnName;
                    parametersList += "@" + column.ColumnName;

                   /* if (column.IsOnconflict)
                    {
                        onConflictList += !firstConflict ? "," : "";
                        onConflictList += column.ColumnName;
                        firstConflict = false;
                    }*/
                }
                first = false;
            }

            return [columnsList, parametersList, onConflictList];
        }

        private static void PopulateInsertCommand(SqliteCommand cmd, object item)
        {
            var columns = EntityMap.Get(item.GetType()).Columns;
            foreach (var column in columns)
            {
                if (column.IsPrimaryKey)
                    continue;
                if (string.IsNullOrEmpty(column.ColumnName))
                {
                    var instance = column.PropertyInfo.GetValue(item);
                    if (instance != null)
                    {
                        //cmd.Parameters.AddWithValue("@" + column.Value.ColumnName, column.Value.PropertyInfo.GetValue(instance) ?? DBNull.Value);
                        PopulateInsertCommand(cmd, instance);
                    }
                }
                else
                {
                    cmd.Parameters.AddWithValue("@" + column.ColumnName, GetSqliteValue(column.PropertyInfo, item));
                }
            }
        }

        public static object GetSqliteValue(PropertyInfo property, object item)
        {
            Type type = property.PropertyType;
            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    DateTime? date = (DateTime)property.GetValue(item);
                    object strDate = date?.ToString("yyyy-MM-dd HH:mm:ss");
                    return strDate ?? DBNull.Value;
                default:
                    return property.GetValue(item) ?? DBNull.Value;

            }

        }

    }

}
