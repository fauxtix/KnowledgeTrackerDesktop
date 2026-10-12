using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Theme;
using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Functional tests for the Statistics view: composing and skinning it must
    /// not throw, independently of any running window.
    /// </summary>
    public class StatisticsViewTests
    {
        private sealed class FakeStatisticsService : IStatisticsService
        {
            private readonly StatisticsSummary _summary;

            public FakeStatisticsService(StatisticsSummary summary) => _summary = summary;

            public Task<StatisticsSummary> GetStatisticsAsync() => Task.FromResult(_summary);
        }

        private sealed class FakeNavigationService : INavigationService
        {
            public Control? CurrentView => null;

            public bool CanGoBack => false;

            public event EventHandler? Navigated;

            public void NavigateTo<TView>(params object[] args) where TView : Control
            {
            }

            public void NavigateToPrimary<TView>(params object[] args) where TView : Control
            {
            }

            public bool GoBack() => false;

            private void Raise() => Navigated?.Invoke(this, EventArgs.Empty);
        }

        private sealed class FakeShellHost : IShellHost
        {
            public void SetPageTitle(string title)
            {
            }

            public void SetHeaderActions(params Control[] actions)
            {
            }

            public void ClearHeaderActions()
            {
            }
        }

        private static StatisticsSummary SampleSummary() => new()
        {
            TotalEntries = 5,
            FavoriteEntries = 2,
            TotalProjects = 2,
            TotalTags = 3,
            EntriesByMonth = new[]
            {
                new MonthCount(2026, 1, 2),
                new MonthCount(2026, 2, 1),
            },
            TopProjects = new[] { new NameCount("Alpha", 3) },
            TopTags = new[] { new NameCount("sqlite", 2) },
        };

        [Fact]
        public async Task BuildThemeAndRender_DoesNotThrow()
        {
            using var view = new ctlStatistics(
                new FakeStatisticsService(SampleSummary()),
                new FakeNavigationService(),
                new FakeShellHost());

            CarbonTheme.Apply(view);
            await view.OnAppearingAsync();
        }
    }
}
