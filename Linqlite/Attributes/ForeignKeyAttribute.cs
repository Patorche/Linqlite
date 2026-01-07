using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        public Type Entity { get; }
        public string Key { get; }
        public bool CascadeDelete { get; set; }
        public ForeignKeyAttribute(Type principalEntity, string principalKey, bool cascadeDelete = false) 
        { 
            Entity = principalEntity; 
            Key = principalKey; 
            CascadeDelete = cascadeDelete;
        }
    }
}
