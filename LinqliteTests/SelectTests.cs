using Linqlite.Linq;
using Linqlite.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqliteTests
{
    public class SelectTests : TestBase
    {
        static string connectionString = "E:\\Dev\\Photolab.db\\photolab.db";

        [Fact]
        public void SelectSimple() 
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>( TrackingMode.Manual);

            var sql = SqlFor(photos, provider);

            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0", sql);
        }

        [Fact]
        public void SelectSimple2()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => true), provider);

            //Assert.Equal("SELECT t0.* FROM PHOTO t0 WHERE TRUE", sql);
        }

        [Fact]
        public void SelectFullProjection()
        {
            var provider = new LinqliteProvider(connectionString);
            var photos = provider.Table<Photo>(TrackingMode.Manual);

            var sql = photos.ToList();

            //Assert.Equal("SELECT t0.* FROM PHOTO t0", sql);
        }

        [Fact]
        public void ProjectionAnonymous()
        {
            var provider = new LinqliteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var l = photos.Select(p => new { p.Id, p.Filename }).ToList();
            int i = 0;
        }

        [Fact]
        public void ProjectionSimple()
        {
            var provider = new LinqliteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var l = photos.Select(p => p.Id).ToList();
            int i = 0;
        }

        [Fact]
        public void ProjectionTuple()
        {
            var provider = new LinqliteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var l = photos.Select(p => ValueTuple.Create(p.Id, p.Filename)).ToList();
            int i = 0;
        }

        [Fact]
        public void ProjectionDto()
        {
            var provider = new LinqliteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var l = photos.Select(p => new MyDto() { Id = p.Id, Name = p.Filename }).ToList();
            int i = 0;
        }

        [Fact]
        public void ProjectionJoin()
        {
            var provider = new LinqliteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var photocatalogue = provider.Table<PhotoCatalogue>();
            var query = photocatalogue.Join(photos, c => c.PhotoId, p => p.Id, (c, p) => new { c, p }).Select(j => new { j.c.CatalogueId, j.p.Filename }).ToList();
        }

        [Fact]
        public void SelectSimple3()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();
           
            var sql = SqlFor(photos.Where(p => p.Id == 15), provider);

            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0 WHERE (t0.id = 15)", sql);
        }

        [Fact]
        public void SelectSimple4()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Localisation.Latitude >= 35 && p.Width > 0), provider);

            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0 WHERE ((t0.latitude >= 35) AND (t0.width > 0))", sql);
        }

        [Fact]
        public void SelectSimple5()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Select(p => new { p.Id, p.Filename }), provider);

            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename FROM PHOTO t0", sql);
        }


        [Fact]
        public void SelectSimple6()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Author.Contains("pat")).Select(p => new { p.Id, p.Filename }), provider);

            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename FROM PHOTO t0 WHERE (t0.author LIKE '%pat%')", sql);
        }

        [Fact]
        public void ContainsList()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();
            List<long?> ids = new() {15000,900,800,1502 };

            var sql = SqlFor(photos.Where(p => ids.Contains(p.Id)).Select(p => new { p.Id, p.Filename }), provider);

            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename FROM PHOTO t0 WHERE (t0.id IN (15000, 900, 800, 1502))", sql);
        }

        [Fact]
        public void JoinWhereSelect() 
        {
            var provider = new LinqliteProvider(@"E:\Dev\Photolab.db\devBase.db");
            provider.Logger = new SqlLogger();
            var photos = provider.Table<Photo>();
            var photoKeyWords = provider.Table<PhotoKeyWords>();
            var keyWords = provider.Table<KeyWord>();
            Photo photo = new Photo() { Id = 35 };
            var ws = keyWords.Join(photoKeyWords, k => k.Id, p => p.KeyWordId, (k, p) => new { k, p }).Where(j => j.p.PhotoId == photo.Id).Select(j => j.k.Word).ToList();
            int i = 0;

        }

        [Fact]
        public void SingleOrDefault()
        {
            var provider = new LinqliteProvider(@"E:\Dev\Photolab.db\devBase.db");
            ITable<KeyWord> keyWords = provider.Table<KeyWord>();
            var res = keyWords.SingleOrDefault(w => w.Word == "test");
            System.Console.WriteLine(res);
        }

        [Fact]
        public void EmptyList()
        {
            var provider = new LinqliteProvider(@"E:\Dev\Photolab.db\devBase.db");
            var keyWords = provider.Table<KeyWord>();
            var res = keyWords.Where(w => w.Word == "doesn't exists").ToList();
            System.Console.WriteLine(res);
        }
    }

    public class MyDto
    {
        public long? Id {  get; set; }
        public string Name { get; set; } = "";
    }
}
