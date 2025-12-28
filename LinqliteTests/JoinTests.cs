
using Linqlite.Linq;
using Linqlite.Models;

namespace Linqlite.Tests 
{

    public class JoinTests : TestBase
    {
        [Fact]
        public void JoinSimple1()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);
            var gps = new QueryableTable<GpsLocalisation>(provider);

            var sql = SqlFor(photos.Join(gps, p => p.Localisation.Latitude, g => g.Latitude, (p, g) => new { p, g }).Where(q => q.p.Id == 12));

            Assert.Equal(
                "SELECT * FROM photo t0 JOIN gpslocalisation t1 ON t0.latitude = t1.latitude WHERE (t0.Id = 12)",
                sql);
        }

        [Fact]
        public void JoinSimple()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);
            var photocatalogue = new QueryableTable<PhotoCatalogue>(provider);

            var sql = SqlFor(photos.Join(photocatalogue, p => p.Id, pc => pc.PhotoId, (p, pc) => new { p, pc }).Where(q => q.pc.IsDeleted == false));
     

            Assert.Equal(
                "SELECT * FROM photo t0 JOIN gpslocalisation t1 ON t0.latitude = t1.latitude WHERE (t0.Id = 12)",
                sql);
        }
    }
}
