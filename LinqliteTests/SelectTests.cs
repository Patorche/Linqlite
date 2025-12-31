using Linqlite.Linq;
using Linqlite.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqliteTests
{
    public class SelectTests : TestBase
    {
        [Fact]
        public void SelectSimple() 
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);

            var sql = SqlFor(photos);

            Assert.Equal("SELECT * FROM PHOTO t0".Trim(),sql.Trim());
        }

        [Fact]
        public void SelectSimple2()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);

            var sql = SqlFor(photos.Where(p => true));

            Assert.Equal("SELECT * FROM PHOTO t0 WHERE True", sql);
        }

        [Fact]
        public void SelectSimple3()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);

            var sql = SqlFor(photos.Where(p => p.Id == 15));

            Assert.Equal("SELECT * FROM PHOTO t0 WHERE (t0.id = 15)", sql);
        }

        [Fact]
        public void SelectSimple4()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);

            var sql = SqlFor(photos.Where(p => p.Localisation.Latitude >= 35 && p.Width > 0));

            Assert.Equal("SELECT * FROM PHOTO t0 WHERE ((t0.latitude >= 35) AND (t0.width > 0))", sql);
        }

        [Fact]
        public void SelectSimple5()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);

            var sql = SqlFor(photos.Select(p => new { p.Id, p.Filename }));

            Assert.Equal("SELECT t0.id, t0.filename FROM PHOTO t0", sql);
        }


        [Fact]
        public void SelectSimple6()
        {
            var provider = new QueryProvider();
            var photos = new QueryableTable<Photo>(provider);

            var sql = SqlFor(photos.Where(p => p.Author.Contains("pat")).Select(p => new { p.Id, p.Filename }));

            Assert.Equal("SELECT t0.id, t0.filename FROM PHOTO t0 WHERE (t0.author LIKE '%pat%')", sql);
        }
    }
}
