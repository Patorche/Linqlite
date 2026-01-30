using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Linqlite.Linq.SqlVisitor
{
    public class ProjectedProperty
    {
        public string Name { get; set; }
        public Expression Expression { get; set; }
        public Type Type { get; set; }
    }

}
