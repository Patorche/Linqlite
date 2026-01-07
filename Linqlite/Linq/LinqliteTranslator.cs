using Linqlite.Linq;
using Linqlite.Linq.SqlExpressions;
using Linqlite.Linq.SqlGeneration;
using Linqlite.Linq.SqlVisitor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    public static class LinqliteTranslator
    {
        public static string Translate<T>(IQueryable<T> query)
        {
            SqlTreeBuilderVisitor visitor = new SqlTreeBuilderVisitor();
            SqlExpression exp =  visitor.Build(query.Expression);
            SqlGenerator gen = new SqlGenerator();
            return gen.Generate(exp);
        }
    }

}
