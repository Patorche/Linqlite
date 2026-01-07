using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlEntityReferenceExpression : SqlExpression
    {
        public string Alias { get; set; }
        public SqlEntityReferenceExpression(string alias, Type type) : base(type)
        {
            Alias = alias;
        }
    }
}
