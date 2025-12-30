using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    public static class TypeSystem
    {
        public static Type GetElementType(Type seqType)
        {
            if (seqType.IsGenericType && seqType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return seqType.GetGenericArguments()[0];

            var ienum = seqType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .FirstOrDefault();

            return ienum?.GetGenericArguments()[0] ?? seqType;
        }
    }

}
