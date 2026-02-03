using Linqlite.Linq;
using Linqlite.Models;
using System;
using System.Collections.Generic;
using System.Text;
using ZSpitz.Util;

namespace LinqliteTests
{
    public class GroupByTest : TestBase
    {
        static string connectionString = "E:\\Dev\\Photolab.db\\devBase.db";

        [Fact]
        public void GroupBy()
        {
            var provider = new LinqliteProvider(connectionString);
            //            var photos = provider.Table<Photo>();

            /* var query =
                     from p in provider.Table<Photo>()
                     join pk in provider.Table<PhotoKeyWords>() on p.Id equals pk.PhotoId into pkGroup
                     from pk in pkGroup.DefaultIfEmpty()
                     join k in provider.Table<KeyWord>() on pk.KeyWordId equals k.Id into kGroup
                     from k in kGroup.DefaultIfEmpty()
                     group k by p into g
                     select new
                     {
                         Photo = g.Key,
                         KeyWords = g.Where(x => x != null).ToList()
                     };
            */
            /*    var _query = provider.Table<Photo>().Join(provider.Table<PhotoKeyWords>(), p => p.Id, pk => pk.PhotoId, (p, pk) => new { p, pk })
                                                    .Join(provider.Table<KeyWord>(), kg => kg.pk.KeyWordId, k => k.Id, (kg, k) => new { kg, k })
                                                    .GroupBy(g => g.kg.p);*/
            /* var _query = from p in provider.Table<Photo>() 
                         join pk in provider.Table<PhotoKeyWords>() on p.Id equals pk.PhotoId 
                         join k in provider.Table<KeyWord>() on pk.KeyWordId equals k.Id 
                         group k by p into g 
                         select new 
                         { 
                             Photo = g.Key, 
                             KeyWords = g.ToList() 
                         };

             var result = _query.ToList();*/
            /*var flat = from p in provider.Table<Photo>() 
                       join pk in provider.Table<PhotoKeyWords>() on p.Id equals pk.PhotoId 
                       join k in provider.Table<KeyWord>() on pk.KeyWordId equals k.Id
                       select new {Photo =  p, KeyWord = k}
                       group k by p.Id into g
                       select new { Photo = g.Key, KeyWords = g.ToList() }*/
            ;
            //var fr = flat.ToList();
            /* var grouped = fr.AsEnumerable() // important : on sort du provider
                        .GroupBy(x => x.p.Id)
                        .Select(
                             g => new 
                             { 
                                 Photo = g.First().p, 
                                 KeyWords = g.Select(x => x.k).ToList() 
                             })*/
            ;
            // System.Diagnostics.Debug.Write(result.Count());
            /*   var _query = provider.Table<Photo>().LeftJoin(provider.Table<PhotoKeyWords>(), p => p.Id, pk => pk.PhotoId, (p, pk) => new { p, pk })
                                                       .LeftJoin(provider.Table<KeyWord>(), kg => kg.pk.KeyWordId, k => k.Id, (kg, k) => new { kg, k })
                                                       .Select(g => new { Photo = g.kg.p,  KeyWord = g.k, PhotoKeyWords = g.kg.pk})
                                                       .ToList();*/
            List<Photo> _query2 = provider.Table<Photo>().WithRelations().ToList();

            foreach (Photo p in _query2)
            {
                if (p.KeyWords != null && p.KeyWords.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine(p.Filename);
                    foreach (KeyWord k in p.KeyWords)
                    {
                        System.Diagnostics.Debug.WriteLine(k.Word);
                    }
                }
            }

            /*  using(var e = _query.GetEnumerator())
              {
                  while (e.MoveNext())
                  {
                      var o = e.Current;
                      int k = 0;
                  }
              }

              Assert.NotNull(_query);*/

        }   

        [Fact]
        public void Rel1NAndNN()
        {
            var provider = new LinqliteProvider(connectionString);
            //List<Catalogue> ctas = provider.Table<Catalogue>().WithRelations().ToList();
            var ctas = provider.Table<Catalogue>().WithRelations().ToList();

            int i = 0;
        }


        [Fact]
        public void Rel1NAndNNManuelle()
        {
            var provider = new LinqliteProvider(connectionString);
            var catalogs = provider.Table<Catalogue>();
            var collections = provider.Table<Collection>(); 
            var photocatalogs = provider.Table<PhotoCatalogue>();
            var photos = provider.Table<Photo>();
            var photoskeywords = provider.Table<PhotoKeyWords>();
            var keywords = provider.Table<KeyWord>();

            var ctas = catalogs.LeftJoin(collections, c => c.Id, col => col.CatalogueId, (c, col) => new { c, col }).Where(c => c.c.Id == 1)
                               .LeftJoin(photocatalogs, x => x.c.Id, pc => pc.CatalogueId, (x, pc) => new { x, pc }).Where(pc => pc.pc.IsDeleted == false)
                               .LeftJoin(photos, x => x.pc.PhotoId, p => p.Id, (x, p) => new { x, p })
                               .LeftJoin(photoskeywords, x => x.p.Id, pk => pk.PhotoId, (x, pk) => new { x, pk })
                               .LeftJoin(keywords, pk => pk.pk.KeyWordId, k => k.Id, (pk, k) => new { pk, k })
                               .ToRootList<Catalogue>();

            int i = 0;
        }
    }
}
