using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Sqlite
{
    /// <summary>
    /// Pragmas Sqlite
    /// </summary>
    public partial class SqlitePragmas
    {
        // -------------------------
        // Journalisation & Durabilité
        // -------------------------

        /// <summary>
        /// Mode de journalisation SQLite. WAL est généralement le meilleur compromis
        /// pour les applications desktop avec beaucoup de lectures.
        /// </summary>
        public SqliteJournalMode JournalMode { get; set; } = SqliteJournalMode.Wal;

        /// <summary>
        /// Niveau de synchronisation. Normal offre un bon compromis entre performance
        /// et durabilité.
        /// </summary>
        public SqliteSynchronousMode Synchronous { get; set; } = SqliteSynchronousMode.Normal;

        /// <summary>
        /// Taille du checkpoint automatique WAL en pages. 1000 est une valeur raisonnable.
        /// </summary>
        public int WalAutoCheckpoint { get; set; } = 1000;

        /// <summary>
        /// Active le fsync complet lors des checkpoints WAL.
        /// </summary>
        public bool WalCheckpointFullfsync { get; set; } = false;

        /// <summary>
        /// Détermine où les tables temporaires sont stockées.
        /// </summary>
        public SqliteTempStore TempStore { get; set; } = SqliteTempStore.Default;


        // -------------------------
        // Performance & Mémoire
        // -------------------------

        /// <summary>
        /// Taille du cache en pages. Valeur négative = taille en Ko.
        /// </summary>
        public int CacheSize { get; set; } = -2000; // ~2 Mo

        /// <summary>
        /// Taille de page SQLite. 4096 est standard et performant.
        /// </summary>
        public int PageSize { get; set; } = 4096;

        /// <summary>
        /// Taille de la mémoire mappée. 0 = désactivé.
        /// </summary>
        public long MmapSize { get; set; } = 0;

        /// <summary>
        /// Mode de verrouillage. Normal convient à 99% des cas.
        /// </summary>
        public SqliteLockingMode LockingMode { get; set; } = SqliteLockingMode.Normal;


        // -------------------------
        // Intégrité & Sécurité
        // -------------------------

        /// <summary>
        /// Active les contraintes de clés étrangères.
        /// </summary>
        public bool ForeignKeys { get; set; } = true;

        /// <summary>
        /// Ignore les contraintes CHECK invalides.
        /// </summary>
        public bool IgnoreCheckConstraints { get; set; } = false;

        /// <summary>
        /// Effacement sécurisé (remplissage des pages supprimées).
        /// </summary>
        public bool SecureDelete { get; set; } = false;


        // -------------------------
        // Concurrence
        // -------------------------

        /// <summary>
        /// Timeout en millisecondes lorsqu'une ressource est verrouillée.
        /// </summary>
        public int BusyTimeout { get; set; } = 5000;

        /// <summary>
        /// Permet la lecture non validée (dirty reads).
        /// </summary>
        public bool ReadUncommitted { get; set; } = false;


        // -------------------------
        // Débogage & Analyse
        // -------------------------

        /// <summary>
        /// Active l'optimisation automatique.
        /// </summary>
        public bool Optimize { get; set; } = true;

        /// <summary>
        /// Limite d'analyse pour les statistiques.
        /// </summary>
        public int AnalysisLimit { get; set; } = 1000;

        public IEnumerable<string> ToSqlCommands()
        {
            if (JournalMode != SqliteJournalMode.Wal)
                yield return $"PRAGMA journal_mode = {JournalMode.ToString().ToLowerInvariant()};";

            if (Synchronous != SqliteSynchronousMode.Normal)
                yield return $"PRAGMA synchronous = {(int)Synchronous};";

            if (CacheSize != -2000)
                yield return $"PRAGMA cache_size = {CacheSize};";

            if (PageSize != 4096)
                yield return $"PRAGMA page_size = {PageSize};";

            if (ForeignKeys != true)
                yield return $"PRAGMA foreign_keys = {(ForeignKeys ? 1 : 0)};";

            if (LockingMode != SqliteLockingMode.Normal)
                yield return $"PRAGMA locking_mode = {LockingMode.ToString().ToLowerInvariant()};";

            if (MmapSize > 0)
                yield return $"PRAGMA mmap_size = {MmapSize};";

            if (TempStore != SqliteTempStore.Default)
                yield return $"PRAGMA temp_store = {((int)TempStore)};";

            if (BusyTimeout != 5000)
                yield return $"PRAGMA busy_timeout = {BusyTimeout};";

            if (ReadUncommitted)
                yield return $"PRAGMA read_uncommitted = 1;";

            if (IgnoreCheckConstraints)
                yield return $"PRAGMA ignore_check_constraints = 1;";

            if (SecureDelete)
                yield return $"PRAGMA secure_delete = 1;";

            if (Optimize)
                yield return "PRAGMA optimize;";
        }


    }

    /// <summary>
    /// Mode de journalisation SQLite.
    /// <para/>
    /// <list type="bullet">
    ///   <item><description>Delete : crée un fichier journal temporaire supprimé après chaque transaction réussie.</description></item>
    ///   <item><description>Truncate : tronque le fichier journal au lieu de le supprimer (souvent plus rapide).</description></item>
    ///   <item><description>Persist : conserve le fichier journal mais le réinitialise après chaque transaction.</description></item>
    ///   <item><description>Memory : stocke le journal en mémoire (rapide mais non durable).</description></item>
    ///   <item><description>Wal : active le Write-Ahead Logging, permettant des lectures concurrentes pendant les écritures.</description></item>
    ///   <item><description>Off : désactive la journalisation (risque élevé de corruption).</description></item>
    /// </list>
    /// </summary>
    public enum SqliteJournalMode
    {
        /// <summary>Crée un fichier journal temporaire supprimé après chaque transaction réussie.</summary>
        Delete,

        /// <summary>Tronque le fichier journal au lieu de le supprimer (souvent plus rapide).</summary>
        Truncate,

        /// <summary>Conserve le fichier journal mais le réinitialise après chaque transaction.</summary>
        Persist,

        /// <summary>Stocke le journal en mémoire (rapide mais non durable).</summary>
        Memory,

        /// <summary>Active le Write-Ahead Logging, permettant des lectures concurrentes pendant les écritures.</summary>
        Wal,

        /// <summary>Désactive la journalisation (risque élevé de corruption).</summary>
        Off
    }



    /// <summary>
    /// Niveau de synchronisation des écritures.
    /// <para/>
    /// <list type="bullet">
    ///   <item><description>Off (0) : aucune synchronisation. Très rapide, mais risque de corruption en cas de crash.</description></item>
    ///   <item><description>Normal (1) : synchronisation réduite. Bon compromis performance / sécurité.</description></item>
    ///   <item><description>Full (2) : synchronisation complète à chaque écriture.</description></item>
    ///   <item><description>Extra (3) : synchronisation encore plus stricte (rarement utile).</description></item>
    /// </list>
    /// </summary>
    public enum SqliteSynchronousMode
    {
        /// <summary>Aucune synchronisation. Très rapide, mais risque de corruption en cas de crash.</summary>
        Off = 0,

        /// <summary>Synchronisation réduite. Bon compromis performance / sécurité.</summary>
        Normal = 1,

        /// <summary>Synchronisation complète à chaque écriture.</summary>
        Full = 2,

        /// <summary>Synchronisation encore plus stricte (rarement utile).</summary>
        Extra = 3
    }



    /// <summary>
    /// Détermine où SQLite stocke les tables temporaires.
    /// <para/>
    /// <list type="bullet">
    ///   <item><description>Default (0) : utilise la valeur de compilation ou de configuration.</description></item>
    ///   <item><description>File (1) : stocke les données temporaires sur disque.</description></item>
    ///   <item><description>Memory (2) : stocke les données temporaires en mémoire (plus rapide).</description></item>
    /// </list>
    /// </summary>
    public enum SqliteTempStore
    {
        /// <summary>Utilise la valeur de compilation ou de configuration.</summary>
        Default = 0,

        /// <summary>Stocke les données temporaires sur disque.</summary>
        File = 1,

        /// <summary>Stocke les données temporaires en mémoire (plus rapide).</summary>
        Memory = 2
    }



    /// <summary>
    /// Mode de verrouillage de la base.
    /// <para/>
    /// <list type="bullet">
    ///   <item><description>Normal : mode par défaut, verrouillage partagé/écrit classique.</description></item>
    ///   <item><description>Exclusive : garde la base verrouillée exclusivement après ouverture.</description></item>
    /// </list>
    /// </summary>
    public enum SqliteLockingMode
    {
        /// <summary>Mode par défaut, verrouillage partagé/écrit classique.</summary>
        Normal,

        /// <summary>Garde la base verrouillée exclusivement après ouverture.</summary>
        Exclusive
    }



}
