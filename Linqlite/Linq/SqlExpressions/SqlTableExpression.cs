using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public sealed class SqlTableExpression : SqlSourceExpression
    {
        public string TableName { get; }

        public SqlTableExpression(string tableName, string alias, Type entityType)
            : base(alias, entityType)
        {
            TableName = tableName;
        }
    }

}
