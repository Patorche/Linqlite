using Linqlite.Mapping;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlGeneration
{
    internal class SchemaManager
    {
        QueryProvider _provider;
        public string DatabaseScript = "";

        public SchemaManager(QueryProvider queryProvider)
        {
            _provider = queryProvider;
        }
        public void EnsureTablesCreated(List<IQueryableTableDefinition> queries)
        {

            List<TableScriptGenerator> tablesScripts = new();
            foreach (var query in queries)
            {
                Type type = query.EntityType;
                TableScriptGenerator scriptGen = new TableScriptGenerator();
                scriptGen.Build(type);
                tablesScripts.Add(scriptGen);
            }

            List<TableScriptGenerator> tables = SortByDependencies(tablesScripts);

            DatabaseScript = "";
            StringBuilder sb = new StringBuilder();
            foreach (var table in tables)
            {
                CreateTableIfNotExists(table);
                UpdateTableIfNeeded(table?.EntityType);
                ;
                sb.Append(table?.Script);
            }
            DatabaseScript = sb.ToString();
        }

        private void CreateTableIfNotExists(TableScriptGenerator table)
        {
            SqliteCommand cmdTables = new SqliteCommand();
            cmdTables.Connection = _provider.Connection;
            cmdTables.CommandText = table.Script.ToString();
            cmdTables.ExecuteNonQuery();
        }

        private void UpdateTableIfNeeded(Type? type)
        {
            if (type == null)
            {
                return;
            }
            var map = EntityMap.Get(type);
            if (map == null)
                throw new InvalidDataException($"Aucun mapping pour le type {type.Name}");

            var tableName = map.TableName;

            var existingColumns = GetExistingColumns(tableName);

            List<EntityPropertyInfo> columns = map.GetFlattedColumnsList();
            foreach (var col in columns)
            {
                if (!existingColumns.Contains(col.ColumnName))
                {
                    var sql = $"ALTER TABLE {tableName} ADD COLUMN {col.ColumnName} {TableScriptGenerator.GetSqlType(col.PropertyType)}";

                    bool isNullable = Nullable.GetUnderlyingType(col.PropertyType) != null;
                    if (!isNullable)
                    {
                        sql += " NOT NULL DEFAULT " + GetDefaultValue(col.PropertyType);
                    }

                    SqliteCommand cmdTables = new SqliteCommand();
                    cmdTables.Connection = _provider.Connection;
                    cmdTables.CommandText = sql;
                    cmdTables.ExecuteNonQuery();
                }
            }
        }

        private string GetDefaultValue(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string))
                return "''";

            if (type == typeof(bool))
                return "0";

            if (type.IsEnum)
                return "0";

            if (type == typeof(int) || type == typeof(long) ||
                type == typeof(float) || type == typeof(double) ||
                type == typeof(decimal))
                return "0";

            // fallback : chaîne vide
            return "''";
        }


        private List<string> GetExistingColumns(string tableName)
        {
            var sql = $"PRAGMA table_info({tableName});";
            SqliteCommand cmd = new SqliteCommand();
            cmd.Connection = _provider.Connection;
            cmd.CommandText = sql;
            SqliteDataReader reader = cmd.ExecuteReader();

            List<string> list = [];
            while (reader.Read())
            {
                string col = reader.GetString(1);
                list.Add(col);
            }
            return list;
        }

        private static List<TableScriptGenerator> SortByDependencies(List<TableScriptGenerator> tables)
        {
            if (tables == null || tables.Count == 0) return new List<TableScriptGenerator>();
            // 1. Construire le graphe
            var graph = tables.ToDictionary(
                t => t.EntityType ?? throw new InvalidDataException("EntityType est null."),
                t => t.ForeignTables.ToList() // copie pour manipulation
            );

            // 2. Trouver les nœuds sans dépendances
            var noIncoming = new Queue<Type>(
                graph.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key)
            );

            var sorted = new List<Type>();

            // 3. Algorithme de Kahn
            while (noIncoming.Count > 0)
            {
                var n = noIncoming.Dequeue();
                sorted.Add(n);

                foreach (var kv in graph)
                {
                    if (kv.Value.Contains(n))
                    {
                        kv.Value.Remove(n);
                        if (kv.Value.Count == 0)
                            noIncoming.Enqueue(kv.Key);
                    }
                }
            }

            // 4. Vérification : cycle détecté ?
            if (graph.Any(kv => kv.Value.Count > 0))
                throw new InvalidOperationException("Cycle de dépendances détecté entre tables.");

            // 5. Retourner les TableScriptGenerator dans l'ordre trié
            return [.. sorted.Select(type => tables.First(t => t.EntityType == type))];
        }
    }
}
