using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Pure presentation logic for the Library list: search, favorite/tag/project
    /// filtering and sorting (PRD sections 10-12, UI_GUIDELINES sections 6-8).
    /// Kept free of WinForms so it stays unit-testable and reusable.
    /// </summary>
    public static class EntryListFilter
    {
        /// <summary>
        /// Applies search, filters and sorting to the loaded collection without
        /// mutating the source or touching persisted data.
        /// </summary>
        public static IReadOnlyList<KnowledgeEntry> Apply(
            IEnumerable<KnowledgeEntry> source,
            string? searchText,
            ISet<string>? selectedTags,
            bool favoritesOnly,
            EntrySortMode sortMode,
            string? projectName = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            var normalizedProject = string.IsNullOrWhiteSpace(projectName)
                ? null
                : projectName.Trim();

            var tags = NormalizeTags(selectedTags);

            var filtered = source.Where(entry =>
                MatchesSearch(entry, searchText) &&
                (!favoritesOnly || entry.IsFavorite) &&
                MatchesProject(entry, normalizedProject) &&
                MatchesAllTags(entry, tags));

            return Sort(filtered, sortMode).ToList();
        }

        /// <summary>
        /// Case-insensitive search across every field listed in PRD section 10.
        /// An empty or whitespace query matches every entry.
        /// </summary>
        public static bool MatchesSearch(KnowledgeEntry entry, string? query)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var needle = query.Trim();
            return Contains(entry.Title, needle) ||
                Contains(entry.Description, needle) ||
                Contains(entry.ProjectName, needle) ||
                Contains(entry.Technologies, needle) ||
                Contains(entry.Tags, needle) ||
                Contains(entry.Comments, needle) ||
                Contains(entry.CodeSnippet, needle);
        }

        /// <summary>Index of the entry with the given stable Id, or -1.</summary>
        public static int FindIndexById(IReadOnlyList<KnowledgeEntry> entries, int? id)
        {
            ArgumentNullException.ThrowIfNull(entries);

            if (id is null)
            {
                return -1;
            }

            for (var index = 0; index < entries.Count; index++)
            {
                if (entries[index].Id == id.Value)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Index of the previously selected entry, falling back to the first
        /// row (or -1 when empty) so selection survives a reload by Id, not by
        /// object reference (UI_DESIGN_SPEC section 8.3).
        /// </summary>
        public static int FindSelectionIndex(IReadOnlyList<KnowledgeEntry> entries, int? id)
        {
            var index = FindIndexById(entries, id);
            if (index >= 0)
            {
                return index;
            }

            return entries.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// Union of every tag across the collection, de-duplicated ignoring
        /// case and ordered case-insensitively.
        /// </summary>
        public static IReadOnlyList<string> CollectTags(IEnumerable<KnowledgeEntry> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source
                .SelectMany(entry => KnowledgeEntryTags.Split(entry.Tags))
                .GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<string> NormalizeTags(ISet<string>? selectedTags)
        {
            if (selectedTags is null || selectedTags.Count == 0)
            {
                return Array.Empty<string>();
            }

            return selectedTags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .ToList();
        }

        private static bool MatchesProject(KnowledgeEntry entry, string? projectName) =>
            projectName is null ||
            string.Equals(
                entry.ProjectName?.Trim(),
                projectName,
                StringComparison.OrdinalIgnoreCase);

        private static bool MatchesAllTags(KnowledgeEntry entry, IReadOnlyList<string> tags)
        {
            if (tags.Count == 0)
            {
                return true;
            }

            var entryTags = KnowledgeEntryTags.Split(entry.Tags);
            foreach (var tag in tags)
            {
                if (!entryTags.Any(candidate =>
                    string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<KnowledgeEntry> Sort(
            IEnumerable<KnowledgeEntry> entries,
            EntrySortMode sortMode) =>
            sortMode switch
            {
                EntrySortMode.DateResolvedAscending => entries
                    .OrderBy(entry => entry.DateResolved.HasValue ? 0 : 1)
                    .ThenBy(entry => entry.DateResolved),
                EntrySortMode.TitleAscending => entries
                    .OrderBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase),
                EntrySortMode.TitleDescending => entries
                    .OrderByDescending(entry => entry.Title, StringComparer.OrdinalIgnoreCase),
                EntrySortMode.ProjectAscending => entries
                    .OrderBy(entry => entry.ProjectName, StringComparer.OrdinalIgnoreCase),
                _ => entries
                    .OrderBy(entry => entry.DateResolved.HasValue ? 0 : 1)
                    .ThenByDescending(entry => entry.DateResolved),
            };

        private static bool Contains(string? value, string needle) =>
            value is not null &&
            value.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}
