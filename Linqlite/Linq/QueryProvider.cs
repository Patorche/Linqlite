using Linqlite.Linq.SqlExpressions;
using Linqlite.Linq.SqlGeneration;
using Linqlite.Linq.SqlVisitor;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly Dictionary<object, PropertyChangedEventHandler> _handlers = new();

        private string _connectionString = "";
        internal SqliteConnection? Connection = null;
       
        public string? DatabaseScript { get; private set; }
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
            if (string.IsNullOrEmpty(ConnectionString)) return;
           // if(!File.Exists(ConnectionString)) return;

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


        public long Insert<T>(T entity, TrackingMode mode) where T : SqliteEntity, new()
        {
            return SqlQuery<T>.InsertOrGetId(entity, this, mode);
        }

        public long InsertOrGetId<T>(T entity, TrackingMode mode) where T : SqliteEntity, new()
        {
            return SqlQuery<T>.InsertOrGetId(entity, this, mode);
        }

        public void Delete<T>(T entity) where T : SqliteEntity, new()
        {
            SqlQuery<T>.Delete(entity, this);
        }

        public void Update<T>(T entity, string property) where T : SqliteEntity, new()
        {
            SqlQuery<T>.Update(entity, property, this);
        }

        public void Update<T>(T entity) where T : SqliteEntity, new()
        {
            SqlQuery<T>.Update(entity, this);
        }


        public object? Execute(Expression expression)
        {
            var termexp = new TerminalVisitor().Visit(expression);

            var table = FindRootTable(expression);
            var mode = table?.TrackingModeOverride ?? DefaultTrackingMode;
            

            // 1. opérateur terminal ?
            if (termexp is MethodCallExpression mce && IsTerminalOperatorWithoutPredicate(mce))
            {
                return ExecuteTerminal(mce, mode);
            }

            // 2. Sinon, exécution normale (ToList, enumeration, etc.)
            return ExecuteSequence(termexp, mode);
        }


        private object? ExecuteSequence(Expression expression, TrackingMode mode)
        {
            SqlTreeBuilderVisitor visitor = new SqlTreeBuilderVisitor();
            SqlExpression exp = visitor.Build(expression);
            SqlGenerator gen = new SqlGenerator();
            var sql = gen.Generate(exp);
            var elementType = TypeSystem.GetElementType(expression.Type);

            var method = typeof(SqlQuery<>)
                .MakeGenericType(elementType)
                .GetMethod(nameof(SqlQuery<SqliteEntity>.Execute));
            return method.Invoke(null, new object[] { sql, this, mode, gen.Parameters });
        }

        private object? ExecuteTerminal(MethodCallExpression mce, TrackingMode mode)
        {
            // La source de la requête (avant First/Single)
            var sourceExpression = mce.Arguments[0];

            // Exécuter la requête source
            var sequence = ExecuteSequence(sourceExpression, mode);

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

        private bool IsTerminalOperatorWithoutPredicate(MethodCallExpression op)
        {
            
            if(!_terminalOperators.Contains(op.Method.Name)) return false;
            if(op.Arguments.Count>1) return false;
            return true;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            //expression = ApplyTrackingMode(expression); 
            //expression = Normalize(expression); 
            //expression = Optimize(expression);
            
            return (TResult)Execute(expression);
        }

        internal IQueryableTableDefinition? FindRootTable(Expression expression)
        {
            while (true)
            {
                switch (expression)
                {
                    case ConstantExpression c:
                        return c.Value as IQueryableTableDefinition;

                    case MethodCallExpression m:
                        expression = m.Arguments.FirstOrDefault() ?? m.Object!;
                        continue;

                    case UnaryExpression u:
                        expression = u.Operand;
                        continue;

                    case MemberExpression me:
                        expression = me.Expression!;
                        continue;

                    default:
                        return null;
                }
            }
        }

        public void Attach(SqliteEntity entity, TrackingMode? mode = TrackingMode.AutoUpdate)
        {
            switch (mode)
            {
                case TrackingMode.None:
                    // On ne tracke pas du tout
                    break;

                case TrackingMode.AutoUpdate:
                    if (_handlers.ContainsKey(entity)) return;
                    PropertyChangedEventHandler handler = (s, e) => { Update(entity, e.PropertyName); };
                    _handlers[entity] = handler;
                    entity.PropertyChanged += handler;
                    break;

                case TrackingMode.Manual:
                    break;
            }
        }

        public void Detach(SqliteEntity entity)
        {
            if (_handlers.TryGetValue(entity, out var handler))
            { 
                entity.PropertyChanged -= handler; 
                _handlers.Remove(entity); 
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