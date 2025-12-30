using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    public class ProjectionMap
    {
        // Nom projeté dans le type anonyme (ex: "PhotoId")
        public string ProjectedName { get; set; }

        // Chemin complet de l'expression (ex: "p.Id")
        public string FullPath { get; set; }

        // Type racine (ex: typeof(Photo))
        public Type RootType { get; set; }

        // Nom de la propriété (ex: "Id")
        public string PropertyName { get; set; }
    }

}
