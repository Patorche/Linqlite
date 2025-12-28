using Linqlite.Attributes;
using Linqlite.Sqlite;


namespace Linqlite.Models
{
    [SqliteTableAttribute("PHOTO")]
    public class Photo : SqliteObservableEntity
    {
        private int _rate = 0;
        private GpsLocalisation _localisation = new();
        private int _thumbWidth;
        private int _thumbHeight;
        private bool _isMenuOpen = false;
        private bool _isLoadingImage = false;
        private string? _author = null;

        [SqliteColumnAttribute(ColumnName = "id", IsKey = true)]
        public long? Id { get; set; }
        [SqliteColumnAttribute(ColumnName = "filename", OnConflict = true)]
        public string Filename { get; set; } = string.Empty;
        [SqliteColumnAttribute(ColumnName = "takendate")]
        public DateTime? TakenDate { get; set; } = DateTime.Now;
        [SqliteColumnAttribute(ColumnName = "folder", OnConflict = true)]
        public string Folder { get; set; } = string.Empty;
        [SqliteColumnAttribute(ColumnName = "width")]
        public int Width { get; set; } = 0;
        [SqliteColumnAttribute(ColumnName = "height")]
        public int Height { get; set; } = 0;
        [SqliteColumnAttribute(ColumnName = "type")]
        public string Type { get; set; } = string.Empty;// JPEG, PEF etc
        [SqliteColumnAttribute(ColumnName = "author")]
        public string? Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }
        [SqliteColumnAttribute(ColumnName = "camera")]
        public string? CameraName { get; set; }
        [SqliteColumnAttribute(ColumnName = "make")]
        public string? Make { get; set; } = string.Empty;
        [SqliteColumnAttribute(IsObjectProperty = true)]
        public GpsLocalisation? Localisation
        {
            get => _localisation;
            set
            {
                if (SetProperty(ref _localisation, value))
                {
                }

            }
        }

        [SqliteColumnAttribute(IsObjectProperty = true)]
        public CameraSetting CameraSetting { get; set; }

        public string? Focal { get; set; } = string.Empty; // 50mm, 18-55mm etc
        [SqliteColumnAttribute(ColumnName = "rate")]
        public int Rate
        {
            get => _rate;
            set
            {
                int v = _rate;
                if (value < 0) value = 0;
                if (value > 5) value = 5;
                SetProperty(ref _rate, value);
            }
        }      

        [SqliteColumnAttribute(ColumnName = "thumbwidth")]
        public int ThumbWidth
        {
            get => _thumbWidth;
            set => SetProperty(ref _thumbWidth, value);
        }

        [SqliteColumnAttribute(ColumnName = "thumbheight")]
        public int ThumbHeight
        {
            get => _thumbHeight;
            set => SetProperty(ref _thumbHeight, value);
        }


        [SqliteColumnAttribute(ColumnName = "orientation")]
        public int Orientation { get; set; }


        public Photo()
        {
        }
    }
}
