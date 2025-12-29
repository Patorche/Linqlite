using Linqlite.Attributes;
using Linqlite.Sqlite;


namespace Linqlite.Models
{
    [TableAttribute("PHOTO")]
    public class Photo : SqliteObservableEntity
    {
        private int _rate = 0;
        private GpsLocalisation _localisation = new();
        private int _thumbWidth;
        private int _thumbHeight;
        private bool _isMenuOpen = false;
        private bool _isLoadingImage = false;
        private string? _author = null;

        [ColumnAttribute(ColumnName = "id", IsKey = true)]
        public long? Id { get; set; }
        [ColumnAttribute(ColumnName = "filename", OnConflict = true)]
        public string Filename { get; set; } = string.Empty;
        [ColumnAttribute(ColumnName = "takendate")]
        public DateTime? TakenDate { get; set; } = DateTime.Now;
        [ColumnAttribute(ColumnName = "folder", OnConflict = true)]
        public string Folder { get; set; } = string.Empty;
        [ColumnAttribute(ColumnName = "width")]
        public int Width { get; set; } = 0;
        [ColumnAttribute(ColumnName = "height")]
        public int Height { get; set; } = 0;
        [ColumnAttribute(ColumnName = "type")]
        public string Type { get; set; } = string.Empty;// JPEG, PEF etc
        [ColumnAttribute(ColumnName = "author")]
        public string? Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }
        [ColumnAttribute(ColumnName = "camera")]
        public string? CameraName { get; set; }
        [ColumnAttribute(ColumnName = "make")]
        public string? Make { get; set; } = string.Empty;
        [ColumnAttribute(IsObjectProperty = true)]
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

        [ColumnAttribute(IsObjectProperty = true)]
        public CameraSetting CameraSetting { get; set; }

        public string? Focal { get; set; } = string.Empty; // 50mm, 18-55mm etc
        [ColumnAttribute(ColumnName = "rate")]
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

        [ColumnAttribute(ColumnName = "thumbwidth")]
        public int ThumbWidth
        {
            get => _thumbWidth;
            set => SetProperty(ref _thumbWidth, value);
        }

        [ColumnAttribute(ColumnName = "thumbheight")]
        public int ThumbHeight
        {
            get => _thumbHeight;
            set => SetProperty(ref _thumbHeight, value);
        }


        [ColumnAttribute(ColumnName = "orientation")]
        public int Orientation { get; set; }


        public Photo()
        {
        }
    }
}
