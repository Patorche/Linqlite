using Linqlite.Attributes;
using Linqlite.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Models
{
    [Table("photos_keywords")]
    public class PhotoKeyWords : SqliteEntity
    {
        [Column("id")]
        [PrimaryKey(AutoIncrement = true)]
        public long? Id { get; set; }


        [Column("photo_id")]
        [ForeignKey(typeof(Photo), nameof(Photo.Id), true)]
        public long PhotoId {  get; set; }

        [Column("keyword_id")]
        [ForeignKey(typeof(KeyWord), nameof(KeyWord.Id), true)]
        public long KeyWordId { get; set; }

        public PhotoKeyWords() { }

    }
}
