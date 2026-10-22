using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;

namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// Aggregates statistics in memory after loading the entries
    /// (ARCHITECTURE.md section 16: do not optimize prematurely).
    /// </summary>
    public sealed class StatisticsService : IStatisticsService
    {
        private const int ActivityMonths = 12;
        private const int TopListSize = 5;

        private readonly IKnowledgeRepository _repository;

        public StatisticsService(IKnowledgeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<StatisticsSummary> GetStatisticsAsync()
        {
            var entries = await _repository.GetAllAsync();
            return Compute(entries);
        }

        public static StatisticsSummary Compute(
            IReadOnlyList<KnowledgeEntry> entries,
            DateTime? asOf = null)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var referenceDate = asOf ?? DateTime.Today;
            var currentMonth = new DateTime(referenceDate.Year, referenceDate.Month, 1);
            var firstMonth = currentMonth.AddMonths(-(ActivityMonths - 1));

            return new StatisticsSummary
            {
                TotalEntries = entries.Count,
                FavoriteEntries = entries.Count(e => e.IsFavorite),
                TotalProjects = GetDistinctProjectCount(entries),
                TotalTags = GetDistinctTagCount(entries),
                EntriesByMonth = ComputeEntriesByMonth(entries, firstMonth, currentMonth),
                TopProjects = ComputeTopNames(
                    entries
                        .Where(e => !string.IsNullOrWhiteSpace(e.ProjectName))
                        .Select(e => e.ProjectName!.Trim()),
                    TopListSize),
                TopTags = ComputeTopNames(
                    entries
                        .SelectMany(e => KnowledgeEntryTags.Split(e.Tags)
                            .Distinct(StringComparer.OrdinalIgnoreCase)),
                    TopListSize),
                EntriesByOrigin = ComputeEntriesByOrigin(entries),
                EntriesWithoutOrigin = entries.Count(entry => entry.ProjectOriginKind is null),
            };
        }

        /// <summary>
        /// How many entries recorded each origin kind. Every kind is listed,
        /// including the ones nobody used yet, so the breakdown does not
        /// change shape as the library grows.
        /// </summary>
        private static IReadOnlyList<OriginCount> ComputeEntriesByOrigin(
            IReadOnlyList<KnowledgeEntry> entries)
        {
            var counts = entries
                .Where(entry => entry.ProjectOriginKind.HasValue)
                .GroupBy(entry => entry.ProjectOriginKind!.Value)
                .ToDictionary(group => group.Key, group => group.Count());

            return ProjectOriginRules.Kinds
                .Select(kind => new OriginCount(
                    kind,
                    counts.TryGetValue(kind, out var count) ? count : 0))
                .ToList();
        }

        private static int GetDistinctProjectCount(IReadOnlyList<KnowledgeEntry> entries) =>
            entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.ProjectName))
                .Select(entry => entry.ProjectName!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

        private static int GetDistinctTagCount(IReadOnlyList<KnowledgeEntry> entries) =>
            entries
                .SelectMany(entry => KnowledgeEntryTags.Split(entry.Tags))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

        private static IReadOnlyList<MonthCount> ComputeEntriesByMonth(
            IReadOnlyList<KnowledgeEntry> entries,
            DateTime firstMonth,
            DateTime currentMonth)
        {
            var counts = entries
                .Where(entry => entry.DateResolved.HasValue)
                .Select(entry => entry.DateResolved!.Value)
                .Where(date => date >= firstMonth && date < currentMonth.AddMonths(1))
                .GroupBy(date => new { date.Year, date.Month })
                .ToDictionary(group => (group.Key.Year, group.Key.Month), group => group.Count());

            return Enumerable.Range(0, ActivityMonths)
                .Select(offset =>
                {
                    var month = firstMonth.AddMonths(offset);
                    counts.TryGetValue((month.Year, month.Month), out var count);
                    return new MonthCount(month.Year, month.Month, count);
                })
                .ToList();
        }

        private static IReadOnlyList<NameCount> ComputeTopNames(
            IEnumerable<string> names,
            int count)
        {
            return names
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(group => new NameCount(group.First(), group.Count()))
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Take(count)
                .ToList();
        }
    }
}
