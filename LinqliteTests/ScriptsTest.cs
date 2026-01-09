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
            //if(File.Exists("E:\\Dev\\Photolab.db\\test.db"))
            //    File.Delete("E:\\Dev\\Photolab.db\\test.db");
            var provider = new QueryProvider(connectionString); // Piour valider le changement de fichier
            provider.Table<Photo>();
            provider.Table<PhotoCatalogue>();
            provider.Table<Catalogue>();
            provider.EnsureTablesCreated();
            provider.Disconnect();
            
        }
    }
}
