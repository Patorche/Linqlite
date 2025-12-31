using Linqlite.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    public static class LinqliteTranslator
    {
        public static string Translate<T>(IQueryable<T> query)
        {
            SqlExpressionVisitor visitor = new SqlExpressionVisitor();
            return visitor.Translate(query.Expression);
        }
    }

}
