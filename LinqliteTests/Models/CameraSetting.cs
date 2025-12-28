using Linqlite.Attributes;
using Linqlite.Sqlite;

namespace Linqlite.Models
{
    public class CameraSetting : SqliteObservableEntity
    {
        [SqliteColumnAttribute(ColumnName = "iso")]
        public int? Iso { get; set; }
        [SqliteColumnAttribute(ColumnName = "aperture")]
        public double? Aperture { get; set; }     // f/2.8 etc.
        [SqliteColumnAttribute(ColumnName = "shutterspeed")]
        public double? ShutterSpeed { get; set; } // en secondes
        [SqliteColumnAttribute(ColumnName = "focal")]
        public double? Focal { get; set; }  // en mm

        public string FocalString => Focal.ToString();

        public override string ToString()
        {
            return $"ISO={Iso}, f/{Aperture}, 1/{1.0 / ShutterSpeed:F0}s, {Focal}mm";
        }
    }
}