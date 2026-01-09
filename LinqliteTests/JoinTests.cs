
using Linqlite.Linq;
using Linqlite.Models;

namespace LinqliteTests 
{

    public class JoinTests : TestBase
    {
        [Fact]
        public void JoinSimple()
        {
            var provider = new LinqLiteProvider();
            var photos = provider.Table<Photo>();
            var photocatalogue = provider.Table<PhotoCatalogue>();

            var sql = SqlFor(photos.Join(photocatalogue, p => p.Id, pc => pc.PhotoId, (p, pc) => new { p, pc }).Where(q => q.pc.IsDeleted == false));

            
     

            Assert.Equal(
                "SELECT t0.* FROM PHOTO t0 JOIN PHOTO_LIB t1 ON (t0.id = t1.photo_id) WHERE (t1.deleted = FALSE)",
                sql);
        }


    }
}
