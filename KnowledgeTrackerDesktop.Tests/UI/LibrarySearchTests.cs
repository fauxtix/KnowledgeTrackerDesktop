using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// UX behaviour of the Library search/filter bar added in phase 1: search
    /// debounce, result counter and the "clear filters" recovery action.
    /// The debounce is exercised through the control the view owns (no added
    /// abstraction): typing must not rebuild immediately, but the pending
    /// rebuild must reflect the latest text.
    /// </summary>
    public class LibrarySearchTests
    {
        [Fact]
        public async Task TypingDefersRebuildUntilTheDebounceElapses()
        {
            using var view = CreateView(
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            Assert.Equal(2, Cards(view).Length);

            Search(view).Text = "Alpha";

            // The card list is only rebuilt when the debounce fires.
            Assert.Equal(2, Cards(view).Length);
            Assert.True(Debounce(view).Enabled);

            FireDebounce(view);

            Assert.Single(Cards(view));
            Assert.Equal("Alpha entry", Cards(view)[0].Entry.Title);
            Assert.False(Debounce(view).Enabled);
        }

        [Fact]
        public async Task TypingMoreResetsTheDebounceAndKeepsTheLatestText()
        {
            using var view = CreateView(
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            Search(view).Text = "Alpha";
            FireDebounce(view);
            Assert.Single(Cards(view));

            // A new keystroke schedules another rebuild that must read the new text.
            Search(view).Text = "Beta";
            Assert.True(Debounce(view).Enabled);
            FireDebounce(view);

            Assert.Single(Cards(view));
            Assert.Equal("Beta entry", Cards(view)[0].Entry.Title);
        }

        [Fact]
        public async Task NonSearchFilterChangesRebuildImmediatelyWithTheLatestSearchText()
        {
            using var view = CreateView(
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            Search(view).Text = "Alpha";
            Assert.True(Debounce(view).Enabled);

            // Sorting is not typing: it must not wait for the debounce and must
            // apply the search text that is currently in the box.
            Sort(view).SelectedIndex = 1;

            Assert.Single(Cards(view));
            Assert.Equal("Alpha entry", Cards(view)[0].Entry.Title);
            Assert.False(Debounce(view).Enabled);
        }

        [Fact]
        public async Task CounterShowsTotalWhenNoFiltersAreActive()
        {
            using var view = CreateView(
                Entry(1, "Alpha"),
                Entry(2, "Beta"));
            await view.OnAppearingAsync();

            Assert.Equal("2 entries", CountLabel(view).Text);
        }

        [Fact]
        public async Task CounterShowsFilteredOfTotalWhenFiltersAreActive()
        {
            using var view = CreateView(
                Entry(1, "Alpha"),
                Entry(2, "Beta"),
                Entry(3, "Gamma"));
            await view.OnAppearingAsync();

            Search(view).Text = "Alpha";
            FireDebounce(view);

            Assert.Equal("1 of 3 entries", CountLabel(view).Text);
        }

        [Fact]
        public async Task CounterUsesSingularForASingleEntryWithoutFilters()
        {
            using var view = CreateView(Entry(1, "Only entry"));
            await view.OnAppearingAsync();

            Assert.Equal("1 entry", CountLabel(view).Text);
        }

        [Fact]
        public async Task NoMatchesShowsClearFiltersWhichRestoresTheList()
        {
            using var view = CreateView(
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            Search(view).Text = "does not exist";
            FireDebounce(view);

            Assert.Empty(Cards(view));
            Assert.True(ClearFiltersButton(view).Visible);
            Assert.Contains("No entries match your search", StateLabel(view).Text);

            InvokePrivate(view, "ClearFilters");

            Assert.Equal(string.Empty, Search(view).Text);
            Assert.Equal(2, Cards(view).Length);
            Assert.False(ClearFiltersButton(view).Visible);
            Assert.Equal("2 entries", CountLabel(view).Text);
        }

        [Fact]
        public async Task EmptyLibraryShowsItsOwnMessageWithoutClearFilters()
        {
            using var view = CreateView();
            await view.OnAppearingAsync();

            Assert.Empty(Cards(view));
            Assert.False(ClearFiltersButton(view).Visible);
            Assert.Contains("No knowledge entries yet", StateLabel(view).Text);
        }

        [Fact]
        public async Task RebuildingTheListDisposesTheReplacedCards()
        {
            using var view = CreateView(Entry(1, "Alpha"), Entry(2, "Beta"));
            await view.OnAppearingAsync();

            var replaced = Cards(view);

            Search(view).Text = "Alpha";
            FireDebounce(view);

            Assert.All(replaced, card => Assert.True(card.IsDisposed));
        }

        private static ctlMain CreateView(params KnowledgeEntry[] entries) =>
            new(
                new FakeEntryService(entries),
                new FakeNavigation(),
                new FakeShellHost(),
                null);

        private static KnowledgeEntry Entry(int id, string title) =>
            new() { Id = id, Title = title };

        private static EntryCard[] Cards(ctlMain view) =>
            Descendants(view).OfType<EntryCard>().ToArray();

        private static Control Search(ctlMain view) => Field<Control>(view, "_search");

        private static Syncfusion.Windows.Forms.Tools.ComboBoxAdv Sort(ctlMain view) =>
            Field<Syncfusion.Windows.Forms.Tools.ComboBoxAdv>(view, "_sort");

        private static Label CountLabel(ctlMain view) => Field<Label>(view, "_countLabel");

        private static Label StateLabel(ctlMain view) => Field<Label>(view, "_stateLabel");

        private static Control ClearFiltersButton(ctlMain view) =>
            Field<Control>(view, "_clearFiltersButton");

        private static System.Windows.Forms.Timer Debounce(ctlMain view) =>
            Field<System.Windows.Forms.Timer>(view, "_searchDebounce");

        private static void FireDebounce(ctlMain view) =>
            InvokePrivate(view, "OnSearchDebounceTick", null, EventArgs.Empty);

        private static T Field<T>(ctlMain view, string name) =>
            (T)typeof(ctlMain)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;

        private static void InvokePrivate(ctlMain view, string method, params object?[] args) =>
            typeof(ctlMain)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(view, args);

        private static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private sealed class FakeEntryService : IKnowledgeEntryService
        {
            private readonly IReadOnlyList<KnowledgeEntry> _entries;

            public FakeEntryService(params KnowledgeEntry[] entries) => _entries = entries;

            public Task<IReadOnlyList<KnowledgeEntry>> GetAllAsync() => Task.FromResult(_entries);

            public Task<KnowledgeEntry?> GetByIdAsync(int id) =>
                Task.FromResult(_entries.FirstOrDefault(entry => entry.Id == id));

            public Task SaveAsync(KnowledgeEntry entry) => Task.CompletedTask;

            public Task DeleteAsync(int id) => Task.CompletedTask;

            public Task ToggleFavoriteAsync(KnowledgeEntry entry) => Task.CompletedTask;
        }

        private sealed class FakeNavigation : INavigationService
        {
            public Control? CurrentView => null;

            public bool CanGoBack => false;

            public event EventHandler? Navigated
            {
                add { }
                remove { }
            }

            public void NavigateTo<TView>(params object[] args) where TView : Control { }

            public void NavigateToPrimary<TView>(params object[] args) where TView : Control { }

            public bool GoBack() => false;
        }

        private sealed class FakeShellHost : IShellHost
        {
            public void SetPageTitle(string title) { }

            public void SetHeaderActions(params Control[] actions) { }

            public void ClearHeaderActions() { }
        }
    }
}
