    using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    public abstract class SqlSourceExpression : SqlExpression
    {
        public string Alias { get; }
        protected SqlSourceExpression(string alias, Type elementType) : base(elementType) 
        { 
            Alias = alias; 
        }
        /*    public string Alias { get; }
            public string Table { get; }
            internal SqlProjectionExpression? Projection { get; private set; }

            public Type EntityType { get; set; }

            protected SqlSourceExpression(string alias, Type type)
                : base(typeof(void))
            {
                EntityType = type;
                Alias = alias;
            }

            internal void SetProjection(SqlProjectionExpression? projection)
            {
                Projection = projection;
            }

            */
    }

}
