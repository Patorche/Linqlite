using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlOrderByExpression : SqlSourceExpression
    {
        public SqlExpression Key { get; }
        public bool Ascending { get; }

        public SqlOrderByExpression(SqlExpression key, bool ascending) : base("", null) 
        {
            Key = key;
            Ascending = ascending;
        }
    }
}
