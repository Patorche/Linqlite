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
            var sql = SqlFor(catalogues);
            Assert.Equal("SELECT t0.* FROM LIBRARY t0", sql);
        }

        [Fact]
        public void Where()
        {
            var provider = new LinqliteProvider("E:\\Dev\\Photolab.db\\photolab.db");
            var catalogues = provider.Table<Catalogue>();

            var sql = SqlFor(catalogues.Where(c => c.Id == 7));
            Assert.Equal("SELECT t0.* FROM LIBRARY t0 WHERE (t0.id = 7)", sql);

        }

        [Fact]
        public void OrderBy1()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Id > 100).OrderBy(p => p.Filename).ThenBy(p => p.Folder));
            Assert.Equal("SELECT t0.* FROM PHOTO t0 WHERE (t0.id > 100) ORDER BY t0.filename ASC, t0.folder ASC", sql);
        }

        [Fact]
        public void OrderByDesc()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Id > 100).OrderByDescending(p => p.Filename));
            Assert.Equal("SELECT t0.* FROM PHOTO t0 WHERE (t0.id > 100) ORDER BY t0.filename DESC", sql);
        }

        [Fact]
        public void OrderByThenByThenBydescending1()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Where(p => p.Id > 100).OrderBy(p => p.Filename).ThenBy(p => p.Folder).ThenByDescending(p => p.Width));
            Assert.Equal("SELECT t0.* FROM PHOTO t0 WHERE (t0.id > 100) ORDER BY t0.filename ASC, t0.folder ASC, t0.width DESC", sql);
        }

        [Fact]
        public void Take()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Take(100));
            Assert.Equal("SELECT t0.* FROM PHOTO t0 LIMIT 100", sql);
        }

        [Fact]
        public void Skip()
        {
            var provider = new LinqliteProvider();
            var photos = provider.Table<Photo>();

            var sql = SqlFor(photos.Skip(100));
            Assert.Equal("SELECT t0.* FROM PHOTO t0 LIMIT -1 OFFSET 100", sql);
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

            var sql = SqlFor(query);

            Assert.Equal(
                "SELECT t0.id, t0.filename FROM PHOTO t0 ORDER BY t0.filename ASC, t0.id ASC",
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

            var sql = SqlFor(query);

            Assert.Equal(
                "SELECT t0.* FROM PHOTO t0 JOIN LIBRARY t1 ON (t0.id = t1.id) ORDER BY t0.filename ASC, t1.creation_date ASC",
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

            var sql = SqlFor(query);

            Assert.Equal(
                "SELECT t0.id, t0.filename, t1.id FROM PHOTO t0 JOIN LIBRARY t1 ON (t0.id = t1.id) ORDER BY t0.filename ASC, t0.id ASC, t1.id ASC",
                sql
            );
        }


    }
}
