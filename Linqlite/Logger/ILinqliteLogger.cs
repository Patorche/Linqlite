using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Logger
{
    public interface ILinqliteLogger
    {
        void Log(string message);
    }
}
