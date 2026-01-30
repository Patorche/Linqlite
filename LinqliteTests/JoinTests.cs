
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
                "SELECT t0.* FROM PHOTO t0 JOIN PHOTO_LIB t1 ON (t0.id = t1.photo_id) WHERE (t1.deleted = FALSE)",
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
