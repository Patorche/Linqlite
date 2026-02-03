using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ZSpitz.Util;

namespace Linqlite.Linq
{
    internal class RootEntityExtractor
    {
        public static object ExtractRootEntities(IEnumerable anonList)
        {
            //var result = new List<object>();
            var seen = new HashSet<object>();
            if (anonList == null || anonList.ToObjectList().Count == 0) 
            {
                return new List<object>();
            }
            var entityType = anonList.GetType().GetGenericArguments()[0].GetProperties()[0].PropertyType;
            var listType = typeof(List<>).MakeGenericType(entityType); 
            var list = (IList)Activator.CreateInstance(listType);

            //var prop = anonList.GetType()
            //                       .GetProperties()
            //                       .First(p => p.PropertyType == entityType);
            var prop = anonList.GetType().GetGenericArguments()[0].GetProperties()[0];

            foreach (var anon in anonList)
            {

                // 2. Récupérer l'entité
                var entity = prop.GetValue(anon);

                // 3. Déduplication
                if (seen.Add(entity))
                    list.Add(entity);
            }

            return list;
        }
    }
}
