using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlSelectManyExpression : SqlExpression
    {
        public SqlExpression Source { get; }
        public LambdaExpression CollectionSelector { get; }
        public LambdaExpression ResultSelector { get; }

        public SqlSelectManyExpression(
            SqlExpression source,
            LambdaExpression collectionSelector,
            LambdaExpression resultSelector)
            : base(typeof(IEnumerable<>))
        {
            Source = source;
            CollectionSelector = collectionSelector;
            ResultSelector = resultSelector;
        }
    }

}
