using Linqlite.Attributes;
using Linqlite.Sqlite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TableAttribute = Linqlite.Attributes.TableAttribute;

namespace Linqlite.Models
{ 
    [Table("PHOTO_LIB")]
    public class PhotoCatalogue : SqliteEntity
    {
        private bool _isDeleted = false;
       
        [Column("id")]
        [PrimaryKey(AutoIncrement = true)]
        public long? Id { get; set; }
       
        [Column("photo_id")]
        [ForeignKey(typeof(Photo), nameof(Photo.Id),true)]
        
        public long? PhotoId { get; set; }
        [Column("lib_id")]
        [ForeignKey(typeof(Catalogue), nameof(Catalogue.Id), true)]
        
        public long CatalogueId { get; set; }
        [Column("deleted")]
       
        public bool IsDeleted 
        { 
            get => _isDeleted;
            set => SetProperty(ref _isDeleted, value);
        }

        public PhotoCatalogue() { }
    }
}
