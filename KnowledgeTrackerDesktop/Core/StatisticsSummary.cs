namespace KnowledgeTrackerDesktop.Core
{
    public sealed record MonthCount(int Year, int Month, int Count);

    public sealed record NameCount(string Name, int Count);

    /// <summary>
    /// Aggregated view of the knowledge base, produced by StatisticsService.
    /// </summary>
    public sealed class StatisticsSummary
    {
        public int TotalEntries { get; init; }

        public int FavoriteEntries { get; init; }

        public int TotalProjects { get; init; }

        public int TotalTags { get; init; }

        public IReadOnlyList<MonthCount> EntriesByMonth { get; init; } =
            Array.Empty<MonthCount>();

        public IReadOnlyList<NameCount> TopProjects { get; init; } =
            Array.Empty<NameCount>();

        public IReadOnlyList<NameCount> TopTags { get; init; } =
            Array.Empty<NameCount>();
    }
}
