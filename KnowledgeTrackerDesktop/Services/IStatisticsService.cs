using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// Application-level statistics over the whole knowledge base.
    /// </summary>
    public interface IStatisticsService
    {
        Task<StatisticsSummary> GetStatisticsAsync();
    }
}
