using Linqlite.Attributes;
using Linqlite.Sqlite;
using System;

namespace Linqlite.Models
{
    [Table("COLLECTION")]
    public class Collection : SqliteEntity
    {
        private string _name = string.Empty;
        private string _dslFilter = string.Empty;
        private bool _isSelected = false;

        [Column("id")]
        [PrimaryKey(AutoIncrement = true)]
        public long Id { get; set; } = 0;

        [Column("library_id")]
        public long CatalogueId { get; set; } = 0;

        [Column("name")]
        [Unique(OnConflict = ConflictAction.Fail)]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [Column("creation_date")]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        [Column("dsl_filter")]
        public string DslFilter
        {
            get => _dslFilter;
            set => SetProperty(ref _dslFilter, value);
        }
     

        //public IPhotosFilter Filter { get => new CompositePhotoFilter(DslFilter); }
        public Collection()
        {
            _name = string.Empty;
            CreationDate = DateTime.Now;
        }
    }
}
