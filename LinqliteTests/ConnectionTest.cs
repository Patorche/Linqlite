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
            var provider = new LinqLiteProvider(connectionString);
            var photos = provider.Table<Photo>(TrackingMode.Manual);

            var query = photos.Where(p => p.Id >0).ToList();

            foreach(var e  in query)
            {
                Console.WriteLine(e);
            }

        }

        [Fact]
        public async Task TestAvecNot()
        {
            var provider = new LinqLiteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var photoCatalogue = provider.Table<PhotoCatalogue>();

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

        }


        [Fact]
        public void TestAvecValeurBool()
        {
            var provider = new LinqLiteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var photoCatalogue = provider.Table<PhotoCatalogue>();

            Catalogue catalogue = new Catalogue() { Id = 2, Name = "Test" };

            //var result = photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == catalogue.Id && !x.c.IsDeleted).OrderBy(x => x.p.TakenDate).Select(x => x.p).ToList();
            
            var query = photos.Join(photoCatalogue, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == catalogue.Id && x.c.IsDeleted == true).OrderBy(x => x.p.TakenDate).Select(x => x.p);
            string res = SqlFor(query);

            Assert.Equal("SELECT t0.id, t0.filename, t0.takendate, t0.folder, t0.width, t0.height, t0.type, t0.author, t0.camera, t0.make, t0.latitude, t0.longitude, t0.city, t0.country, t0.iso, t0.aperture, t0.shutterspeed, t0.focal, t0.rate, t0.thumbwidth, t0.thumbheight, t0.orientation FROM PHOTO t0 JOIN PHOTO_LIB t1 ON (t0.id = t1.photo_id) WHERE ((t1.lib_id = @v0) AND (t1.deleted = TRUE)) ORDER BY t0.takendate ASC",
                res);
            List<Photo> list = query.ToList();
        }


        [Fact]
        public void TestInsertDelete()
        {
            var provider = new LinqLiteProvider(connectionString);
            var photos = provider.Table<Photo>();

            Photo photo = new Photo()
            {
                Author = "Patorche",
                CameraName = "K100",
                Make = "Pentax",
                Filename = "IMG00SHY2.DNG",
                Focal = "100",
                Folder = @"c:\Imagess",
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

            photos.InsertOrGetId(photo);
            photos.Delete(photo);

        }

        [Fact]
        public void TestFullUpdate()
        {
            var provider = new LinqLiteProvider(connectionString);
            var photos = provider.Table<Photo>();

            Photo photo = new Photo()
            {
                Author = "Patorche",
                CameraName = "K100",
                Make = "Pentax",
                Filename = "IMG00SHYdsf2SS.DNG",
                Focal = "100",
                Folder = @"c:\Imagess",
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

            photos.InsertOrGetId(photo);

            
            var photomodified = new Photo() 
            { 
                Id = photo.Id, 
                Filename = photo.Filename, 
                Make=photo.Make,
                Author = "MODIFIED",
                CameraName= photo.CameraName,
                CameraSetting = photo.CameraSetting,
                Focal= photo.Focal,
                Folder = photo.Folder,
                Height = photo.Height,
                Width = photo.Width,
                IsNew = true,
                Orientation = 7,
                Rate = 2,
                TakenDate = new DateTime(2025, 12, 31),
                ThumbHeight = 100,
                ThumbWidth = 100,
                Localisation = new GpsLocalisation() { City = "Paris", Country = "France", Latitude = 44.0, Longitude = 44.0 },
                Type = "PNG"

            };
            photos.Update(photomodified);
            var p = photos.Single(p => p.Id == photo.Id);
            photos.Delete(photo);
        }


        [Fact]
        public void TestUpdateTracking()
        {
            var provider = new LinqLiteProvider(connectionString);
            var photos = provider.Table<Photo>();

            var p = photos.Single(p => p.Filename == "IMG-20200819-WA0004.jpg");

            p.Rate++;

            var q = photos.Single(p => p.Filename == "IMG-20200819-WA0004.jpg");
            Assert.Equal(p.Rate, q.Rate);
        }

        [Fact]
        public void TestPhotos()
        {
            var provider = new LinqLiteProvider(connectionString);
            var photos = provider.Table<Photo>();
            var photocatalogues = provider.Table<PhotoCatalogue>();
            Catalogue catalogue = new Catalogue() { Id = 7 };
            photos.Join(photocatalogues, p => p.Id, c => c.PhotoId, (p, c) => new { p, c }).Where(x => x.c.CatalogueId == catalogue.Id && !x.c.IsDeleted).OrderByDescending(x => x.p.TakenDate).Select(x => x.p);
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
