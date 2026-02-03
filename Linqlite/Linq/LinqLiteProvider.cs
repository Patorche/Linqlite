using Linqlite.Linq.Relations;
using Linqlite.Linq.SqlExpressions;
using Linqlite.Linq.SqlGeneration;
using Linqlite.Linq.SqlVisitor;
using Linqlite.Logger;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using Linqlite.Utils;
using Microsoft.Data.Sqlite;
using OneOf.Types;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Linqlite.Linq
{
    public class LinqliteProvider : IQueryProvider, IDisposable
    {

        private static readonly HashSet<string> _terminalOperators = [.. Enum.GetNames<TerminalOperator>()];
        private readonly List<IQueryableTableDefinition> _queries = [];
        private readonly Dictionary<object, PropertyChangedEventHandler> _handlers = [];
        private string _dbFilename = "";
        private SchemaManager _schemaManager;
        private bool _isWithRelation = false;
        private SqlitePragmas _pragmas;


        internal SqliteConnection? Connection = null;
        private bool _hasUserProjection = true;
        private bool _fromTerminal = false;

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

        public LinqliteProvider(string dbFilename, Action<SqlitePragmas>? configure = null)
        {
            DbFileName = dbFilename;
            _pragmas = new SqlitePragmas(); 
            configure?.Invoke(_pragmas);
            _schemaManager = new SchemaManager(this);
            Connect();
        }

        public LinqliteProvider(string dbFilename, SqlitePragmas pragmas)
        {
            DbFileName = dbFilename;
            _pragmas = pragmas;
            _schemaManager = new SchemaManager(this);
            Connect();
        }

       

        private void Connect()
        {
            if (string.IsNullOrEmpty(DbFileName)) return;

            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
            Connection = new SqliteConnection(@$"Data Source={DbFileName}");
            Connection.Open();
            ApplyPragmas();
        }

       /* public void Optimize()
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
        }*/

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
            _isWithRelation = false;
            var table = FindRootTable(expression);
            var mode = table?.TrackingModeOverride ?? DefaultTrackingMode;

            if (expression is SqlWithRelationsExpression)
            {
                _isWithRelation = true;
                return ExecuteSequence(expression, mode);
            }

            var termexp = new TerminalVisitor().Visit(expression);
           

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
            SqlExpression? exp = null;
            if (expression is not SqlExpression)

            {
                SqlTreeBuilderVisitor visitor = new(this);
                exp = visitor.Build(expression);
            }
            else
            {
                exp = expression as SqlExpression;
            }
            if (exp is SqlGroupByExpression groupByExpression)
            {
                var method = typeof(LinqliteProvider).GetMethod(nameof(ExecuteGroupBy), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(groupByExpression.OriginalKeySelector.ReturnType, groupByExpression.ElementType);
                //return ExecuteGroupBy(groupByExpression, mode);
                var result = method.Invoke(this, new object[] { groupByExpression, mode }); 
                return result;
            }

            if (exp is SqlSelectManyExpression sm) return ExecuteSelectMany(sm, mode);


            SqlGenerator gen = new SqlGenerator();
            var sql = gen.Generate(exp);
            Logger?.Log(sql);
            var elementType = TypeSystem.GetElementType(expression.Type);
            var p = ((SqlSelectExpression)exp).Projection;
            if (p != null) 
            {
                elementType = p.Type;
                _hasUserProjection = true;
            }
            else
            {
                p = ((SqlSelectExpression)exp).DefaultProjection;
                elementType = p.Type;
                _hasUserProjection = true;
            }


            if (TypesUtils.IsEntityType(elementType))
            {
                return ExecuteEntitySequence(elementType, sql, mode, gen.Parameters);
            }
            else
            {
                return ExecuteProjectionSequence(elementType, sql, gen.Parameters, (SqlMemberProjectionExpression)p);
            }
        }

        private IEnumerable<IGrouping<TKey, TElement>> ExecuteGroupBy<TKey, TElement>(SqlGroupByExpression expr, TrackingMode mode)
        {
            var source = ExecuteSequence(expr.Source, mode) as IEnumerable;

            var keySelector = expr.KeySelector.Compile();
            var elementSelector = expr.ElementSelector?.Compile();
            var originalKeySelector = expr.OriginalKeySelector.Compile();

            var select = (SqlSelectExpression)expr.Source;
            var elementType = select.ElementType;

            var groups = new Dictionary<object, IList>();
            var listType = typeof(List<>).MakeGenericType(elementType);

            foreach (var item in source!)
            {
                var key = keySelector.DynamicInvoke(item); // toujours la clé interne (Id)

                var value = elementSelector != null
                    ? elementSelector.DynamicInvoke(item)
                    : item;

                if (!groups.TryGetValue(key!, out var list))
                {
                    list = (IList)Activator.CreateInstance(listType)!;
                    groups[key!] = list;
                }

                list.Add(value!);
            }


            var keyType = expr.KeySelector.ReturnType;
            var groupingType = typeof(Grouping<,>).MakeGenericType(expr.OriginalTypeIsEntity ? expr.OriginalKeySelector.ReturnType : keyType, expr.ElementType);

            foreach (var g in groups)
            {
                var internalKey = g.Key;
                var values = g.Value;

                object key;

                if (expr.OriginalTypeIsEntity)
                {
                    key = originalKeySelector.DynamicInvoke(values[0]);
                }
                else
                {
                    key = internalKey;
                }
                var grouping = Activator.CreateInstance(groupingType, key, values);
                yield return (IGrouping<TKey, TElement>)grouping;
            }

        }

  

        private IEnumerable ExecuteSelectMany(SqlSelectManyExpression expr, TrackingMode mode)
        {
            var source = ExecuteSequence(expr.Source, mode) as IEnumerable;

            var colSel = expr.CollectionSelector.Compile();
            var resSel = expr.ResultSelector.Compile();

            foreach (var item in source!)
            {
                var collection = colSel.DynamicInvoke(item) as IEnumerable;

                if (collection == null)
                    continue;

                foreach (var inner in collection)
                    yield return resSel.DynamicInvoke(item, inner);
            }
        }




        private object? ExecuteTerminal(MethodCallExpression mce, TrackingMode mode)
        {
            _fromTerminal = true;
            // La source de la requête (avant First/Single)
            var sourceExpression = mce.Arguments[0];

            // Exécuter la requête source
            var sequence = ExecuteSequence(sourceExpression, mode);

            var elementType = TypeSystem.GetElementType(sourceExpression.Type);
       
            var res = RootEntityExtractor.ExtractRootEntities((IEnumerable)sequence);

            var method = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast));
            var castMethod = method?.MakeGenericMethod(elementType);
            var casted = castMethod?.Invoke(null, new[] { res });

            // Convertir en IEnumerable<T>
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

            if (typeof(TResult) == result.GetType()) // Cas   de retour atomique
                return (TResult)result;
            // Sinon, on a un enumerable
            if (typeof(TResult).GetGenericArguments()[0] == result.GetType().GetGenericArguments()[0])
                return (TResult)result;

            if (!_fromTerminal && (_isWithRelation || _hasUserProjection))
            {
                var enumerable = ((IEnumerable)result).Cast<object>();
                var res = RootEntityExtractor.ExtractRootEntities(enumerable);

                var objectList = ((IEnumerable)res).Cast<object>().ToList();

                var anonType = typeof(TResult).GetGenericArguments()[0]; 
                var wrapperType = typeof(FakeEnumerable<>).MakeGenericType(anonType); 
                var wrapper = Activator.CreateInstance(wrapperType, objectList);

                return (TResult)wrapper;
            }

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
            table.Provider = this;
            Register(table); 
            return table; 
        }

        public IQueryableTableDefinition GetTable(Type t)
        {
            var table = _queries.SingleOrDefault(q => q.EntityType == t);
            if(table == null)
            {
                var tb = Activator.CreateInstance(typeof(TableLite<>).MakeGenericType(t));
                var property = tb.GetType().GetProperty("Provider");
                if (property != null)
                {
                    property.SetValue(tb, this);
                }
                table = (IQueryableTableDefinition)tb;
                Register(table);
            }
            return table;
        }

        // Gestion des queryable pour génération  script SQl
        private void Register(IQueryableTableDefinition query)
        {
            var table = _queries.SingleOrDefault(q => q.EntityType == query.EntityType);
            if (table == null)
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

        private void ApplyPragmas()
        {
            using var cmd = Connection?.CreateCommand();

            foreach (var pragma in _pragmas.ToSqlCommands())
            {
                cmd?.CommandText = pragma;
                cmd?.ExecuteNonQuery();
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

    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        public TKey Key { get; }
        private readonly IEnumerable<TElement> _items;

        public Grouping(TKey key, IEnumerable<TElement> items)
        {
            Key = key;
            _items = items;
        }

        public IEnumerator<TElement> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class SelectFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        public bool ContainsSelect(Expression expr)
        {
            Visit(expr);
            return Found;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Select"
                && node.Method.DeclaringType == typeof(Queryable))
            {
                Found = true;
            }

            return base.VisitMethodCall(node);
        }
    }

    class FakeEnumerable<TAnon> : IEnumerable<TAnon>
    {
        private readonly IEnumerable _inner;

        public FakeEnumerable(IEnumerable inner)
        {
            _inner = inner;
        }

        public IEnumerator<TAnon> GetEnumerator()
            => new FakeEnumerator<TAnon>(_inner.GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    class FakeEnumerator<TAnon> : IEnumerator<TAnon>
    {
        private readonly IEnumerator _inner;

        public FakeEnumerator(IEnumerator inner)
        {
            _inner = inner;
        }

        public TAnon Current
        {
            get
            {
                // On renvoie l'objet tel quel, SANS cast réel
                // Le cast (TAnon) ne sera jamais vérifié par LINQ
                return (TAnon)_inner.Current;
            }
        }

        object IEnumerator.Current => _inner.Current;

        public bool MoveNext() => _inner.MoveNext();
        public void Reset() => _inner.Reset();
        public void Dispose() { }
    }



}