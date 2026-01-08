using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace Linqlite.Hydration
{
    public static class HydratorBuilder
    {
        public static T GetEntity<T>(this SqliteDataReader reader) where T : SqliteEntity, new()
        {
            try
            {
                T entity = EntityFactory<T>.CreateInstance();
                foreach (var column in EntityMap.Get(typeof(T)).Columns)
                {
                    try
                    {
                        column.PropertyInfo.SetValue(entity, reader.GetValue(column));
                    }
                    catch (Exception ex) { }
                }
                entity.IsNew = false;
                return entity;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static void SetPrimaryKey<T>(T entity, long id) where T : SqliteEntity
        {
            var primary = EntityMap.Get(typeof(T)).Columns.Single(c => c.IsPrimaryKey);
            primary.PropertyInfo.SetValue(entity, id);
        }

    }

}
