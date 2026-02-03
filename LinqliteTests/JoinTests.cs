
using Linqlite.Linq;
using Linqlite.Logger;
using Linqlite.Models;

namespace LinqliteTests 
{

    public class JoinTests : TestBase
    {
        static string connectionString = "E:\\Dev\\Photolab.db\\photolab.db";

        [Fact]
        public void JoinSimple()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();
            var photocatalogue = provider.Table<PhotoCatalogue>();

            var sql = SqlFor(photos.Join(photocatalogue, p => p.Id, pc => pc.PhotoId, (p, pc) => new { p, pc }).Where(q => q.pc.IsDeleted == false), provider);

            
     

            Assert.Equal(
                "SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation, t1.id AS t1_id, t1.photo_id AS t1_photo_id, t1.lib_id AS t1_lib_id, t1.deleted AS t1_deleted FROM PHOTO t0 JOIN PHOTO_LIB t1 ON (t0.id = t1.photo_id) WHERE (t1.deleted = FALSE)",
                sql);
        }

        [Fact]
        public void Join2() 
        {
            var provider = new LinqliteProvider(connectionString);
            provider.Logger = new SqlLogger();
            var photos = provider.Table<Photo>();
            var photoKeyWords = provider.Table<PhotoKeyWords>();
            var keyWords = provider.Table<KeyWord>();
            provider.EnsureTablesCreated();

            var kw = keyWords.Join(photoKeyWords, k => k.Id, pk => pk.KeyWordId, (w, pk) => new { w, pk }).Where(q => q.pk.PhotoId == 15000).Select(j => j.w).ToList();
        }


    }

    public class SqlLogger : ILinqliteLogger
    {
        public void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
