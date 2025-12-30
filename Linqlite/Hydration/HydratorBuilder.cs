using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System.Linq.Expressions;

namespace Linqlite.Hydration
{
    public static class HydratorBuilder
    {
        public static T GetEntity<T>(this SqliteDataReader reader) where T : SqliteObservableEntity, new()
        {
            T entity = EntityFactory<T>.CreateInstance();
            foreach (var column in EntityMap.Get(typeof(T)).Columns)
            {
                /*if (column.PropertyPath.Length > 1)
                {
                    var objectInstance = column.PropertyPath[0].GetValue(entity);
                    if (objectInstance == null)
                    {
                        objectInstance = EntityDynamicFactory.CreateInstance(column.PropertyPath[1].PropertyType);
                        column.PropertyPath[1].SetValue(entity, objectInstance);
                    }
                    column.PropertyPath[0].SetValue(objectInstance, reader.GetValue(column));
                    //column.Value.CompiledSetter!(objectInstance, reader.GetValue(column.Value));
                }
                else*/
                {
                    column.PropertyInfo.SetValue(entity, reader.GetValue(column));
                    //var val = reader.GetValue(column.Value); 
                    //column.Value.CompiledSetter!(entity, val);
                }
            }
            entity.IsNew = false;
            return entity;
        }
        /* public static Func<SqliteDataReader, T> CompileHydrator<T>() where T : new()
         {
             var readerParam = Expression.Parameter(typeof(SqliteDataReader), "reader");
             var entityVar = Expression.Variable(typeof(T), "entity");

             var expressions = new List<Expression>
             {
                 Expression.Assign(entityVar, Expression.New(typeof(T)))
             };

             foreach (var col in EntityMap.Get(typeof(T)).Columns)
             {
                 // reader.GetValue("column")
                 var getValueCall = Expression.Call(
                     readerParam,
                     nameof(SqliteDataReader.GetValue),
                     null,
                     Expression.Constant(col.ColumnName)
                 );

                 Expression nested = entityVar;

                 // instancier les objets intermédiaires
                 for (int i = 0; i < col.PropertyPath.Length - 1; i++)
                 {
                     var p = col.PropertyPath[i];
                     var access = Expression.Property(nested, p);

                     var ensureNotNull = Expression.IfThen(
                         Expression.Equal(access, Expression.Constant(null)),
                         Expression.Assign(access, Expression.New(p.PropertyType))
                     );

                     expressions.Add(ensureNotNull);
                     nested = access;
                 }

                 // propriété finale
                 var finalProp = col.PropertyPath.Last();

                 var castValue = Expression.Convert(getValueCall, finalProp.PropertyType);

                 var assign = Expression.Assign(
                     Expression.Property(nested, finalProp),
                     castValue
                 );

                 expressions.Add(assign);
             }

             expressions.Add(entityVar);

             var body = Expression.Block(new[] { entityVar }, expressions);

             ExprDump.Dump(body);

             return Expression.Lambda<Func<SqliteDataReader, T>>(body, readerParam).Compile();
         }*/
    }

}
