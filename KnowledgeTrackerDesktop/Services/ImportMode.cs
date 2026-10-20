namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// What an import should do with an entry that already exists locally.
    /// Duplicates are matched on title + project, case-insensitive, which is
    /// what the user sees in the list; database ids are never used because an
    /// imported entry is always a new record.
    /// </summary>
    public enum ImportMode
    {
        /// <summary>Keep the local entry, skip the imported one.</summary>
        SkipDuplicates = 0,

        /// <summary>Import the entry anyway, as an additional copy.</summary>
        ImportAsCopies,
    }

    /// <summary>Outcome of an import, used for the summary the UI shows.</summary>
    public sealed class ImportResult
    {
        public ImportResult(int imported, int skippedDuplicates, int invalid)
        {
            Imported = imported;
            SkippedDuplicates = skippedDuplicates;
            Invalid = invalid;
        }

        /// <summary>How many entries were written.</summary>
        public int Imported { get; }

        /// <summary>How many were left out because the entry already existed.</summary>
        public int SkippedDuplicates { get; }

        /// <summary>
        /// How many were left out because they do not satisfy the entry rules
        /// (for example a missing project or resolution date). An export from an
        /// older or hand-edited file can contain these.
        /// </summary>
        public int Invalid { get; }

        public int Total => Imported + SkippedDuplicates + Invalid;
    }
}
