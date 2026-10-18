namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// Raised when the database on disk was created by a newer application
    /// version, that is, its <c>PRAGMA user_version</c> is higher than
    /// <see cref="SchemaMigrator.CurrentVersion"/>.
    /// The migrator stops immediately so the newer database is never rewritten,
    /// repaired or downgraded to an older schema version.
    /// </summary>
    public sealed class SchemaVersionTooNewException : Exception
    {
        public SchemaVersionTooNewException(int storedVersion, int supportedVersion)
            : base(
                $"The knowledge base schema version ({storedVersion}) is newer than the highest " +
                $"version supported by this application ({supportedVersion}). The database was not modified.")
        {
            StoredVersion = storedVersion;
            SupportedVersion = supportedVersion;
        }

        /// <summary>Value read from <c>PRAGMA user_version</c>.</summary>
        public int StoredVersion { get; }

        /// <summary>Highest schema version supported by this build.</summary>
        public int SupportedVersion { get; }
    }
}
