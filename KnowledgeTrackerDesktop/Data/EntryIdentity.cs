namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// The title/project pair that identifies an entry to the user. Used to
    /// detect duplicate imports without comparing database ids, which are local
    /// and never travel in an export.
    /// </summary>
    public readonly record struct EntryIdentity(string Title, string ProjectName)
    {
        /// <summary>
        /// Normalises a raw title/project into the comparison key: trimmed, with
        /// a missing project flattened to an empty string, matched ignoring case.
        /// </summary>
        public static EntryIdentity Of(string? title, string? projectName) =>
            new(
                (title ?? string.Empty).Trim(),
                (projectName ?? string.Empty).Trim());

        public bool Matches(EntryIdentity other) =>
            string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(ProjectName, other.ProjectName, StringComparison.OrdinalIgnoreCase);
    }
}
