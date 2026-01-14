using Linqlite.Linq.SqlExpressions;
using Linqlite.Linq.SqlGeneration;
using Linqlite.Linq.SqlVisitor;
using Linqlite.Logger;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Linqlite.Linq
{
    public class LinqliteProvider : IQueryProvider, IDisposable
    {
        private static readonly HashSet<string> _terminalOperators = [.. Enum.GetNames<TerminalOperator>()];
        private readonly List<IQueryableTableDefinition> _queries = [];
        private readonly Dictionary<object, PropertyChangedEventHandler> _handlers = [];
        private string _dbFilename = "";
        private SchemaManager _schemaManager;

        internal SqliteConnection? Connection = null;

        public ILinqliteLogger? Logger { get; set; }

        public TrackingMode DefaultTrackingMode { get; set; } = TrackingMode.AutoUpdate;

        public string? DatabaseScript => _schemaManager?.DatabaseScript;


        public string DbFileName
        {
            get => _dbFilename;
            private set => _dbFilename = value;
        }

        public LinqliteProvider()
        {
            _schemaManager = new SchemaManager(this);
        }

        public LinqliteProvider(string dbFilename)
        {
            DbFileName = dbFilename;
            _schemaManager = new SchemaManager(this);
            Connect();
        }

        bool IsEntityType(Type t) => typeof(SqliteEntity).IsAssignableFrom(t);

        private void Connect()
        {
            if (string.IsNullOrEmpty(DbFileName)) return;

            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
            Connection = new SqliteConnection(@$"Data Source={DbFileName}");
            Connection.Open();
            Optimize();
        }

        public void Optimize()
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
                object? o = Activator.CreateInstance(orderedType, this, expression);
                return o == null ? throw new InvalidOperationException("Echec lors de l'instancation de IQueryable") : (IQueryable)o;
            }
            else
            {
                var elementType = expression.Type.GetGenericArguments()[0];
                var normalType = typeof(TableLite<>).MakeGenericType(elementType);
                object? o = Activator.CreateInstance(normalType, this, expression);
                return o == null ? throw new InvalidOperationException("Echec lors de l'instancation de IQueryable") : (IQueryable)o;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (typeof(IOrderedQueryable<TElement>).IsAssignableFrom(expression.Type))
            {
                return new OrderedQueryableTable<TElement>(this, expression);
            }

            return new TableLite<TElement>(this, expression);
        }


        public long Insert<T>(T entity, TrackingMode mode) where T : SqliteEntity, new()
        {
            return SqlQuery<T>.Insert(entity, this, mode);
        }

        public long InsertOrGetId<T>(T entity, TrackingMode mode) where T : SqliteEntity, new()
        {
            return SqlQuery<T>.InsertOrGetId(entity, this, mode);
        }

        public void Delete<T>(T entity) where T : SqliteEntity, new()
        {
            SqlQuery<T>.Delete(entity, this);
        }

        public void Update<T>(T entity, string? property) where T : SqliteEntity, new()
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
            Logger?.Log(sql);
            var elementType = TypeSystem.GetElementType(expression.Type);

            if (IsEntityType(elementType))
            {
                return ExecuteEntitySequence(elementType,sql,mode,gen.Parameters);
            }
            else
            {
                return ExecuteProjectionSequence(elementType, sql, gen.Parameters, (SqlMemberProjectionExpression)((SqlSelectExpression)exp).Projection);
            }
        }

        

        private object? ExecuteTerminal(MethodCallExpression mce, TrackingMode mode)
        {
            // La source de la requête (avant First/Single)
            var sourceExpression = mce.Arguments[0];

            // Exécuter la requête source
            var sequence = ExecuteSequence(sourceExpression, mode);

            // Convertir en IEnumerable<T>
            var elementType = TypeSystem.GetElementType(sourceExpression.Type);
            var method = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast));
            var castMethod = method?.MakeGenericMethod(elementType);

            var casted = castMethod?.Invoke(null, new[] { sequence });


            return EnumerableTerminalOperator(elementType, casted, mce.Method.Name);
        }

        private static object? EnumerableTerminalOperator(Type elementType, object? casted, string op)
        {
            if (casted is null) throw new InvalidOperationException($"L'opérateur terminal '{op}' ne peut être appliqué à une séquence null.");

            var method = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == op && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);

            return method.Invoke(null, new[] { casted });
        }

        private object? ExecuteEntitySequence(Type? elementType, string sql, TrackingMode mode, IReadOnlyDictionary<string, object> parameters)
        {
            var method = typeof(SqlQuery<>)
                .MakeGenericType(elementType)
                .GetMethod(nameof(SqlQuery<SqliteEntity>.Execute));
            return method?.Invoke(null, new object[] { sql, this, mode, parameters });
        }


        private object? ExecuteProjectionSequence(Type elementType, string sql, IReadOnlyDictionary<string, object> parameters, SqlMemberProjectionExpression infos)
        {
            var method = typeof(ProjectionQuery<>)
                .MakeGenericType(elementType)
                .GetMethod(nameof(ProjectionQuery<object>.Execute));

            return method!.Invoke(null, new object[] { sql, this, parameters, infos });
        }


        private static bool IsTerminalOperatorWithoutPredicate(MethodCallExpression op)
        {
            
            if(!_terminalOperators.Contains(op.Method.Name)) return false;
            if(op.Arguments.Count>1) return false;
            return true;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var result = Execute(expression);
            if(result == null && typeof(TResult).IsValueType == false) 
                return default;

            return result == null
                ? throw new UnreachableException("Une  erreur est survenue lors de l'analyse de l'arbre d'expresssion.")
                : (TResult)result;
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

        public void Attach(SqliteEntity entity, TrackingMode mode)
        {
            var m = (mode == TrackingMode.Undefined) ? DefaultTrackingMode : TrackingMode.Undefined;
            switch (m)
            {
                case TrackingMode.None:
                    // On ne tracke pas du tout
                    break;

                case TrackingMode.AutoUpdate:
                    if (_handlers.ContainsKey(entity)) return;
                    PropertyChangedEventHandler handler = (s, e) => { Update(entity, e?.PropertyName); };
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

        public ITable<T> Table<T>(TrackingMode trackingMode = TrackingMode.Undefined) where T : SqliteEntity
        { 
            var table = new TableLite<T>(trackingMode);
            Register(table); 
            return table; 
        }

        // Gestion des queryable pour génération  script SQl
        private void Register<T>(TableLite<T> query) where T : SqliteEntity
        {
            query.Provider = this;
            _queries.Add(query);
        }


        public void EnsureTablesCreated()
        {
            _schemaManager.EnsureTablesCreated(_queries);
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

        
    }

    enum TerminalOperator
    {
        First,
        FirstOrDefault,
        Single,
        SingleOrDefault,
        Any,
        Count
    }

    public enum TrackingMode 
    { 
        None, 
        AutoUpdate, 
        Manual,
        Undefined
    }
}