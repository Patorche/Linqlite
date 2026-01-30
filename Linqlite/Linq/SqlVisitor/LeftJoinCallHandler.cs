using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    internal class LeftJoinCallHandler : JoinCallHandler, IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder)
        {
            return base.Handle(node, builder, SqlJoinType.LeftOuter);
        }
    }
}
