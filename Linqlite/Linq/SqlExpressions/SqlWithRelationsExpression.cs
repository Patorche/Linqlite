using Linqlite.Linq.Relations;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public class SqlWithRelationsExpression : Expression
    {
        public Expression Source { get; }
        public LinqliteProvider Provider { get; }

        public SqlWithRelationsExpression(Expression source, IQueryProvider provider)
        {
            Source = source;
            Provider = (LinqliteProvider)provider;
        }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => Source.Type;

        public override bool CanReduce => false;

        public override Expression Reduce() => this;
        
    }



}
