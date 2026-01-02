using Linqlite.Attributes;
using Linqlite.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linqlite.Models
{ 
    [Table("PHOTO_LIB", isJoin:true)]
    public class PhotoCatalogue : SqliteEntity
    {
        private bool _isDeleted = false;

        [Column(ColumnName = "photo_id", JoinedTableName = "PHOTO")]
        public long? PhotoId { get; set; }
        [Column(ColumnName = "lib_id", JoinedTableName = "LIBRARY")]
        public long CatalogueId { get; set; }
        [Column(ColumnName = "deleted")]
        public bool IsDeleted 
        { 
            get => _isDeleted;
            set => SetProperty(ref _isDeleted, value);
        }

        public PhotoCatalogue() { }
    }
}
