using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Linqlite.Sqlite
{
    public class SqliteEntity : ObservableObject
    {
        public bool IsNew = true;

        protected override bool SetProperty<TValue>(ref TValue storage, TValue value, [CallerMemberName] string propertyName = "")
        {
            bool changed = base.SetProperty(ref storage, value, propertyName);
           return changed;
        }

    }

}
