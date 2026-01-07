using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class AbstractSqlProjectionExpression : SqlExpression
    {

        protected AbstractSqlProjectionExpression(Type type) : base(type)
        {
        }
    }
}
