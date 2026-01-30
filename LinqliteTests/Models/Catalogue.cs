using Linqlite.Attributes;
using Linqlite.Sqlite;
using System.Collections.ObjectModel;

namespace Linqlite.Models
{
    [TableAttribute("LIBRARY")]
    public class Catalogue : SqliteEntity
    {
        [ColumnAttribute("id")]
        [PrimaryKey(AutoIncrement = true)]
        public long Id { get; set; }
        private string _name = string.Empty;
        private ObservableCollection<string> _folders = [];
        private int _totalCount = 0;


        [ColumnAttribute("name")]
        [Unique(OnConflict=ConflictAction.Fail)]
        public string Name 
        {  get => _name;
            set => SetProperty(ref _name, value);
        }
        [ColumnAttribute("creation_date")]
        public DateTime CreationDate { get; set; } = DateTime.Now;
        
        public ObservableCollection<string> Folders
        {
            get => _folders;
            set => SetProperty(ref _folders, value);
        }
        

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        [OnexN(TargetKey = nameof(Collection.CatalogueId))]
        public List<Collection> Collections { get; set; }

        [NxN(AssociationType = typeof(PhotoCatalogue), LeftKey = nameof(PhotoCatalogue.CatalogueId), RightKey = nameof(PhotoCatalogue.PhotoId))]
        public List<Photo> Photos{ get; set; }

        public Catalogue() 
        {
            _name = string.Empty;
            CreationDate = DateTime.Now;
        }
     }
}
