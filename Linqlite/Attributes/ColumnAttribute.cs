using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqlite.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string ColumnName { get; set; }


        public ColumnAttribute(string column)
        {
            ColumnName = column;
        }

        public ColumnAttribute()
        {
            ColumnName = "";
        }

    }
}
