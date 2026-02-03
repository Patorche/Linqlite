using Linqlite.Hydration;
using Linqlite.Linq.SqlExpressions;
using Linqlite.Utils;
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
            
            foreach (var parameter in parameters)
            {
                cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }

            using var reader = cmd.ExecuteReader();

            var list = new List<T?>();

            while (reader.Read())
            {
                T? e = default;
                if(TypesUtils.IsCSharpAnonymousType(typeof(T)))
                    e = HydratorBuilder.HydrateAnonymous<T>(reader,provider, infos);
                if (TypesUtils.IsLinqliteAnonymousType(typeof(T)))
                    e = HydratorBuilder.HydrateLinqliteAnonymous<T>(reader, provider, infos);
                else if (TypesUtils.IsBaseType(typeof(T))){
                    e = (T)reader.GetValue(0);
                }
                else if (TypesUtils.IsDto(typeof(T)))
                {
                    e = HydratorBuilder.HydrateDto<T>(reader, infos);
                }

                    list.Add(e);
            }

            return list;
        }

        
        


    }
}
