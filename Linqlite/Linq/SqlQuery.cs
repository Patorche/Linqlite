using Linqlite.Hydration;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Principal;
using System.Text;
using System.Transactions;

namespace Linqlite.Linq
{
    public static class SqlQuery<T> where T : SqliteEntity, new()
    {
        public static IEnumerable<T> Execute(string sql, LinqliteProvider provider, TrackingMode trackingMode, IReadOnlyDictionary<string, object> parameters)
        {
            CheckConnection(provider.Connection);
            
            var command = provider?.Connection?.CreateCommand() ?? throw new InvalidOperationException("Le provider a retourné un commande null !");
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
                    T entity = HydratorBuilder.GetEntity<T>(reader) ?? throw new InvalidDataException("Entité null retournée");
                    provider?.Attach(entity, trackingMode);
                    yield return entity;
                }
            }
            finally 
            { 
                reader.Dispose(); 
                command.Dispose(); 
            }
        }


        public static long InsertOrGetId(T entity, LinqliteProvider provider, TrackingMode trackingMode)
        {
            CheckConnection(provider.Connection);

            var map = EntityMap.Get(typeof(T)) ?? throw new UnreachableException("Tentaive d'insertion d'un objet non mappé");

            IUniqueConstraint? upsertKey = map.GetUpsertKey();
            if(upsertKey == null)
                throw new Exception("InsertOrGetId requière un groupe unique. Vous devez définir une propriété Groupe. Utilisez Insert sinon");

            StringBuilder sb = new StringBuilder();
            sb.Append($"INSERT INTO {map.TableName}");

            var ColsParams = GetColumnsInsertQuery(entity);
            sb.Append("(" + ColsParams.Columns + ")")
                .Append(" VALUES(" + ColsParams.Parameters + ")");

            var cols = string.Join(", ", upsertKey.Columns.Select(c => $"\"{c}\""));
            sb.Append(" ON CONFLICT (" + cols + ")");
            sb.Append($" DO UPDATE SET {upsertKey.Columns[0]} = excluded.{upsertKey.Columns[0]}");
            sb.Append(" RETURNING " + map.Columns.Single(c => c.IsPrimaryKey).ColumnName);
            sb.Append(";");

            using var cmd = provider?.Connection?.CreateCommand() ?? throw new InvalidOperationException("Le provider a retourné un commande null !");
            PopulateInsertCommand(cmd, entity);

            var sql = sb.ToString();
            provider.Logger?.Log(sql);
            cmd.CommandText = sql;
            var res = cmd.ExecuteScalar();
            long id = Convert.ToInt64(res);
            HydratorBuilder.SetPrimaryKey(entity, id);
            provider.Attach(entity, trackingMode);
            return id;
        }

        public static long Insert(T entity, LinqliteProvider provider, TrackingMode trackingMode)
        {
            CheckConnection(provider.Connection);

            var map = EntityMap.Get(typeof(T)) ?? throw new UnreachableException("Tentaive d'insertion d'un objet non mappé");

            StringBuilder sb = new StringBuilder();
            sb.Append($"INSERT INTO {map.TableName}");

            var ColsParams = GetColumnsInsertQuery(entity);
            sb.Append("(" + ColsParams.Columns + ")")
                .Append(" VALUES(" + ColsParams.Parameters + ")");

            using var cmd = provider?.Connection?.CreateCommand() ?? throw new InvalidOperationException("Le provider a retourné un commande null !");
            PopulateInsertCommand(cmd, entity);

            var sql = sb.ToString();
            provider.Logger?.Log(sql);
            cmd.CommandText = sql;
            var res = cmd.ExecuteNonQuery();
            
            if (res == 1)
            {
                var idQuery = $"SELECT last_insert_rowid();";
                var q = new SqliteCommand(idQuery, provider.Connection);
                provider.Attach(entity, trackingMode);
                var id = Convert.ToInt64(q.ExecuteScalar()!);
                HydratorBuilder.SetPrimaryKey(entity, id);
                provider.Attach(entity, trackingMode);
                return id;
            }

            return -1;
        }



        public static void Delete(T entity, LinqliteProvider provider)
        {
            CheckConnection(provider.Connection);

            var map = EntityMap.Get(typeof(T)) ?? throw new UnreachableException("Tentaive de suppression d'un objet non mappé");
            var table = map.TableName;
            var key = map.GetPrimaryKey();

            StringBuilder sb = new StringBuilder();
            
            sb.Append($"DELETE FROM {table} WHERE ");
            sb.Append($"{key.ColumnName} = @{key.ColumnName}");

            using var cmd = provider?.Connection?.CreateCommand() ?? throw new InvalidOperationException("Le provider a retourné un commande null !");
            cmd.Parameters.AddWithValue($"@{key.ColumnName}", GetSqliteValue(key.PropertyInfo, entity));

            var sql = sb.ToString();
            provider.Logger?.Log(sql);
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            provider.Detach(entity);
        }

        internal static void Update(T entity, string? property, LinqliteProvider provider)
        {
            ArgumentNullException.ThrowIfNull(property);

            CheckConnection(provider.Connection);

            if (string.IsNullOrEmpty(property)) return;

            var map = EntityMap.Get(entity.GetType()) ?? throw new UnreachableException("Tentaive de mise à jour d'un objet non mappé");
            var tableName = map.TableName;
            var key = map.GetPrimaryKey();

            var columns = map.Columns;
            var column = columns.SingleOrDefault(c => c.PropertyInfo.Name == property);
            if (column == default(EntityPropertyInfo))
                return;

            
            using var cmd = provider?.Connection?.CreateCommand() ?? throw new InvalidOperationException("Le provider a retourné un commande null !"); ;

            StringBuilder sb = new StringBuilder();
            sb.Append($"UPDATE {tableName} SET ");

            if (!string.IsNullOrEmpty(column.ColumnName))
            {
                sb.Append($" {column.ColumnName} = @{column.ColumnName}");
                cmd.Parameters.AddWithValue($"@{column.ColumnName}", GetSqliteValue(column.PropertyInfo, entity));
            }
            else
            {
                object? v = column?.PropertyInfo?.GetValue(entity);
                if (v != null)
                {
                    string updtString = GetUpdatesFromEntity(v, cmd.Parameters);
                    sb.Append(updtString);
                }
            }
            // Where
            sb.Append(" WHERE ");
            sb.Append($"{key.ColumnName} = @{key.ColumnName}");
            cmd.Parameters.AddWithValue($"@{key.ColumnName}", GetSqliteValue(key.PropertyInfo, entity));

            var sql = sb.ToString();
            provider.Logger?.Log(sql);
            cmd.CommandText += sql;
            cmd.ExecuteNonQuery();
        }

        private static string GetUpdatesFromEntity(object o, SqliteParameterCollection parameters)
        {
            if (o is not SqliteEntity)
                throw new Exception("o doit être de type SqliteEntity");
           
            // On parcours toutes les colonnes de o et on ajoutes au paramètres et on construite la chaine d'UPDATE xx = @xx AND yy = @ yy etc
            var map = EntityMap.Get(o.GetType()) ?? throw new UnreachableException("Tentative de mise à jour d'un objet non mappé");
            var columns = map.Columns;

            StringBuilder updateString = new StringBuilder();
            foreach(var col in columns)
            {
                if (col.IsPrimaryKey)
                    continue;
                if (!string.IsNullOrEmpty(col.ColumnName))
                {
                    if (updateString.Length > 0)
                        updateString.Append(", ");
                    updateString.Append($" {col.ColumnName} = @{col.ColumnName}");
                    parameters.AddWithValue($"@{col.ColumnName}", GetSqliteValue(col.PropertyInfo, o));
                }
                else
                {
                    object? v = col.PropertyInfo.GetValue(o);
                    if (v != null)
                    {
                        string subString = GetUpdatesFromEntity(v, parameters);
                        if (subString != null)
                        {
                            if (updateString.Length > 0)
                                updateString.Append(", ");
                            updateString.Append(subString);
                        }
                    }
                }
            }
            return updateString.ToString();
        }

        public static void Update(T entity, LinqliteProvider provider)
        {
            CheckConnection(provider.Connection);
             
            var map = EntityMap.Get(entity.GetType()) ?? throw new UnreachableException("Tentative de mise à jour d'un objet non mappé");
            string tableName = map.TableName;
            var columns = map.Columns;
            var key = map.GetPrimaryKey();
            
            StringBuilder sb = new StringBuilder();

            sb.Append($"UPDATE {tableName} SET ");

            using var cmd = provider?.Connection?.CreateCommand() ?? throw new InvalidOperationException("Le provider a retourné un commande null !"); ;
            string updtString = GetUpdatesFromEntity(entity, cmd.Parameters);
            sb.Append(updtString);
            
            sb.Append(" WHERE ");

            sb.Append($"{key.ColumnName} = @{key.ColumnName}");
            cmd.Parameters.AddWithValue($"@{key.ColumnName}", GetSqliteValue(key.PropertyInfo, entity));

            var sql = sb.ToString();
            provider.Logger?.Log(sql);
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }


        private static void CheckConnection(SqliteConnection? sqliteConnection)
        {
            if (sqliteConnection == null)
            {
                throw new Exception("Impossible d'exécuter une requête, aucune connexion n'est définié.");
            }
        }

        private static (string Columns, string Parameters) GetColumnsInsertQuery(object entity)
        {
            string columnsList = "";
            string parametersList = "";
            bool first = true;
            var map = EntityMap.Get(entity.GetType()) ?? throw new UnreachableException("Tentative d'insertion' d'un objet non mappé");
            foreach (var column in map.Columns)
            {
                if (column.IsPrimaryKey) continue;

                columnsList += !first ? "," : "";
                parametersList += !first ? "," : "";
                if (string.IsNullOrEmpty(column.ColumnName))
                {
                    // on doit aller récupérer les colonnes liées au type de l'objet
                    object? o = column?.PropertyInfo?.GetValue(entity);
                    if (o != null)
                    {
                        (string Columns, string Parameters) objParts = GetColumnsInsertQuery(o);
                        columnsList += objParts.Columns;
                        parametersList += objParts.Parameters;
                    }
                }
                else
                {
                    columnsList += column.ColumnName;
                    parametersList += "@" + column.ColumnName;
                }
                first = false;
            }

            return (columnsList, parametersList);
        }

        private static void PopulateInsertCommand(SqliteCommand cmd, object item)
        {
            var map = EntityMap.Get(item.GetType()) ??  throw new UnreachableException("Tentative d'insertion' d'un objet non mappé");
            var columns = map.Columns;
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
            Type? type = property.PropertyType;
            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.DateTime:
                    object? o = property?.GetValue(item) ;
                    DateTime? date =  o as DateTime?;
                    object? strDate = date?.ToString("yyyy-MM-dd HH:mm:ss");
                    return strDate ?? DBNull.Value;
                default:
                    return property.GetValue(item) ?? DBNull.Value;

            }

        }

    }

}
