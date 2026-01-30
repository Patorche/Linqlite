using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq.Relations
{
    public interface IRelation
    {
        Type LeftType { get; }
        Type TargetType { get; }
        void ApplyJoins(BuildContext ctx);
    }

}
