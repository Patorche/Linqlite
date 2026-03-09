using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OnexOneAttribute : Attribute
    {
        public string TargetKey { get; set; }
    }
}
