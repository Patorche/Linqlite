using Linqlite.Linq.SqlExpressions;
using Linqlite.Linq.SqlGeneration;
using Linqlite.Linq.SqlVisitor;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    public class QueryProvider : IQueryProvider, IDisposable
    {
        private static readonly HashSet<string> _terminalOperators = Enum.GetNames(typeof(TerminalOperator)).ToHashSet();
        public TrackingMode DefaultTrackingMode { get; set; } = TrackingMode.AutoUpdate;
        private List<IQueryableTableDefinition> _queries = new();

        private string _connectionString = "";
        internal SqliteConnection? Connection = null;
        public string DatabaseScript { get; private set; }

        

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
            Connection = new SqliteConnection(@$"Data Source={ConnectionString}");
            Connection.Open();
            Optimize();
        }

        private void Optimize()
        {
            using (var command = Connection?.CreateCommand())
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




        public long Insert<T>(T entity) where T : SqliteEntity, new()
        {
            return SqlQuery<T>.Insert(entity, this);
        }

        public void Delete<T>(T entity) where T : SqliteEntity, new()
        {
            SqlQuery<T>.Delete(entity, this);
        }

        public void Update<T>(T entity, string property) where T : SqliteEntity, new()
        {
            SqlQuery<T>.Update(entity, property, this);
        }


       

        public object? Execute(Expression expression)
        {
            // 1. opérateur terminal ?
            if (expression is MethodCallExpression mce && IsTerminalOperator(mce.Method.Name))
            {
                return ExecuteTerminal(mce);
            }

            // 2. Sinon, exécution normale (ToList, enumeration, etc.)
            return ExecuteSequence(expression);
        }


        private object? ExecuteSequence(Expression expression)
        {
            TrackingMode? trackingMode;
            //var sql = Translate(expression, out trackingMode);
            //SqlExpressionVisitor visitor = new SqlExpressionVisitor();
            SqlTreeBuilderVisitor visitor = new SqlTreeBuilderVisitor();
            SqlExpression exp = visitor.Build(expression);
            SqlGenerator gen = new SqlGenerator();
            var sql = gen.Generate(exp);
            //var sql = visitor.Translate(expression);
            trackingMode = DefaultTrackingMode;//visitor.TrackingMode;
            var elementType = TypeSystem.GetElementType(expression.Type);

            var method = typeof(SqlQuery<>)
                .MakeGenericType(elementType)
                .GetMethod(nameof(SqlQuery<SqliteEntity>.Execute));
            return method.Invoke(null, new object[] { sql, this, trackingMode, gen.Parameters });
        }

        private object? ExecuteTerminal(MethodCallExpression mce)
        {
            // La source de la requête (avant First/Single)
            var sourceExpression = mce.Arguments[0];

            // Exécuter la requête source
            var sequence = ExecuteSequence(sourceExpression);

            // Convertir en IEnumerable<T>
            var elementType = TypeSystem.GetElementType(sourceExpression.Type);
            var castMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(elementType);

            var casted = castMethod.Invoke(null, new[] { sequence });


            return EnumerableTerminalOperator(elementType, casted, mce.Method.Name);
        }

        private object EnumerableTerminalOperator(Type elementType, object casted, string op)
        {
            var method = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == op && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);

            return method.Invoke(null, new[] { casted });
        }

        private bool IsTerminalOperator(string op)
        {
            return _terminalOperators.Contains(op);
        }


        private string Translate(Expression expression, out TrackingMode? trackingMode)
        {
            trackingMode = null;
            return "";
            //SqlExpressionVisitor visitor = new SqlExpressionVisitor();
            //trackingMode = visitor.TrackingMode;
            //return visitor.Translate(expression);
        }

        public TResult? Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);
        }

        public void Attach(SqliteEntity entity, TrackingMode? mode = TrackingMode.AutoUpdate)
        {
            switch (mode)
            {
                case TrackingMode.None:
                    // On ne tracke pas du tout
                    break;

                case TrackingMode.AutoUpdate:
                    entity.PropertyChanged += (s, e) =>
                    {
                        Update(entity, e.PropertyName);
                    };
                    break;

                case TrackingMode.Manual:
                    break;
            }
        }

        // Gestion des queryable pour génération  script SQl
        public void Register<T>(QueryableTable<T> query) where T : SqliteEntity
        {
            query.Provider = this;
            _queries.Add(query);
        }

        public void CreateDatabase(string filename = "")
        {
            string cnx = string.IsNullOrEmpty(filename) ? ConnectionString : filename;
            if (File.Exists(cnx))
            {
                throw new Exception($"Le fichier {cnx} existe déjà");
            }

            List<TableScriptGenerator> tablesScripts = new();
            foreach (var query in _queries)
            {
                Type type = query.EntityType;
                TableScriptGenerator scriptGen = new TableScriptGenerator();
                scriptGen.Build(type);
                tablesScripts.Add(scriptGen);
            }

            List<TableScriptGenerator> tables = SortByDependencies(tablesScripts);

            StringBuilder sb = new StringBuilder();
            foreach (var table in tables)
            {
                sb.Append(table.Script).AppendLine();
            }
            DatabaseScript = sb.ToString();

           
            Disconnect();
            ConnectionString = filename;
            Connect();

            SqliteCommand cmdTables = new SqliteCommand();
            cmdTables.Connection = Connection;
            cmdTables.CommandText = DatabaseScript;
            cmdTables.ExecuteNonQuery();

        }

        public async void Dispose()
        {
            Disconnect();
        }

        public async void Disconnect()
        {
            if (Connection != null)
            {
                await Connection.CloseAsync();
                Connection.Dispose();
            }
        }

        private static List<TableScriptGenerator> SortByDependencies(List<TableScriptGenerator> tables)
        {
            // 1. Construire le graphe
            var graph = tables.ToDictionary(
                t => t.EntityType,
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
            return sorted
                .Select(type => tables.First(t => t.EntityType == type))
                .ToList();
        }


    }

    enum TerminalOperator
    {
        First, FirstOrDefault, Single, SingleOrDefault, Any, Count
    }

    public enum TrackingMode 
    { 
        None, 
        AutoUpdate, 
        Manual 
    }
}