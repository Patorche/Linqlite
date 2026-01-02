
using Linqlite.Linq;
using Linqlite.Models;

namespace Linqlite.Tests 
{

    public class JoinTests : TestBase
    {
        [Fact]
        public void JoinSimple()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);
            var photocatalogue = new QueryableTable<PhotoCatalogue>(provider);

            var sql = SqlFor(photos.Join(photocatalogue, p => p.Id, pc => pc.PhotoId, (p, pc) => new { p, pc }).Where(q => q.pc.IsDeleted == false));

            
     

            Assert.Equal(
                "SELECT * FROM PHOTO t0 JOIN PHOTO_LIB t1 ON t0.id = t1.photo_id WHERE (t1.deleted = FALSE)",
                sql);
        }


    }
}
