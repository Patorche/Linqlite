using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Linqlite.Linq.Relations
{
    internal class OnexNRelation : IRelation
    {
        public Type LeftType { get; }

        public Type TargetType { get; }

        public string TargetKey;

        public PropertyInfo Property { get; set; }

        public void ApplyJoins(BuildContext ctx)
        {
            var lk = EntityMap.Get(LeftType).GetPrimaryKey().PropertyInfo.Name;

            ctx.AddJoin(LeftType, TargetType, lk, TargetKey);
        }

        public OnexNRelation(Type leftType, Type targetType, string targetKey, PropertyInfo property)
        {
            LeftType = leftType;
            TargetType = targetType;
            TargetKey = targetKey;
            Property = property;
        }
    }
}
