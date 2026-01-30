using System;
using System.Collections.Generic;
using System.Text;
using ZSpitz.Util;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlSelectExpression : SqlExpression
    {
        private readonly List<SqlOrderByExpression> _orders = [];

        internal required SqlSourceExpression From { get; set; }
        internal SqlExpression? Where { get; set; }
        internal AbstractSqlProjectionExpression? Projection { get; private set; }
        internal IReadOnlyList<SqlOrderByExpression> Orders => _orders;
        public Type ElementType { get; set; }

        public int? Offset { get; set; }
        public int? Limit { get; set; }

        public SqlSelectExpression(Type type) : base(type)
        {
            ElementType = type;
        }      

        internal void AddOrder(SqlExpression predicate, bool ascending = true)
        {
            _orders.Add(new SqlOrderByExpression(predicate,ascending));
        }

        public void AddWhere(SqlExpression predicate)
        {
            if (Where == null)
                Where = predicate;
            else Where = new SqlBinaryExpression(Where, "AND", predicate);
        }

        internal void SetProjection(AbstractSqlProjectionExpression? projection)
        {
            Projection = projection;
        }
    }
}
