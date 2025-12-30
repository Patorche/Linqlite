using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Linqlite.Linq
{
    public static class SqlCleaner
    {
        public static string Clean(string sql)
        {
            return Regex.Replace(sql, @"\s+", " ").Trim();
        }
    }

}
