using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;

namespace Linqlite.Linq.Relations
{
    public class NxNRelation : IRelation
    {
        public Type AssociationType { get; set; }
        public string AssociationLeftKey { get; set; }
        public string AssociationRightKey { get; set; }
        public PropertyInfo Property { get; set; }

        public Type LeftType { get; }

        public Type RightType { get; }

        public Type TargetType => RightType;

        public NxNRelation(Type left, Type right, Type association, string leftKey, string rightKey, PropertyInfo property)
        {
            LeftType = left;
            RightType = right;
            AssociationType = association;
            AssociationLeftKey = leftKey;
            AssociationRightKey = rightKey;
            Property = property;
        }


        public void ApplyJoins(BuildContext ctx)
        {
            var lk = EntityMap.Get(LeftType).GetPrimaryKey().PropertyInfo.Name;
            var rk = EntityMap.Get(RightType).GetPrimaryKey().PropertyInfo.Name;

            ctx.AddJoin(LeftType, AssociationType, lk, AssociationLeftKey); 
            ctx.AddJoin(AssociationType, RightType, AssociationRightKey, rk);
        }
    }
}
