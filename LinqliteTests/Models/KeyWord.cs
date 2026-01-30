using Linqlite.Attributes;
using Linqlite.Sqlite;
using System.ComponentModel.DataAnnotations.Schema;
using ColumnAttribute = Linqlite.Attributes.ColumnAttribute;
using TableAttribute = Linqlite.Attributes.TableAttribute;

namespace Linqlite.Models
{
    [Table("keywords")]
    public class KeyWord : SqliteEntity
    {
        [Column("id")]
        [PrimaryKey(AutoIncrement = true)]
        public long? Id { get; set; }

        [Column("word")]
        public string Word { get; set; } = "";
    }
}
