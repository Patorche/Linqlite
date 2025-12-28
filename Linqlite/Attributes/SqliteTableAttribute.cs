using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqlite.Attributes
{

    [AttributeUsage(AttributeTargets.Class)]
    public class SqliteTableAttribute : Attribute
    {
        private string _tableName = "";
        private bool _isJoin = false;
        

        public string TableName
        {
            get => _tableName;
        }

        public bool IsJoin { get => _isJoin; }



        public SqliteTableAttribute(string tableName) 
        { 
            _tableName = tableName;
        }

        public SqliteTableAttribute(string tableName, bool isJoin)
        {
            _tableName = tableName;
            _isJoin = isJoin;
        }

    }
}
