namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// Raised when the safety copy taken before a schema upgrade or repair could
    /// not be created (for example, because the disk is full or the folder is not
    /// writable). The migration is aborted so the database is never changed
    /// without a backup, and the existing backups are never overwritten.
    /// </summary>
    public sealed class DatabaseBackupException : Exception
    {
        public DatabaseBackupException(string backupPath, Exception innerException)
            : base($"The database could not be backed up to '{backupPath}'.", innerException)
        {
            BackupPath = backupPath;
        }

        /// <summary>Path the backup was meant to be written to.</summary>
        public string BackupPath { get; }
    }
}
