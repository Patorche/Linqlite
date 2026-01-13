using Linqlite.Hydration;
using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Text;

namespace Linqlite.Linq
{
    internal class ProjectionQuery<T>
    {
        public static List<T?> Execute(string sql, LinqliteProvider provider, IReadOnlyDictionary<string, object> parameters, SqlMemberProjectionExpression infos)
        {
            if (provider.Connection == null) throw new ArgumentNullException("Aucune connection n'est active pour le Provider");
            using var cmd = provider.Connection.CreateCommand();// sql, parameters);
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            var list = new List<T?>();

            while (reader.Read())
            {
                T? e = default;
                if(IsAnonymousType(typeof(T)))
                    e = HydratorBuilder.HydrateAnonymous<T>(reader);
                else if (IsBaseType(typeof(T))){
                    e = (T)reader.GetValue(0);
                }
                else if (IsDto(typeof(T)))
                {
                    e = HydratorBuilder.HydrateDto<T>(reader, infos);
                }

                    list.Add(e);
            }

            return list;
        }

        private static bool IsAnonymousType(Type t) => t.Name.Contains("AnonymousType") && t.IsSealed && t.IsNotPublic;
        private static bool IsBaseType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t; 
                return t.IsPrimitive || 
                       t.IsEnum || 
                       t == typeof(string) || 
                       t == typeof(decimal) || 
                       t == typeof(DateTime) || 
                       t == typeof(Guid) || 
                       t == typeof(byte[]);
        }

        private static bool IsDto(Type t) => !IsBaseType(t) && !IsAnonymousType(t);

    }
}
