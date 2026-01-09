using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal class SqlJoinExpression : SqlSourceExpression
    {
        internal SqlExpression Left { get; set; }
        internal SqlExpression Right { get; set; }
        public SqlExpression On { get; private set; }
        public SqlJoinType JoinType { get; } = SqlJoinType.Cross;
       /* public SqlJoinExpression(string alias, Type type) : base(alias, type)
        {
        }*/

        public SqlJoinExpression(SqlExpression left, SqlExpression right, SqlExpression on, SqlJoinType joinType, string alias, Type elementType) : base(alias, elementType)
        {
            Left = left;
            Right = right;
            On = on;
            JoinType = joinType;
        }

      /*  public SqlJoinExpression(SqlExpression left, SqlExpression right, SqlJoinType joinType, string alias) : base(alias, right.Type)
        {
            Left = left;
            Right = right;
            JoinType = joinType;
        }*/

        public void SetOn(SqlExpression on)
        {
            On = on;
        }
    }

    public enum SqlJoinType 
    { 
        Inner, 
        LeftOuter, 
        RightOuter, 
        FullOuter,
        Cross
    }
}
