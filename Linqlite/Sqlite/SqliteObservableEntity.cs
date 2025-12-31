using Linqlite.Mapping;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Linqlite.Sqlite
{
    public class SqliteObservableEntity : ObservableObject
    {
        public bool IsNew = true;

        protected override bool SetProperty<TValue>(ref TValue storage, TValue value, [CallerMemberName] string propertyName = "")
        {
            bool changed = base.SetProperty(ref storage, value, propertyName);

            if (changed)
                OnPropertyChangedForSqlite(propertyName, value);

            return changed;
        }

        private void OnPropertyChangedForSqlite<TValue>(string propertyName, TValue value)
        {
          /*  
            if (IsNew)
                return;

            var entityType = GetType();

            // 1. Récupérer le mapping propre
            var mapType = typeof(EntityMap<>).MakeGenericType(entityType);
            var columnsProp = mapType.GetProperty("Columns", BindingFlags.Public | BindingFlags.Static);
            var columns = (List<EntityPropertyInfo>)columnsProp.GetValue(null);

            // 2. Trouver la colonne correspondant à la propriété modifiée
            var col = columns.FirstOrDefault(c => c.PropertyPath.Last().Name == propertyName);
            if (col == null)
                return;

            // 3. Construire un dictionnaire { columnName → value }
            var updateDict = new Dictionary<string, object?>
            {
                [col.ColumnName] = value
            };
            
            
            /*var controllerType = typeof(SqliteAccessController<>).MakeGenericType(entityType);
            var instance = controllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            var updateMethod = controllerType.GetMethod("UpdateAsync");
            updateMethod?.Invoke(instance, new object[] { this, updateDict });*/
        }
    }

}
