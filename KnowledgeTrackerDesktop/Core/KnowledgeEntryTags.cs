namespace KnowledgeTrackerDesktop.Core
{
    /// <summary>
    /// KnowledgeEntry.Tags is stored as a single varchar column.
    /// Legacy data uses ';' as the separator; ',' is also accepted.
    /// </summary>
    public static class KnowledgeEntryTags
    {
        private static readonly char[] Separators = { ';', ',' };

        public static IReadOnlyList<string> Split(string? tags)
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return Array.Empty<string>();
            }

            return tags.Split(
                Separators,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
