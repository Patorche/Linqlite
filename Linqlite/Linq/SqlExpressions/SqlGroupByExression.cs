using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlGroupByExpression : SqlExpression
    {
        public Expression Source { get; }
        public LambdaExpression KeySelector { get; }
        public LambdaExpression OriginalKeySelector { get; }
        public LambdaExpression ElementSelector { get; }
        public bool OriginalTypeIsEntity { get; }
        public Type ElementType { get; }
        public SqlGroupByExpression(
            Expression source,
            LambdaExpression keySelector,
            LambdaExpression elementSelector,
            bool originalTypeIsEntity,
            LambdaExpression originalKeySelector,
            Type returnType)   
            : base(typeof(IGrouping<,>)) // on affinera le type après
        {
            Source = source;
            KeySelector = keySelector;
            ElementSelector = elementSelector;
            OriginalTypeIsEntity = originalTypeIsEntity;
            OriginalKeySelector = originalKeySelector;
            ElementType = returnType;
        }
    }

}
