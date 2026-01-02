using Linqlite.Attributes;
using Linqlite.Sqlite;

namespace Linqlite.Models
{
    public class CameraSetting : SqliteEntity
    {
        [ColumnAttribute(ColumnName = "iso")]
        public int? Iso { get; set; }
        [ColumnAttribute(ColumnName = "aperture")]
        public double? Aperture { get; set; }     // f/2.8 etc.
        [ColumnAttribute(ColumnName = "shutterspeed")]
        public double? ShutterSpeed { get; set; } // en secondes
        [ColumnAttribute(ColumnName = "focal")]
        public double? Focal { get; set; }  // en mm

        public string FocalString => Focal.ToString();

        public override string ToString()
        {
            return $"ISO={Iso}, f/{Aperture}, 1/{1.0 / ShutterSpeed:F0}s, {Focal}mm";
        }
    }
}