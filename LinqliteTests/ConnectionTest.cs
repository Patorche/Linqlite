using Linqlite.Linq;
using Linqlite.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace LinqliteTests
{
    public class ConnectionTest : TestBase
    {
        static string connectionString = "E:\\Dev\\Photolab.db\\photolab.db";
        [Fact]
        public void ConnectionTest1()
        {
            var provider = new QueryProvider(connectionString);
            var photos = new QueryableTable<Photo>(provider);

            var query = photos.Where(p => p.Id >0).ToList();

            foreach(var e  in query)
            {
                Console.WriteLine(e);
            }

        }

        [Fact]
        public async Task TestAvecNot()
        {
            var provider = new QueryProvider(connectionString);
            var photos = new QueryableTable<Photo>(provider);
            var photoCatalogue = new QueryableTable<PhotoCatalogue>(provider);

            Catalogue catalogue = new Catalogue() { Id = 2, Name = "Test" };

            //var result = photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == catalogue.Id && !x.c.IsDeleted).OrderBy(x => x.p.TakenDate).Select(x => x.p).ToList();
            long cid = catalogue.Id;
            var query = photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == cid && !x.c.IsDeleted).OrderBy(x => x.p.TakenDate).Select(x => x.p);
            string res = SqlFor(query);
                                
            Assert.Equal("SELECT t0.id, t0.filename, t0.takendate, t0.folder, t0.width, t0.height, t0.type, t0.author, t0.camera, t0.make, t0.latitude, t0.longitude, t0.city, t0.country, t0.iso, t0.aperture, t0.shutterspeed, t0.focal, t0.rate, t0.thumbwidth, t0.thumbheight, t0.orientation FROM PHOTO t0 JOIN PHOTO_LIB t1 ON (t0.id = t1.photo_id) WHERE ((t1.lib_id = @v0) AND (NOT t1.deleted)) ORDER BY t0.takendate ASC",
                res);
            await foreach(var p in query.ToEnumerableAsync())
            {
                //Console.WriteLine(p);
                System.Diagnostics.Debug.WriteLine("----------------");
                System.Diagnostics.Debug.WriteLine(p.Filename);
                System.Diagnostics.Debug.WriteLine(p.Folder);
                System.Diagnostics.Debug.WriteLine(p.Author);
            }

            int i = 0;
        }


        [Fact]
        public void TestAvecValeurBool()
        {
            var provider = new QueryProvider(connectionString);
            var photos = new QueryableTable<Photo>(provider);
            var photoCatalogue = new QueryableTable<PhotoCatalogue>(provider);

            Catalogue catalogue = new Catalogue() { Id = 2, Name = "Test" };

            //var result = photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == catalogue.Id && !x.c.IsDeleted).OrderBy(x => x.p.TakenDate).Select(x => x.p).ToList();
            
            var query = photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == catalogue.Id && x.c.IsDeleted == true).OrderBy(x => x.p.TakenDate).Select(x => x.p);
            string res = SqlFor(query);

            Assert.Equal("SELECT t0.id, t0.filename, t0.takendate, t0.folder, t0.width, t0.height, t0.type, t0.author, t0.camera, t0.make, t0.latitude, t0.longitude, t0.city, t0.country, t0.iso, t0.aperture, t0.shutterspeed, t0.focal, t0.rate, t0.thumbwidth, t0.thumbheight, t0.orientation FROM PHOTO t0 JOIN PHOTO_LIB t1 ON (t0.id = t1.photo_id) WHERE ((t1.lib_id = @v0) AND (t1.deleted = TRUE)) ORDER BY t0.takendate ASC",
                res);
            List<Photo> list = query.ToList();
            int i = 0;
        }


        [Fact]
        public void TestDelete()
        {
            var provider = new QueryProvider(connectionString);
            var photos = new QueryableTable<Photo>(provider);

            Photo photo = new Photo()
            {
                Id = 16091,
                Author = "Patorche",
                CameraName = "K100",
                Make = "Pentax",
                Filename = "IMG001.DNG",
                Focal = "100",
                Folder = @"c:\Images",
                Height = 100,
                Width = 100,
                IsNew = true,
                Orientation = 3,
                Rate = 2,
                TakenDate = new DateTime(2025, 12, 31),
                ThumbHeight = 100,
                ThumbWidth = 100,
                Localisation = new GpsLocalisation() { City = "Paris", Country = "France", Latitude = 44.0, Longitude = 44.0},
                CameraSetting = new CameraSetting() { Aperture = 1, Focal = 100, Iso = 1500, ShutterSpeed = 0.001},
                Type = "PNG"
            };

            photos.Insert(photo);
            photos.Delete(photo);
            //var result = photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == catalogue.Id && !x.c.IsDeleted).OrderBy(x => x.p.TakenDate).Select(x => x.p).ToList();
            //string res = SqlFor(photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == 1 && !x.c.IsDeleted).OrderBy(x => x.p.TakenDate).Select(x => x.p));
            //Assert.Equal("SELECT t0.id, t0.filename, t0.takendate, t0.folder, t0.width, t0.height, t0.type, t0.author, t0.camera, t0.make, t0.latitude, t0.longitude, t0.city, t0.country, t0.iso, t0.aperture, t0.shutterspeed, t0.focal, t0.rate, t0.thumbwidth, t0.thumbheight, t0.orientation FROM PHOTO t0 JOIN PHOTO_LIB t1 ON t0.id = t1.photo_id WHERE ((t1.lib_id = 1) AND t1.deleted) ORDER BY t0.takendate",
                //res);
            int i = 0;
        }

        [Fact]
        public void TestInsert()
        {
            var provider = new QueryProvider(connectionString);
            var photos = new QueryableTable<Photo>(provider);

            Photo photo = new Photo()
            {
                Author = "Patorche",
                CameraName = "K100",
                Make = "Pentax",
                Filename = "IMG001.DNG",
                Focal = "100",
                Folder = @"c:\Images",
                Height = 100,
                Width = 100,
                IsNew = true,
                Orientation = 3,
                Rate = 2,
                TakenDate = new DateTime(2025, 12, 31),
                ThumbHeight = 100,
                ThumbWidth = 100,
                Localisation = new GpsLocalisation() { City = "Paris", Country = "France", Latitude = 44.0, Longitude = 44.0 },
                CameraSetting = new CameraSetting() { Aperture = 1, Focal = 100, Iso = 1500, ShutterSpeed = 0.001 },
                Type = "PNG"
            };

            long res = photos.Insert(photo);

            int i = 0;
        }

        [Fact]
        public void TestUpdate()
        {
            var provider = new QueryProvider(connectionString);
            var photos = new QueryableTable<Photo>(provider);

            var query = photos.Where(p => p.Filename == "IMG-20200819-WA0004.jpg");
            var sql = SqlFor(query);
            var ps = query.ToList();

            ps[0].Rate = 3;
            //photos.Update(photo, nameof(Photo.Localisation));

            int i = 0;
        }

    }

    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> ToEnumerableAsync<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
                await Task.Yield(); // libère le contexte, évite le blocage
            }
        }
    }
}
