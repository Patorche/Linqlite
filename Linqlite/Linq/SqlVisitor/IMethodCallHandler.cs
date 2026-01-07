using Linqlite.Linq.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    public interface IMethodCallHandler
    {
        public SqlExpression Handle(MethodCallExpression node, SqlTreeBuilderVisitor builder);
    }

}
