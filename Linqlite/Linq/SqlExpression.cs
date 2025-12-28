using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    class SqlExpression : Expression
    {
        public string Sql { get; }

        public SqlExpression(string sql)
        {
            Sql = sql;
        }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => typeof(string);
    }

}
