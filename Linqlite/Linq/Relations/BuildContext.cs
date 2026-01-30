using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Linqlite.Linq.Relations
{
    public class BuildContext
    {
        public Expression CurrentExpression { get; private set; }
        public LinqliteProvider Provider { get; }

        // Empêche les JOIN en double
        private readonly HashSet<(Type Left, Type Right)> _joins = new();

        // Stocke les sources par type (Photo → IQueryable<Photo>, Keyword → IQueryable<Keyword>, etc.)
        private readonly Dictionary<Type, IQueryable> _sourcesByType = new();

        public BuildContext(Expression source, LinqliteProvider provider)
        {
            CurrentExpression = source;
            Provider = provider;

            // On enregistre la source initiale
            var elementType = source.Type.GetGenericArguments()[0];
            var table = provider.GetTable(elementType);
            _sourcesByType[elementType] = (IQueryable)table;
        }

        /// <summary>
        /// Récupère la source IQueryable<T> correspondant à un type donné.
        /// </summary>
        public IQueryable GetSource(Type type)
        {
            if (_sourcesByType.TryGetValue(type, out var src))
                return src;

            // Si la source n'existe pas encore, on la crée via le provider
            var table = Provider.GetTable(type);
            _sourcesByType[type] = (IQueryable)table;
            return (IQueryable)table;
        }

        /// <summary>
        /// Ajoute un JOIN entre leftType et rightType.
        /// </summary>
        public void AddJoin(Type leftType, Type rightType, string leftKey, string rightKey)
        {
            // Empêcher les JOIN en double
            if (!_joins.Add((leftType, rightType)))
                return;

            // Récupérer les sources correctes
            IQueryable leftSource; 
            if (CurrentExpression != null) 
            { 
                leftSource = Provider.CreateQuery(CurrentExpression); 
            } 
            else 
            { 
                leftSource =  GetSource(leftType); 
            }
            var rightSource = GetSource(rightType);

            // Construire le JOIN
            var joined = JoinBuilder.BuildJoin(
                leftSource,
                leftType,
                rightSource,
                rightType,
                leftKey,
                rightKey
            );

            // Mettre à jour l'expression courante
            // Mettre à jour l’expression courante
            CurrentExpression = joined.Expression; // IMPORTANT : la nouvelle source pour leftType est la jointure
            _sourcesByType[leftType] = joined;
        }
    }
}
