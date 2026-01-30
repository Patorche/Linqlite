using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public  class NxNAttribute : Attribute
    {
        public Type AssociationType { get; set; }
        public string LeftKey { get; set; }
        public string RightKey { get; set; }

    }
}
