namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// Raised when an upgrade or repair failed after the initial state had been
    /// inspected. The application stops instead of starting with a partially
    /// repaired database; when a safety copy was taken first, its location is
    /// exposed through <see cref="BackupPath"/> so it can be restored manually.
    /// Automatic restoration is intentionally not performed.
    /// </summary>
    public sealed class SchemaMigrationException : Exception
    {
        public SchemaMigrationException(string? backupPath, Exception innerException)
            : base(
                backupPath is null
                    ? "The knowledge base could not be upgraded or repaired."
                    : $"The knowledge base could not be upgraded or repaired. " +
                      $"A safety copy is available at '{backupPath}'.",
                innerException)
        {
            BackupPath = backupPath;
        }

        /// <summary>
        /// Safety copy taken before the failed operation, or <c>null</c> when no
        /// backup was required.
        /// </summary>
        public string? BackupPath { get; }
    }
}
