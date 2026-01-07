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
        public bool IsNew { get; set; }

        public Catalogue() 
        {
            _name = string.Empty;
            CreationDate = DateTime.Now;
        }
     }
}
