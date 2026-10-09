using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Theme;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.Windows.Forms.Tools;
using System.Reflection;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    public class LibraryCardVisibilityTests
    {
        [Fact]
        public async Task AppearingWithEntriesCreatesVisibleCards()
        {
            var first = new KnowledgeEntry
            {
                Id = 1,
                Title = "First visible entry",
                Description = "First description",
            };
            var second = new KnowledgeEntry
            {
                Id = 2,
                Title = "Second visible entry",
                Description = "Second description",
            };
            using var view = new ctlMain(
                new FakeEntryService(first, second),
                new FakeNavigation(),
                new FakeShellHost(),
                null);

            await view.OnAppearingAsync();

            var cards = Descendants(view).OfType<EntryCard>().ToArray();
            Assert.Equal(2, cards.Length);
            Assert.All(cards, card => Assert.True(card.Visible));
            Assert.All(cards, card => Assert.Contains(
                Descendants(card).OfType<Label>(),
                label => label.Text == card.Entry.Title));
        }

        [Fact]
        public async Task TagFilterOptionsUseDarkThemeTextAndBackground()
        {
            var entry = new KnowledgeEntry
            {
                Id = 1,
                Title = "Tagged entry",
                Tags = "C#, WinForms",
            };
            using var view = new ctlMain(
                new FakeEntryService(entry),
                new FakeNavigation(),
                new FakeShellHost(),
                null);

            await view.OnAppearingAsync();

            var tagsPanel = (FlowLayoutPanel)typeof(ctlMain)
                .GetField("_tagsPanel", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;
            var options = tagsPanel.Controls.OfType<CheckBoxAdv>().ToArray();
            Assert.Equal(2, options.Length);
            Assert.All(options, option =>
            {
                Assert.Equal(CarbonPalette.TextPrimary, option.ForeColor);
                Assert.Equal(CarbonPalette.Surface, option.BackColor);
            });
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
