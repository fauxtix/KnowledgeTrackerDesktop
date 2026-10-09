using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.Tests.Data;
using SQLite;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Services
{
    public class StatisticsServiceTests
    {
        [Fact]
        public void Compute_CountsTotalsAndFavorites()
        {
            var entries = new List<KnowledgeEntry>
            {
                new() { Title = "a", IsFavorite = true },
                new() { Title = "b" },
                new() { Title = "c", IsFavorite = true },
            };

            var summary = StatisticsService.Compute(entries);

            Assert.Equal(3, summary.TotalEntries);
            Assert.Equal(2, summary.FavoriteEntries);
        }

        [Fact]
        public void Compute_CountsDistinctProjectsAndTagsAcrossAllEntries()
        {
            var entries = new List<KnowledgeEntry>
            {
                new() { Title = "a", ProjectName = " Alpha ", Tags = "sqlite;ui" },
                new() { Title = "b", ProjectName = "alpha", Tags = "UI, markdown" },
                new() { Title = "c", ProjectName = "Beta", Tags = "SQLite" },
                new() { Title = "d", ProjectName = " ", Tags = " ; " },
            };

            var summary = StatisticsService.Compute(entries);

            Assert.Equal(2, summary.TotalProjects);
            Assert.Equal(3, summary.TotalTags);
        }

        [Fact]
        public void Compute_ReturnsLastTwelveMonthsIncludingEmptyMonths()
        {
            var entries = new List<KnowledgeEntry>
            {
                new() { Title = "a", DateResolved = new DateTime(2026, 3, 10) },
                new() { Title = "b", DateResolved = new DateTime(2026, 1, 2) },
                new() { Title = "c", DateResolved = new DateTime(2025, 12, 30) },
                new() { Title = "d", DateResolved = new DateTime(2026, 1, 15) },
                new() { Title = "e" }, // no date: counted in total, not in timeline
            };

            var summary = StatisticsService.Compute(entries, new DateTime(2026, 10, 8));

            Assert.Equal(5, summary.TotalEntries);
            Assert.Equal(12, summary.EntriesByMonth.Count);
            Assert.Equal(
                new[]
                {
                    (2025, 11, 0), (2025, 12, 1),
                    (2026, 1, 2), (2026, 2, 0), (2026, 3, 1),
                    (2026, 4, 0), (2026, 5, 0), (2026, 6, 0),
                    (2026, 7, 0), (2026, 8, 0), (2026, 9, 0), (2026, 10, 0),
                },
                summary.EntriesByMonth
                    .Select(m => (m.Year, m.Month, m.Count))
                    .ToArray());
        }

        [Fact]
        public void Compute_ExcludesResolvedDatesOutsideTheActivityWindow()
        {
            var entries = new List<KnowledgeEntry>
            {
                new() { Title = "old", DateResolved = new DateTime(2025, 10, 31) },
                new() { Title = "first included", DateResolved = new DateTime(2025, 11, 1) },
                new() { Title = "current month", DateResolved = new DateTime(2026, 10, 8) },
                new() { Title = "future", DateResolved = new DateTime(2026, 11, 1) },
            };

            var summary = StatisticsService.Compute(entries, new DateTime(2026, 10, 8));

            Assert.Equal(4, summary.TotalEntries);
            Assert.Equal(1, summary.EntriesByMonth[0].Count);
            Assert.Equal(1, summary.EntriesByMonth[^1].Count);
            Assert.Equal(2, summary.EntriesByMonth.Sum(month => month.Count));
        }

        [Fact]
        public void Compute_TopProjects_SortedByCountThenName()
        {
            var entries = new List<KnowledgeEntry>
            {
                new() { Title = "a", ProjectName = "Alpha" },
                new() { Title = "b", ProjectName = "Beta" },
                new() { Title = "c", ProjectName = "Alpha" },
                new() { Title = "d" }, // no project: ignored
                new() { Title = "e", ProjectName = "   " }, // blank: ignored
            };

            var summary = StatisticsService.Compute(entries);

            Assert.Equal(
                new[] { ("Alpha", 2), ("Beta", 1) },
                summary.TopProjects.Select(p => (p.Name, p.Count)).ToArray());
        }

        [Fact]
        public void Compute_TopTags_SplitsLegacySeparatorsAndMergesCase()
        {
            var entries = new List<KnowledgeEntry>
            {
                new() { Title = "a", Tags = "WinUI 3; UIA; bug" },
                new() { Title = "b", Tags = "qa,Bug" },
                new() { Title = "c" },
            };

            var summary = StatisticsService.Compute(entries);

            Assert.Equal(
                new[] { ("bug", 2), ("qa", 1), ("UIA", 1), ("WinUI 3", 1) },
                summary.TopTags.Select(t => (t.Name, t.Count)).ToArray());
        }

        [Fact]
        public void Compute_TopLists_CountEachNameOncePerEntryAndReturnAtMostFive()
        {
            var entries = new List<KnowledgeEntry>();
            for (var index = 0; index < 7; index++)
            {
                entries.Add(new KnowledgeEntry
                {
                    Title = $"Entry {index}",
                    ProjectName = $"Project {index}",
                    Tags = $"tag{index}; TAG{index}",
                });
            }

            var summary = StatisticsService.Compute(entries, new DateTime(2026, 10, 8));

            Assert.Equal(5, summary.TopProjects.Count);
            Assert.Equal(5, summary.TopTags.Count);
            Assert.All(summary.TopTags, tag => Assert.Equal(1, tag.Count));
        }

        [Fact]
        public void Compute_EmptyList_ReturnsZeroSummary()
        {
            var summary = StatisticsService.Compute(new List<KnowledgeEntry>());

            Assert.Equal(0, summary.TotalEntries);
            Assert.Equal(0, summary.FavoriteEntries);
            Assert.Equal(0, summary.TotalProjects);
            Assert.Equal(0, summary.TotalTags);
            Assert.Equal(12, summary.EntriesByMonth.Count);
            Assert.All(summary.EntriesByMonth, month => Assert.Equal(0, month.Count));
            Assert.Empty(summary.TopProjects);
            Assert.Empty(summary.TopTags);
        }

        [Fact]
        public async Task GetStatisticsAsync_AggregatesRepositoryData()
        {
            using var db = new TempDatabase();
            var connection = db.OpenConnection();
            try
            {
                await new SchemaMigrator(db.DatabasePath).InitializeAsync();
                var service = new StatisticsService(new KnowledgeRepositorySQLite(connection));

                var entry = new KnowledgeEntry
                {
                    Title = "a",
                    IsFavorite = true,
                    ProjectName = "AppTracker",
                    Tags = "sqlite;winforms",
                    DateResolved = new DateTime(2026, 10, 8),
                };
                await new KnowledgeEntryService(new KnowledgeRepositorySQLite(connection))
                    .SaveAsync(entry);

                var summary = await service.GetStatisticsAsync();

                Assert.Equal(1, summary.TotalEntries);
                Assert.Equal(1, summary.FavoriteEntries);
                Assert.Equal(1, summary.TotalProjects);
                Assert.Equal(2, summary.TotalTags);
                Assert.Equal(12, summary.EntriesByMonth.Count);
                Assert.Equal(1, summary.EntriesByMonth.Single(m => m.Year == 2026 && m.Month == 10).Count);
                Assert.Equal(("AppTracker", 1), (summary.TopProjects[0].Name, summary.TopProjects[0].Count));
                Assert.Equal(2, summary.TopTags.Count);
            }
            finally
            {
                await connection.CloseAsync();
                db.Dispose();
            }
        }
    }
}
