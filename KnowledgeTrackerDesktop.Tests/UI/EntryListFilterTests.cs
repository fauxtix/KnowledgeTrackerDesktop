using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Pure filtering/sorting behaviour of the main list
    /// (PRD sections 10-12, UI_GUIDELINES sections 6-8).
    /// </summary>
    public class EntryListFilterTests
    {
        private static KnowledgeEntry Entry(
            int id = 1,
            string title = "Title",
            string? description = null,
            string? project = null,
            string? technologies = null,
            string? tags = null,
            string? comments = null,
            string? codeSnippet = null,
            DateTime? dateResolved = null,
            bool isFavorite = false)
        {
            return new KnowledgeEntry
            {
                Id = id,
                Title = title,
                Description = description,
                ProjectName = project,
                Technologies = technologies,
                Tags = tags,
                Comments = comments,
                CodeSnippet = codeSnippet,
                DateResolved = dateResolved,
                IsFavorite = isFavorite,
            };
        }

        [Fact]
        public void FindIndexById_RestoresSelectionAcrossReloadedInstances()
        {
            var previousInstance = Entry(id: 42, title: "Selected");
            var reloadedEntries = new[]
            {
                Entry(id: 7, title: "First"),
                Entry(id: 42, title: "Selected"),
            };

            Assert.NotSame(previousInstance, reloadedEntries[1]);
            Assert.Equal(1, EntryListFilter.FindIndexById(reloadedEntries, previousInstance.Id));
            Assert.Equal(-1, EntryListFilter.FindIndexById(reloadedEntries, null));
            Assert.Equal(-1, EntryListFilter.FindIndexById(reloadedEntries, 404));
        }

        [Fact]
        public void FindSelectionIndex_PreservesExistingIdOrFallsBackToFirstResult()
        {
            var entries = new[]
            {
                Entry(id: 7),
                Entry(id: 42),
            };

            Assert.Equal(1, EntryListFilter.FindSelectionIndex(entries, 42));
            Assert.Equal(0, EntryListFilter.FindSelectionIndex(entries, 404));
            Assert.Equal(-1, EntryListFilter.FindSelectionIndex(Array.Empty<KnowledgeEntry>(), 42));
        }

        [Fact]
        public void MatchesSearch_IsCaseInsensitive_AcrossEveryPrdField()
        {
            var fields = new[]
            {
                Entry(title: "needle"),
                Entry(description: "needle"),
                Entry(project: "needle"),
                Entry(technologies: "needle"),
                Entry(tags: "needle"),
                Entry(comments: "needle"),
                Entry(codeSnippet: "needle"),
            };

            Assert.All(fields, entry => Assert.True(EntryListFilter.MatchesSearch(entry, "NEEDLE")));
        }

        [Fact]
        public void MatchesSearch_EmptyQuery_MatchesEverything()
        {
            Assert.True(EntryListFilter.MatchesSearch(Entry(), "  "));
            Assert.True(EntryListFilter.MatchesSearch(Entry(), null));
        }

        [Fact]
        public void MatchesSearch_NoMatch_ReturnsFalse()
        {
            Assert.False(EntryListFilter.MatchesSearch(Entry(title: "Windows update"), "linux"));
        }

        [Fact]
        public void Apply_FavoritesOnly_KeepsJustFavorites()
        {
            var source = new[]
            {
                Entry(id: 1, isFavorite: true),
                Entry(id: 2, isFavorite: false),
                Entry(id: 3, isFavorite: true),
            };

            var result = EntryListFilter.Apply(
                source, null, new HashSet<string>(), favoritesOnly: true,
                EntrySortMode.DateResolvedDescending);

            Assert.Equal(new[] { 1, 3 }, result.Select(e => e.Id));
        }

        [Fact]
        public void Apply_TagFilter_RequiresEverySelectedTag()
        {
            var source = new[]
            {
                Entry(id: 1, tags: "bug;ui"),
                Entry(id: 2, tags: "qa"),
                Entry(id: 3, tags: "unrelated"),
            };

            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bug", "qa" };

            var result = EntryListFilter.Apply(
                source, null, selected, favoritesOnly: false,
                EntrySortMode.DateResolvedDescending);

            Assert.Empty(result);

            var matching = EntryListFilter.Apply(
                new[]
                {
                    Entry(id: 1, tags: "bug;qa;ui"),
                    Entry(id: 2, tags: "bug"),
                    Entry(id: 3, tags: "qa"),
                },
                null,
                selected,
                favoritesOnly: false,
                EntrySortMode.DateResolvedDescending);

            Assert.Equal(new[] { 1 }, matching.Select(e => e.Id));
        }

        [Fact]
        public void Apply_ProjectFilter_IsSingleValueAndCombinesWithOtherFilters()
        {
            var source = new[]
            {
                Entry(id: 1, project: "KnowledgeTracker", tags: "ui", isFavorite: true),
                Entry(id: 2, project: "KnowledgeTracker", tags: "ui", isFavorite: false),
                Entry(id: 3, project: "Other", tags: "ui", isFavorite: true),
            };

            var result = EntryListFilter.Apply(
                source,
                "title",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ui" },
                favoritesOnly: true,
                EntrySortMode.DateResolvedDescending,
                projectName: " knowledgetracker ");

            Assert.Equal(new[] { 1 }, result.Select(entry => entry.Id));
        }

        [Fact]
        public void Apply_CombinesSearchFavoritesAndTags()
        {
            var source = new[]
            {
                Entry(id: 1, title: "Syncfusion grid crash", tags: "ui", isFavorite: true),
                Entry(id: 2, title: "Syncfusion grid crash", tags: "ui", isFavorite: false),
                Entry(id: 3, title: "Syncfusion grid crash", tags: "db", isFavorite: true),
                Entry(id: 4, title: "Other thing", tags: "ui", isFavorite: true),
            };

            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ui" };

            var result = EntryListFilter.Apply(
                source, "syncfusion", selected, favoritesOnly: true,
                EntrySortMode.DateResolvedDescending);

            var single = Assert.Single(result);
            Assert.Equal(1, single.Id);
        }

        [Fact]
        public void Apply_DateResolvedDescending_PutsNewestFirst_AndNullsLast()
        {
            var source = new[]
            {
                Entry(id: 1, dateResolved: new DateTime(2025, 6, 1)),
                Entry(id: 2, dateResolved: new DateTime(2026, 1, 15)),
                Entry(id: 3, dateResolved: null),
            };

            var result = EntryListFilter.Apply(
                source, null, new HashSet<string>(), false,
                EntrySortMode.DateResolvedDescending);

            Assert.Equal(new[] { 2, 1, 3 }, result.Select(e => e.Id));
        }

        [Fact]
        public void Apply_DateResolvedAscending_KeepsNullsLast()
        {
            var source = new[]
            {
                Entry(id: 1, dateResolved: new DateTime(2025, 6, 1)),
                Entry(id: 2, dateResolved: new DateTime(2026, 1, 15)),
                Entry(id: 3, dateResolved: null),
            };

            var result = EntryListFilter.Apply(
                source, null, new HashSet<string>(), false,
                EntrySortMode.DateResolvedAscending);

            Assert.Equal(new[] { 1, 2, 3 }, result.Select(e => e.Id));
        }

        [Fact]
        public void Apply_TitleSorting_IgnoresCase()
        {
            var source = new[]
            {
                Entry(id: 1, title: "beta"),
                Entry(id: 2, title: "Alpha"),
                Entry(id: 3, title: "gamma"),
            };

            var ascending = EntryListFilter.Apply(
                source, null, new HashSet<string>(), false, EntrySortMode.TitleAscending);
            Assert.Equal(new[] { 2, 1, 3 }, ascending.Select(e => e.Id));

            var descending = EntryListFilter.Apply(
                source, null, new HashSet<string>(), false, EntrySortMode.TitleDescending);
            Assert.Equal(new[] { 3, 1, 2 }, descending.Select(e => e.Id));
        }

        [Fact]
        public void Apply_ProjectAscending_OrdersByProject()
        {
            var source = new[]
            {
                Entry(id: 1, project: "Zeta"),
                Entry(id: 2, project: "Alpha"),
                Entry(id: 3, project: null),
            };

            var result = EntryListFilter.Apply(
                source, null, new HashSet<string>(), false, EntrySortMode.ProjectAscending);

            Assert.Equal(new[] { 3, 2, 1 }, result.Select(e => e.Id));
        }

        [Fact]
        public void CollectTags_ReturnsUnion_DistinctIgnoringCase_Ordered()
        {
            var source = new[]
            {
                Entry(id: 1, tags: "WinUI 3; UIA; bug"),
                Entry(id: 2, tags: "qa,Bug"),
                Entry(id: 3, tags: null),
            };

            var tags = EntryListFilter.CollectTags(source);

            Assert.Equal(new[] { "bug", "qa", "UIA", "WinUI 3" }, tags);
        }

        [Fact]
        public void CollectTags_EmptySource_ReturnsEmpty()
        {
            Assert.Empty(EntryListFilter.CollectTags(Array.Empty<KnowledgeEntry>()));
        }

        [Fact]
        public void MatchesSearch_FindsTextInResolutionSteps_CaseInsensitive()
        {
            var entry = Entry(id: 1, title: "Bug");
            entry.ResolutionSteps = "Restarted the service successfully.";

            Assert.True(EntryListFilter.MatchesSearch(entry, "service"));
            Assert.True(EntryListFilter.MatchesSearch(entry, "SUCCESSFULLY"));
            Assert.False(EntryListFilter.MatchesSearch(entry, "network"));
        }

        [Fact]
        public void MatchesSearch_FindsTextInStepDescriptions_CaseInsensitive()
        {
            var entry = Entry(id: 1, title: "Issue");
            entry.Steps.Add(new Step { Id = 1, KnowledgeEntryId = 1, Description = "Check firewall rules" });
            entry.Steps.Add(new Step { Id = 2, KnowledgeEntryId = 1, Description = "Test connection" });

            Assert.True(EntryListFilter.MatchesSearch(entry, "firewall"));
            Assert.True(EntryListFilter.MatchesSearch(entry, "CONNECTION"));
            Assert.False(EntryListFilter.MatchesSearch(entry, "dns"));
        }

        [Fact]
        public void MatchesSearch_FindsTextInAttachmentFileNames_CaseInsensitive()
        {
            var entry = Entry(id: 1, title: "Log");
            entry.Attachments.Add(new AttachmentInfo { Id = 1, KnowledgeEntryId = 1, FileName = "error.log" });
            entry.Attachments.Add(new AttachmentInfo { Id = 2, KnowledgeEntryId = 1, FileName = "StackTrace.txt" });

            Assert.True(EntryListFilter.MatchesSearch(entry, "log"));
            Assert.True(EntryListFilter.MatchesSearch(entry, "STACK"));
            Assert.False(EntryListFilter.MatchesSearch(entry, "report"));
        }

        [Fact]
        public void MatchesSearch_NullStepsOrAttachments_DoesNotThrow()
        {
            var entry = Entry(id: 1, title: "matchable title");
            entry.Steps = null!;
            entry.Attachments = null!;

            Assert.True(EntryListFilter.MatchesSearch(entry, "matchable"));
            Assert.False(EntryListFilter.MatchesSearch(entry, "nothing"));
        }

        [Fact]
        public void Apply_SearchCombinesWithProjectTagsAndFavorites()
        {
            var source = new[]
            {
                Entry(id: 1, title: "Auth", project: "App", tags: "auth;security", isFavorite: true, description: "OAuth flow"),
                Entry(id: 2, title: "Auth", project: "App", tags: "auth", isFavorite: false, description: "OAuth flow"),
                Entry(id: 3, title: "Auth", project: "Web", tags: "auth;security", isFavorite: true, description: "OAuth flow"),
                Entry(id: 4, title: "Other", project: "App", tags: "auth;security", isFavorite: true, description: "Unrelated"),
            };

            var selectedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "auth", "security" };
            var result = EntryListFilter.Apply(
                source,
                "oauth",
                selectedTags,
                favoritesOnly: true,
                EntrySortMode.TitleAscending,
                projectName: "App");

            Assert.Equal(new[] { 1 }, result.Select(e => e.Id));
        }

        [Fact]
        public void Apply_SearchOnStepOrAttachmentFields_DoesNotReturnFalsePositives()
        {
            var entry = Entry(id: 1, title: "Test");
            entry.Steps.Add(new Step { Description = "step one" });
            entry.Attachments.Add(new AttachmentInfo { FileName = "file.txt" });
            entry.ResolutionSteps = "fixed";

            var result = EntryListFilter.Apply(
                new[] { entry },
                "missing",
                new HashSet<string>(),
                favoritesOnly: false,
                EntrySortMode.TitleAscending);

            Assert.Empty(result);
        }

        [Fact]
        public void Apply_SearchInStepDescription_RespectsOtherFilters()
        {
            var source = new[]
            {
                Entry(id: 1, project: "App", tags: "bug", title: "Alpha"),
                Entry(id: 2, project: "Web", tags: "bug", title: "Beta"),
            };
            source[0].Steps.Add(new Step { Description = "restarted iis apppool" });
            source[1].Steps.Add(new Step { Description = "restarted iis apppool" });

            var result = EntryListFilter.Apply(
                source,
                "apppool",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bug" },
                favoritesOnly: false,
                EntrySortMode.TitleAscending,
                projectName: "App");

            Assert.Equal(new[] { 1 }, result.Select(e => e.Id));
        }

        [Fact]
        public void Apply_SearchWithSort_PreservesSortOrder()
        {
            var source = new[]
            {
                Entry(id: 1, title: "Alpha", description: "needle", dateResolved: new DateTime(2020, 1, 1)),
                Entry(id: 2, title: "Beta", description: "needle", dateResolved: new DateTime(2021, 1, 1)),
                Entry(id: 3, title: "Gamma", description: "other", dateResolved: new DateTime(2022, 1, 1)),
            };

            var descending = EntryListFilter.Apply(
                source, "needle", new HashSet<string>(), favoritesOnly: false,
                EntrySortMode.DateResolvedDescending);
            Assert.Equal(new[] { 2, 1 }, descending.Select(e => e.Id));

            var ascending = EntryListFilter.Apply(
                source, "needle", new HashSet<string>(), favoritesOnly: false,
                EntrySortMode.DateResolvedAscending);
            Assert.Equal(new[] { 1, 2 }, ascending.Select(e => e.Id));
        }
    }
}
