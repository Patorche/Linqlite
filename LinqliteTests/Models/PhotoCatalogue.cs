using Linqlite.Attributes;
using Linqlite.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqlite.Models
{ 
    [TableAttribute("PHOTO_LIB", isJoin:true)]
    public class PhotoCatalogue : SqliteObservableEntity
    {
        private bool _isDeleted = false;

        [ColumnAttribute(ColumnName = "photo_id", JoinedTableName = "PHOTO")]
        public long? PhotoId { get; set; }
        [ColumnAttribute(ColumnName = "lib_id", JoinedTableName = "LIBRARY")]
        public string CatalogueId { get; set; }
        [ColumnAttribute(ColumnName = "deleted")]
        public bool IsDeleted 
        { 
            get => _isDeleted;
            set => SetProperty(ref _isDeleted, value);
        }

        public PhotoCatalogue() { }
    }
}
