namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// Raised when an import document cannot be read: it is not valid JSON, it
    /// is missing its structure, or it was written by a newer format version.
    /// Kept separate from <see cref="System.Text.Json.JsonException"/> so the UI
    /// can show one clear message for every unreadable file.
    /// </summary>
    public sealed class KnowledgeExportFormatException : Exception
    {
        public KnowledgeExportFormatException()
            : base("The file is not a readable KnowledgeTracker export.")
        {
        }

        public KnowledgeExportFormatException(bool newerFormat)
            : base(newerFormat
                ? "The file was written by a newer version of KnowledgeTracker."
                : "The file is not a readable KnowledgeTracker export.")
        {
            IsNewerFormat = newerFormat;
        }

        /// <summary>
        /// True when the file is well formed but was written by a newer format
        /// version, which is a different problem to report than a broken file.
        /// </summary>
        public bool IsNewerFormat { get; }
    }
}
