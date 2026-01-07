using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq
{
    public class SqlExpressionOld : Expression
    {
        public string Sql { get; }
        public Expression Expression { get; }
        

        public SqlExpression(string sql)
        {
            Sql = sql;
        }

        public SqlExpression(Expression e)
        {
            Expression = e;
        }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => typeof(string);
    }

}
