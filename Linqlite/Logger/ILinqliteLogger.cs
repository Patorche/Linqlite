using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Logger
{
    public interface ILinqliteLogger
    {
        public bool LogQueries { get; }
        void Log(string message);
    }
}
