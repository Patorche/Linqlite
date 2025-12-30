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
        private string _columnName = "";
        private bool _isKey = false;
        private bool _isObjectProperty = false;
        private string _joinedTableName = "";
        private bool _onConflict = false;

        public string ColumnName
        { 
            get => _columnName; 
            set => _columnName = value;
        }

        public bool IsKey
        {
            get => _isKey;
            set => _isKey = value;
        }

        public bool IsObjectProperty
        {
            get => _isObjectProperty;
            set => _isObjectProperty = value;
        }

        public string JoinedTableName
        {
            get => _joinedTableName;
            set => _joinedTableName = value;
        }

        public bool OnConflict
        {
            get => _onConflict;
            set => _onConflict = value;
        }

        public ColumnAttribute()
        {
        }

    }
}
