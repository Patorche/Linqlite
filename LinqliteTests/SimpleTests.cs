using Linqlite.Linq;
using Linqlite.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqliteTests
{
    public class SimpleTests : TestBase
    {

        [Fact]
        public void Base()
        {
            var provider = new LinqliteProvider("E:\\Dev\\Photolab.db\\photolab.db");
            var catalogues = provider.Table<Catalogue>();
            var sql = SqlFor(catalogues, provider);
            Assert.Equal("SELECT t0.id AS t0_id, t0.name AS t0_name, t0.creation_date AS t0_creation_date FROM LIBRARY t0", sql);
        }

        [Fact]
        public void Where()
        {
            var provider = new LinqliteProvider("E:\\Dev\\Photolab.db\\photolab.db");
            var catalogues = provider.Table<Catalogue>();

            var sql = SqlFor(catalogues.Where(c => c.Id == 7), provider);
            Assert.Equal("SELECT t0.id AS t0_id, t0.name AS t0_name, t0.creation_date AS t0_creation_date FROM LIBRARY t0 WHERE (t0.id = 7)", sql);

        }

        [Fact]
        public void OrderBy1()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Id > 100).OrderBy(p => p.Filename).ThenBy(p => p.Folder), provider);
            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0 WHERE (t0.id > 100) ORDER BY t0.filename ASC, t0.folder ASC", sql);
        }

        [Fact]
        public void OrderByDesc()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Id > 100).OrderByDescending(p => p.Filename), provider);
            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0 WHERE (t0.id > 100) ORDER BY t0.filename DESC", sql);
        }

        [Fact]
        public void OrderByThenByThenBydescending1()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Id > 100).OrderBy(p => p.Filename).ThenBy(p => p.Folder).ThenByDescending(p => p.Width), provider);
            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0 WHERE (t0.id > 100) ORDER BY t0.filename ASC, t0.folder ASC, t0.width DESC", sql);
        }

        [Fact]
        public void Take()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Take(100), provider);
            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0 LIMIT 100", sql);
        }

        [Fact]
        public void Skip()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Skip(100), provider);
            Assert.Equal("SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation FROM PHOTO t0 LIMIT -1 OFFSET 100", sql);
        }

        [Fact]
        public void OrderBy_After_Select_Should_Generate_Single_OrderBy_Clause()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var query = photos
                .Select(p => new { p.Id, p.Filename })
                .OrderBy(x => x.Filename)
                .ThenBy(x => x.Id);

            var sql = SqlFor(query, provider);

            Assert.Equal(
                "SELECT t0.id AS t0_id, t0.filename AS t0_filename FROM PHOTO t0 ORDER BY t0.filename ASC, t0.id ASC",
                sql
            );
        }

        [Fact]
        public void OrderBy_ThenBy_After_Join_Should_Produce_Single_OrderBy_Clause()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();
            var catalog = provider.Table<Catalogue>();


            var query = photos
                .Join(catalog, p => p.Id, c => c.Id, (p, c) => new { p, c })
                .OrderBy(x => x.p.Filename)
                .ThenBy(x => x.c.CreationDate);

            var sql = SqlFor(query, provider);

            Assert.Equal(
                "SELECT t0.id AS t0_id, t0.filename AS t0_filename, t0.takendate AS t0_takendate, t0.folder AS t0_folder, t0.width AS t0_width, t0.height AS t0_height, t0.type AS t0_type, t0.author AS t0_author, t0.camera AS t0_camera, t0.make AS t0_make, t0.latitude AS t0_latitude, t0.longitude AS t0_longitude, t0.city AS t0_city, t0.country AS t0_country, t0.iso AS t0_iso, t0.aperture AS t0_aperture, t0.shutterspeed AS t0_shutterspeed, t0.focal AS t0_focal, t0.rate AS t0_rate, t0.thumbwidth AS t0_thumbwidth, t0.thumbheight AS t0_thumbheight, t0.orientation AS t0_orientation, t1.id AS t1_id, t1.name AS t1_name, t1.creation_date AS t1_creation_date FROM PHOTO t0 JOIN LIBRARY t1 ON (t0.id = t1.id) ORDER BY t0.filename ASC, t1.creation_date ASC",
                sql
            );
        }

        [Fact]
        public void JoinSelectProjection()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();
            var catalogues = provider.Table<Catalogue>();

            var query = photos
                .Join(catalogues, p => p.Id, c => c.Id, (p, c) => new { p, c })
                .Select(j => new { j.p.Id, j.p.Filename, cId= j.c.Id })
                .OrderBy(x => x.Filename)
                .ThenBy(x => x.Id)
                .ThenBy(x => x.cId);

            var sql = SqlFor(query, provider);

            Assert.Equal(
                "SELECT t0.id AS t0_id, t0.filename AS t0_filename, t1.id AS t1_id FROM PHOTO t0 JOIN LIBRARY t1 ON (t0.id = t1.id) ORDER BY t0.filename ASC, t0.id ASC, t1.id ASC",
                sql
            );
        }


    }
}
