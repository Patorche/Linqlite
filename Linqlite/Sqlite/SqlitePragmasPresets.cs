using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Sqlite
{

    public partial class SqlitePragmas
    {
        /// <summary>
        /// Contient des configurations prédéfinies (presets) pour les PRAGMA SQLite.
        /// <para/>
        /// Ces presets fournissent des réglages optimisés pour différents scénarios
        /// d’utilisation (performance, durabilité, lecture seule, etc.).
        /// <para/>
        /// Chaque preset retourne une nouvelle instance de <see cref="SqlitePragmas"/>
        /// entièrement configurée, utilisable telle quelle
        /// ou modifiée avant utilisation.
        /// </summary>
        public static class Preset
        {
            /// <summary>
            /// Configuration optimisée pour des performances maximales en mémoire.
            /// <para/>
            /// <list type="bullet">
            ///   <item><description>Synchronous = Off : aucune synchronisation, vitesse maximale.</description></item>
            ///   <item><description>JournalMode = Memory : journalisation en mémoire, non durable.</description></item>
            ///   <item><description>TempStore = Memory : tables temporaires en RAM.</description></item>
            ///   <item><description>CacheSize = -4000 : environ 4 Mo de cache.</description></item>
            ///   <item><description>ForeignKeys = true : contraintes FK activées.</description></item>
            /// </list>
            /// <para/>
            /// Idéal pour : tests unitaires, calculs temporaires, bases éphémères.
            /// </summary>
            public static SqlitePragmas InMemoryFast => new()
            {
                Synchronous = SqliteSynchronousMode.Off,
                JournalMode = SqliteJournalMode.Memory,
                TempStore = SqliteTempStore.Memory,
                CacheSize = -4000,
                ForeignKeys = true
            };

            /// <summary>
            /// Configuration équilibrée pour une application desktop classique.
            /// <para/>
            /// <list type="bullet">
            ///   <item><description>JournalMode = Wal : excellentes performances en lecture.</description></item>
            ///   <item><description>Synchronous = Normal : bon compromis performance / sécurité.</description></item>
            ///   <item><description>CacheSize = -2000 : environ 2 Mo de cache.</description></item>
            ///   <item><description>ForeignKeys = true : contraintes FK activées.</description></item>
            ///   <item><description>BusyTimeout = 5000 : gestion douce des verrous.</description></item>
            /// </list>
            /// <para/>
            /// Idéal pour : applications WPF/WinUI, outils bureautiques, bases locales.
            /// </summary>
            public static SqlitePragmas DesktopApp => new()
            {
                JournalMode = SqliteJournalMode.Wal,
                Synchronous = SqliteSynchronousMode.Normal,
                TempStore = SqliteTempStore.Default,
                CacheSize = -2000,
                ForeignKeys = true,
                BusyTimeout = 5000
            };

            /// <summary>
            /// Configuration optimisée pour les charges d’écriture intensives.
            /// <para/>
            /// <list type="bullet">
            ///   <item><description>JournalMode = Wal : écritures rapides et lectures concurrentes.</description></item>
            ///   <item><description>Synchronous = Off : maximise la vitesse d’écriture.</description></item>
            ///   <item><description>TempStore = Memory : opérations temporaires rapides.</description></item>
            ///   <item><description>CacheSize = -8000 : environ 8 Mo de cache.</description></item>
            ///   <item><description>BusyTimeout = 10000 : évite les erreurs de verrouillage.</description></item>
            /// </list>
            /// <para/>
            /// Idéal pour : import massif, synchronisation, journaux, traitement batch.
            /// </summary>
            public static SqlitePragmas HighWriteLoad => new()
            {
                JournalMode = SqliteJournalMode.Wal,
                Synchronous = SqliteSynchronousMode.Off,
                TempStore = SqliteTempStore.Memory,
                CacheSize = -8000,
                ForeignKeys = true,
                BusyTimeout = 10000
            };

            /// <summary>
            /// Configuration optimisée pour les bases en lecture seule.
            /// <para/>
            /// <list type="bullet">
            ///   <item><description>JournalMode = Off : aucune journalisation nécessaire.</description></item>
            ///   <item><description>Synchronous = Off : aucune garantie d’écriture requise.</description></item>
            ///   <item><description>TempStore = Memory : opérations temporaires rapides.</description></item>
            ///   <item><description>CacheSize = -4000 : environ 4 Mo de cache.</description></item>
            ///   <item><description>ForeignKeys = false : contraintes inutiles en lecture seule.</description></item>
            /// </list>
            /// <para/>
            /// Idéal pour : catalogues embarqués, données statiques, applications offline.
            /// </summary>
            public static SqlitePragmas ReadOnly => new()
            {
                JournalMode = SqliteJournalMode.Off,
                Synchronous = SqliteSynchronousMode.Off,
                TempStore = SqliteTempStore.Memory,
                CacheSize = -4000,
                ForeignKeys = false
            };
        }
    }

}
