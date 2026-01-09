using Linqlite.Mapping;
using Linqlite.Sqlite;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Linqlite.Hydration
{
    public static class HydratorBuilder
    {
        public static T? GetEntity<T>(this SqliteDataReader reader) where T : SqliteEntity, new()
        {
            try
            {
                T entity = EntityFactory<T>.CreateInstance();
                var map = EntityMap.Get(typeof(T));
                if (map == null)
                    return null;
                foreach (var column in map.Columns)
                {
                    try
                    {
                        column.PropertyInfo.SetValue(entity, reader.GetValue(column));
                    }
                    catch (Exception) { }
                }
                entity.IsNew = false;
                return entity;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void SetPrimaryKey<T>(T entity, long id) where T : SqliteEntity
        {
            var map = EntityMap.Get(typeof(T));
            if (map == null)
                throw new UnreachableException("Entitymap est null.");

            var primary = map.Columns.Single(c => c.IsPrimaryKey);
            primary.PropertyInfo.SetValue(entity, id);
        }

    }

}
