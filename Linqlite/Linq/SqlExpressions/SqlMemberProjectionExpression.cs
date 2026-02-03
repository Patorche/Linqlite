using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Linq.SqlExpressions
{
    internal sealed class SqlMemberProjectionExpression : AbstractSqlProjectionExpression
    {
        //public IReadOnlyDictionary<MemberInfo, SqlExpression> Columns { get; }
        
        public IReadOnlyDictionary<string, (MemberInfo?, SqlExpression)> Columns { get; }

        //public SqlMemberProjectionExpression(Dictionary<MemberInfo, SqlExpression> columns, Type type) : base(type) // ou un type plus précis plus tard { Columns = columns.ToList();
        public SqlMemberProjectionExpression(Dictionary<string, (MemberInfo?,SqlExpression)> columns, Type type) : base(type) // ou un type plus précis plus tard { Columns = columns.ToList();
        {
            Columns = columns;
        }

        public SqlMemberProjectionExpression() : base(typeof(object)) { }
    }
}
