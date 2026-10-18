using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Phase 3 behaviour of the library: selecting a card previews it in the
    /// right pane without leaving the list, and a selection that disappears
    /// from the filter is not kept alive. The tests drive the card the way the
    /// UI does (mouse message handlers); opening a modal dialog or an external
    /// application is outside what an automated run can assert and is covered
    /// by the manual checklist.
    /// </summary>
    public class LibraryPreviewSelectionTests
    {
        [Fact]
        public async Task PreviewStartsEmptyAndShowsTheEmptyState()
        {
            var navigation = new FakeNavigation();
            using var view = CreateView(navigation, Entry(1, "Alpha"));

            await view.OnAppearingAsync();

            Assert.True(Preview(view).IsEmpty);
            Assert.Contains(
                Descendants(Preview(view)).OfType<Label>(),
                label => label.Text == AppLocalization.Translate("Select an entry to preview it."));
            Assert.Empty(navigation.Calls);
        }

        [Fact]
        public async Task SelectingACardPreviewsItWithoutNavigating()
        {
            var navigation = new FakeNavigation();
            using var view = CreateView(
                navigation,
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            var card = Cards(view).Single(card => card.Entry.Id == 1);
            InvokeMouseDown(card, MouseButtons.Left);

            Assert.Same(card.Entry, Preview(view).CurrentEntry);
            Assert.Equal(1, SelectedId(view));
            Assert.True(card.IsSelected);
            Assert.False(Cards(view).Single(card => card.Entry.Id == 2).IsSelected);
            Assert.Empty(navigation.Calls);
        }

        [Fact]
        public async Task SelectingAnotherCardMovesThePreviewAndTheHighlight()
        {
            var navigation = new FakeNavigation();
            using var view = CreateView(
                navigation,
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            var first = Cards(view).Single(card => card.Entry.Id == 1);
            var second = Cards(view).Single(card => card.Entry.Id == 2);
            InvokeMouseDown(first, MouseButtons.Left);
            InvokeMouseDown(second, MouseButtons.Left);

            Assert.Same(second.Entry, Preview(view).CurrentEntry);
            Assert.Equal(2, SelectedId(view));
            Assert.False(first.IsSelected);
            Assert.True(second.IsSelected);
            Assert.Empty(navigation.Calls);
        }

        [Fact]
        public async Task DoubleClickStillOpensTheSelectedEntryInTheEditor()
        {
            var navigation = new FakeNavigation();
            using var view = CreateView(navigation, Entry(1, "Alpha entry"));
            await view.OnAppearingAsync();

            var card = Cards(view).Single();
            InvokeMouseDown(card, MouseButtons.Left, clicks: 2);

            var call = Assert.Single(navigation.Calls);
            Assert.Equal(typeof(ctlAddEdit), call.View);
            var request = Assert.IsType<EntryEditorRequest>(Assert.Single(call.Args));
            Assert.Same(card.Entry, request.Entry);
        }

        [Fact]
        public async Task EditIntentFromThePreviewOpensTheEditorForTheSameEntry()
        {
            var navigation = new FakeNavigation();
            using var view = CreateView(navigation, Entry(1, "Alpha entry"));
            await view.OnAppearingAsync();

            var card = Cards(view).Single();
            InvokeMouseDown(card, MouseButtons.Left);
            Preview(view).RequestEditEntry();

            var call = Assert.Single(navigation.Calls);
            Assert.Equal(typeof(ctlAddEdit), call.View);
            var request = Assert.IsType<EntryEditorRequest>(Assert.Single(call.Args));
            Assert.Same(card.Entry, request.Entry);
            Assert.Equal(1, SelectedId(view));
        }

        [Fact]
        public async Task EntryFilteredOutOfTheListIsNoLongerSelected()
        {
            var navigation = new FakeNavigation();
            using var view = CreateView(
                navigation,
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            InvokeMouseDown(Cards(view).Single(card => card.Entry.Id == 1), MouseButtons.Left);
            Assert.False(Preview(view).IsEmpty);

            Search(view).Text = "Beta";
            FireDebounce(view);

            Assert.True(Preview(view).IsEmpty);
            Assert.Null(SelectedId(view));
            Assert.All(Cards(view), card => Assert.False(card.IsSelected));

            // Restoring the filter does not resurrect a stale selection.
            Search(view).Text = "Alpha";
            FireDebounce(view);

            Assert.True(Preview(view).IsEmpty);
            Assert.Null(SelectedId(view));
        }

        [Fact]
        public async Task ReloadingAnEntryThatIsGoneClearsThePreview()
        {
            var navigation = new FakeNavigation();
            var service = new FakeEntryService(Entry(1, "Alpha entry"));
            using var view = new ctlMain(service, navigation, new FakeShellHost(), null);
            await view.OnAppearingAsync();

            InvokeMouseDown(Cards(view).Single(), MouseButtons.Left);
            Assert.False(Preview(view).IsEmpty);

            service.SetEntries(Entry(2, "Only survivor"));
            await view.OnAppearingAsync();

            Assert.True(Preview(view).IsEmpty);
            Assert.Null(SelectedId(view));
            Assert.Equal(2, Cards(view).Single().Entry.Id);
        }

        [Fact]
        public async Task SplitLayoutKeepsBothPanesUsableAndHonoursTheUsersDrag()
        {
            var navigation = new FakeNavigation();
            using var view = CreateView(
                navigation,
                Entry(1, "Alpha entry"),
                Entry(2, "Beta entry"));
            await view.OnAppearingAsync();

            view.Size = new Size(1200, 800);
            view.PerformLayout();

            var split = Split(view);
            Assert.Equal(340, split.Panel1MinSize);
            Assert.Equal(380, split.Panel2MinSize);
            Assert.InRange(
                split.SplitterDistance,
                split.Panel1MinSize,
                split.Width - split.Panel2MinSize - split.SplitterWidth);

            Assert.Same(split.Panel2, Preview(view).Parent);
            Assert.All(Cards(view), card => Assert.True(IsInside(split.Panel1, card)));

            // A drag by the user is the new position, not a value the view keeps
            // resetting: the preview keeps its width while the window resizes.
            split.SplitterDistance = 500;
            var previewWidth = split.Panel2.Width;

            view.Size = new Size(1400, 800);
            view.PerformLayout();

            Assert.InRange(split.Panel2.Width, previewWidth - 2, previewWidth + 2);
            Assert.Equal(340, split.Panel1MinSize);
            Assert.Equal(380, split.Panel2MinSize);

            // Narrower than the minimum sizes: neither pane may collapse.
            view.Size = new Size(760, 600);
            view.PerformLayout();

            Assert.True(split.Panel1.Width >= split.Panel1MinSize - 2);
            Assert.True(split.Panel2.Width >= split.Panel2MinSize - 2);
        }

        private static ctlMain CreateView(FakeNavigation navigation, params KnowledgeEntry[] entries) =>
            new(
                new FakeEntryService(entries),
                navigation,
                new FakeShellHost(),
                null);

        private static KnowledgeEntry Entry(int id, string title) =>
            new() { Id = id, Title = title };

        private static ctlEntryPreview Preview(ctlMain view) =>
            Field<ctlEntryPreview>(view, "_preview");

        private static SplitContainer Split(ctlMain view) =>
            Field<SplitContainer>(view, "_split");

        private static int? SelectedId(ctlMain view) =>
            Field<int?>(view, "_selectedEntryId");

        private static EntryCard[] Cards(ctlMain view) =>
            Descendants(view).OfType<EntryCard>().ToArray();

        private static Control Search(ctlMain view) =>
            Field<Control>(view, "_search");

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

        private static void InvokeMouseDown(Control control, MouseButtons button, int clicks = 1)
        {
            var method = typeof(Control).GetMethod(
                "OnMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { new MouseEventArgs(button, clicks, 2, 2, 0) });
        }

        private static bool IsInside(Control ancestor, Control control)
        {
            for (var current = control; current is not null; current = current.Parent)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

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
            private IReadOnlyList<KnowledgeEntry> _entries;

            public FakeEntryService(params KnowledgeEntry[] entries) => _entries = entries;

            public void SetEntries(params KnowledgeEntry[] entries) => _entries = entries;

            public Task<IReadOnlyList<KnowledgeEntry>> GetAllAsync() => Task.FromResult(_entries);

            public Task<KnowledgeEntry?> GetByIdAsync(int id) =>
                Task.FromResult(_entries.FirstOrDefault(entry => entry.Id == id));

            public Task SaveAsync(KnowledgeEntry entry) => Task.CompletedTask;

            public Task DeleteAsync(int id) => Task.CompletedTask;

            public Task ToggleFavoriteAsync(KnowledgeEntry entry) => Task.CompletedTask;
        }

        private sealed class FakeNavigation : INavigationService
        {
            public List<(Type View, object?[] Args)> Calls { get; } = new();

            public Control? CurrentView => null;

            public bool CanGoBack => false;

            public event EventHandler? Navigated
            {
                add { }
                remove { }
            }

            public void NavigateTo<TView>(params object[] args) where TView : Control =>
                Calls.Add((typeof(TView), args));

            public void NavigateToPrimary<TView>(params object[] args) where TView : Control =>
                Calls.Add((typeof(TView), args));

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
