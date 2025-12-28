using Linqlite.Attributes;
using Linqlite.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqlite.Models
{ 
    [SqliteTableAttribute("PHOTO_LIB", isJoin:true)]
    public class PhotoCatalogue : SqliteObservableEntity
    {
        private bool _isDeleted = false;

        [SqliteColumnAttribute(ColumnName = "photo_id", JoinedTableName = "PHOTO")]
        public long? PhotoId { get; set; }
        [SqliteColumnAttribute(ColumnName = "lib_id", JoinedTableName = "LIBRARY")]
        public string CatalogueId { get; set; }
        [SqliteColumnAttribute(ColumnName = "deleted")]
        public bool IsDeleted 
        { 
            get => _isDeleted;
            set => SetProperty(ref _isDeleted, value);
        }

        public PhotoCatalogue() { }
    }
}
