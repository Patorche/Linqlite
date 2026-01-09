using Linqlite.Attributes;
using Linqlite.Models;
using Linqlite.Sqlite;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;


namespace Linqlite.Models
{
    [Table("PHOTO")]
    public class Photo : SqliteEntity
    {
        public static List<string> AcceptedExtentions = new() { ".jpg", ".jpeg", ".png", ".pef", ".bmp", ".tiff", ".dng" };
        private int _rate = 0;
        private bool _isSelected = false;
        private byte[]? _thumb = null;
        private GpsLocalisation _localisation = new();
        private int _thumbWidth;
        private int _thumbHeight;
        private bool _isMenuOpen = false;
        private bool _isLoadingImage = false;
        private string? _author = null;

        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }
        public bool IsLoadingImage
        {
            get => _isLoadingImage;
            set => SetProperty(ref _isLoadingImage, value);
        }

        [Column("id")]
        [PrimaryKey(AutoIncrement = true)]
        public long? Id { get; set; } = -1;
       
        [Column("filename")]
        [UniqueGroup("fullpath",OnConflict = ConflictAction.Ignore)]
        public string? Filename { get; set; } = string.Empty;
      
        [Column("takendate")]
        [NotNull()]
        public DateTime? TakenDate { get; set; } = DateTime.Now;
      
        [Column("folder")]
        [UniqueGroup("fullpath", OnConflict = ConflictAction.Ignore, IsUpsertKey = true)]
        public string? Folder { get; set; } = string.Empty;
      
        [Column("width")]
        public int? Width { get; set; } = 0;
      
        [Column("height")]
        public int? Height { get; set; } = 0;
        [Column("type")]
        public string? Type { get; set; } = string.Empty;// JPEG, PEF etc
        [Column("author")]
        public string? Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }
        [Column("camera")]
        public string? CameraName { get; set; }
        [Column("make")]
        public string? Make { get; set; } = string.Empty;
        [Column()]
        public GpsLocalisation? Localisation
        {
            get => _localisation;
            set
            {
                if (SetProperty(ref _localisation, value))
                {
                    if (_localisation != null)
                        _localisation.Parent = this;
                }

            }
        }

        [Column()] 
        public CameraSetting CameraSetting { get; set; }

        public string? Focal { get; set; } = string.Empty; // 50mm, 18-55mm etc
        [Column(ColumnName = "rate")]
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
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public byte[]? Thumb
        {
            get => _thumb;
            set
            {
                SetProperty(ref _thumb, value);
            }

        }

        [Column(ColumnName = "thumbwidth")]
        public int ThumbWidth
        {
            get => _thumbWidth;
            set => SetProperty(ref _thumbWidth, value);
        }

        [Column(ColumnName = "thumbheight")]
        public int ThumbHeight
        {
            get => _thumbHeight;
            set => SetProperty(ref _thumbHeight, value);
        }


        public bool IsImageLoaded { get; set; }

        [Column(ColumnName = "orientation")]
        public int Orientation { get; set; }


        // Cache interne pour le BitmapImage
        /// <summary>
        /// Retourne le BitmapImage gelé pour WPF. Construit et met en cache à la première demande.
        /// </summary>

        public Photo()
        {
        }

    }
}
