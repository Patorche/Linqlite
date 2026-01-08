using Linqlite.Attributes;
using Linqlite.Mapping;
using Linqlite.Sqlite;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Security.Principal;
using System.Text;

namespace Linqlite.Linq.SqlGeneration
{
    internal class TableScriptGenerator
    {
        private StringBuilder _sb = new();
        private StringBuilder fk = new();
        private StringBuilder constraints = new();
        private Type _type;
        private List<KeyValuePair<string, ConflictAction>> _uniques = new();

        public List<Type> ForeignTables { get; private set; } = new();
        public Type EntityType => _type;
        public StringBuilder Script => _sb;
        
        
        public void Build(Type type)
        {
            _type = type;
            var map = EntityMap.Get(type);
            bool hasColumn = false;

            _sb.Append($"CREATE TABLE \"{map.TableName}\" (").AppendLine();
            foreach (var column in map.Columns) 
            { 
                GenerateForeignKey(column);
                GeneratePrimaryKey(column);

                if (hasColumn) _sb.Append(",").AppendLine();
                
                _sb.Append(GenerateColumn(column));

                hasColumn = true;
            }
            if (constraints.Length > 0)
            {
                _sb.Append(',').AppendLine().Append(constraints);
            }
            if (fk.Length > 0)
            {
                _sb.Append(',').AppendLine().Append(fk);
            }


            for(int i = 0; i < map.UniqueConstraints.Count; i++)
            {
                _sb.Append(',').AppendLine();
                IUniqueConstraint c = map.UniqueConstraints[i];
                if(c.OnConflict == ConflictAction.None)
                    continue;
                var conflict = GetOnConflict(c.OnConflict);
                var cols = string.Join(", ", c.Columns.Select(s => $"\"{s}\""));
                _sb.Append('\t').Append($"UNIQUE({cols}) ON CONFLICT {conflict}");
            }


            _sb.AppendLine().Append(");");
            
        }
        private string GetOnConflict(ConflictAction value)
        {
            var res = value switch
            {
                ConflictAction.Replace => "REPLACE",
                ConflictAction.Fail => "FAIL",
                ConflictAction.Rollback => "ROLLBACK",
                ConflictAction.Abort => "ABORT",
                ConflictAction.Ignore => "IGNORE",
                _ => "IGNORE"
            };
            return res;
        }

        private StringBuilder GenerateColumn(EntityPropertyInfo column)
        {
            StringBuilder sb = new StringBuilder();
            if (string.IsNullOrEmpty(column.ColumnName))
            {
                return GenerateSubEntityColumns(column.PropertyType);
            }

            string type = GetSqlType(column.PropertyType);
            sb.Append('\t')
                .Append($"\"{column.ColumnName}\" {type}");
            if (column.IsNotNull || column.IsPrimaryKey)
                sb.Append(" NOT NULL");
            if (column.IsUnique)
            {
                _uniques.Add(new KeyValuePair<string, ConflictAction>(column.ColumnName, column.ConflictAction ?? ConflictAction.Ignore));
            }
            return sb;
        }

        private StringBuilder GenerateSubEntityColumns(Type type)
        {
            StringBuilder sb = new StringBuilder();
            var map = EntityMap.Get(type);
            bool hasCol = false;
            foreach (var column in map.Columns)
            {
                if (string.IsNullOrEmpty(column.ColumnName))
                {
                    sb.Append(GenerateSubEntityColumns(column.PropertyType));
                }
                else
                {
                    if (hasCol)
                        sb.Append(',').AppendLine();
                    sb.Append(GenerateColumn(column));
                    hasCol = true;
                }
            }
            return sb;
        }

        private string GetSqlType(Type propertyType)
        {
            // Gestion des Nullable<T>
            var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(short) ||
                type == typeof(byte))
                return "INTEGER";

            if (type == typeof(bool))
                return "INTEGER"; // SQLite n'a pas de bool, 0/1

            if (type == typeof(float) ||
                type == typeof(double))
                return "REAL";

            if (type == typeof(decimal))
                return "REAL"; // SQLite stocke DECIMAL comme REAL

            if (type == typeof(string))
                return "TEXT"; // valeur par défaut, tu peux ajuster

            if (type == typeof(DateTime))
                return "TEXT";

            if (type == typeof(Guid))
                return "TEXT";

            if (type.IsEnum)
                return "INTEGER"; // SQLite stocke les enums comme int

            // Fallback générique
            return "TEXT";
        }


        private void GeneratePrimaryKey(EntityPropertyInfo column)
        {
            if(!column.IsPrimaryKey) return;

            // PRIMARY KEY("id" AUTOINCREMENT)
            if (constraints.Length > 0)
            {
                constraints.Append(',');
                constraints.AppendLine();
            }
            constraints.Append('\t')
                .Append($"PRIMARY KEY(\"{column.ColumnName}\"");
            if (column.IsAutoIncrement)
                constraints.Append(" AUTOINCREMENT");
            constraints.Append(")");
        }

        private void GenerateForeignKey(EntityPropertyInfo column)
        {
            if (column.ForeignKey is null) return;

            if (fk.Length > 0)
            {
                fk.Append(',');
                fk.AppendLine();
            }

            var f = column.ForeignKey;
            ForeignTables.Add(f.Value.Entity);
            fk.Append('\t').Append("FOREIGN KEY ");
            fk.Append($"({column.ColumnName})");
            fk.Append($" REFERENCES {EntityMap.Get(f.Value.Entity).TableName}({EntityMap.Get(f.Value.Entity).Column(f.Value.Key)})");
            if (f.Value.CascadeDelete)
            {
                fk.Append(" ON DELETE CASCADE");
            }
        }
    }
}
