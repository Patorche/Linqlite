using Linqlite.Linq;
using Linqlite.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqliteTests
{
    public class ScriptsTest : TestBase
    {
        static string connectionString = "E:\\Dev\\Photolab.db\\test.db";

        [Fact]
        public void TestScriptTable()
        {
            var provider = new QueryProvider(connectionString);
            provider.Register(new QueryableTable<Photo>());
            provider.Register(new QueryableTable<PhotoCatalogue>());
            provider.Register(new QueryableTable<Catalogue>());
            provider.CreateDatabase("E:\\Dev\\Photolab.db\\test2.db");
        }
    }
}
