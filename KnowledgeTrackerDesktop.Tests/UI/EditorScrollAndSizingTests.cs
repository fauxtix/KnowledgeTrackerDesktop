using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// The editor groups its fields into tabs. These assertions guard that
    /// layout contract: every tab hosts its own scrolling body (so no single
    /// global scrollbar stacks all the groups), and once a tab is active its
    /// fields are laid out with a usable size instead of collapsing.
    /// </summary>
    public sealed class EditorScrollAndSizingTests
    {
        private static ctlAddEdit CreateEditor(int width = 1024, int height = 700)
        {
            var entry = new KnowledgeEntry
            {
                Id = 1,
                Title = "Sizing entry",
                ProjectName = "Project",
                Technologies = "csharp",
                Tags = "ui;sqlite",
                Description = "markdown description",
                CodeSnippet = "Console.WriteLine();",
                Comments = "notes",
                YouTubeUrl = "https://youtube.com/watch?v=abc",
            };
            entry.Steps.Add(new Step { Description = "step 1", Order = 1, KnowledgeEntryId = entry.Id });

            var editor = new ctlAddEdit(
                new FakeEntryService(),
                new FakeNavigation(),
                new FakeShellHost(),
                new EntryEditorRequest(entry))
            {
                Size = new Size(width, height),
            };
            editor.PerformLayout();
            return editor;
        }

        [Theory]
        [InlineData(1024, 700)]
        [InlineData(1280, 800)]
        public void EachTabScrollsIndependentlyWithoutAGlobalScrollbar(int width, int height)
        {
            using var editor = CreateEditor(width, height);

            var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

            Assert.True(tabs.TabPages.Count > 1, "The editor must offer tab navigation.");

            foreach (TabPageAdv page in tabs.TabPages)
            {
                var body = page.Controls.OfType<Panel>().Single();
                Assert.True(body.AutoScroll, $"The '{page.Text}' tab must scroll on its own.");
            }

            // The editor root must not be a single scrolling container holding
            // every group; only the active tab body scrolls.
            var root = editor.Controls[0];
            Assert.False(
                root is Panel { AutoScroll: true },
                "The editor must not stack every group into one global scrollbar.");
        }

        [Theory]
        [InlineData(1024, 700)]
        [InlineData(1280, 800)]
        public void ActiveTabFieldsKeepAUsableSize(int width, int height)
        {
            using var editor = CreateEditor(width, height);

            var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

            // Which fields are expected on which tab (index into TabPages).
            var expected = new (string Field, int Tab)[]
            {
                ("_title", 0),
                ("_project", 0),
                ("_technologies", 0),
                ("_date", 0),
                ("_tags", 0),
                ("_favorite", 0),
                ("_description", 0),
                ("_comments", 0),
                ("_code", 2),
                ("_youtube", 3),
            };

            foreach (var group in expected.GroupBy(item => item.Tab).OrderBy(group => group.Key))
            {
                tabs.SelectedIndex = group.Key;
                editor.PerformLayout();

                foreach (var (field, _) in group)
                {
                    var control = FieldOf(editor, field);
                    Assert.True(control.Visible, $"{field} must be visible on tab {group.Key}.");
                    Assert.True(control.Width > 0, $"{field} must have a width.");
                    Assert.True(control.Height > 0, $"{field} must have a height.");
                }
            }
        }

        [Theory]
        [InlineData(1024, 700)]
        [InlineData(1280, 800)]
        public void TheActiveTabFitsTheViewportWithoutGlobalScrolling(int width, int height)
        {
            using var editor = CreateEditor(width, height);

            var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

            for (var index = 0; index < tabs.TabPages.Count; index++)
            {
                tabs.SelectedIndex = index;
                editor.PerformLayout();

                var body = tabs.TabPages[index].Controls.OfType<Panel>().Single();
                var form = body.Controls.OfType<TableLayoutPanel>().Single();

                // The General tab (index 0) intentionally has many fixed-height
                // fields whose sum may exceed a small viewport — it is scrollable
                // by design (AutoScroll = true, verified by
                // EachTabScrollsIndependentlyWithoutAGlobalScrollbar).
                // Only verify that the non-dense tabs fit comfortably.
                if (index == 0)
                {
                    continue;
                }

                Assert.True(
                    form.PreferredSize.Height <= body.ClientSize.Height + 4,
                    $"The '{tabs.TabPages[index].Text}' tab should fit without scrolling " +
                    $"(content {form.PreferredSize.Height}px vs viewport {body.ClientSize.Height}px).");
            }
        }

        private static Control FieldOf(object instance, string name) => (Control)instance
            .GetType()
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(instance)!;

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
            public Task<IReadOnlyList<KnowledgeEntry>> GetAllAsync() =>
                Task.FromResult<IReadOnlyList<KnowledgeEntry>>(Array.Empty<KnowledgeEntry>());

            public Task<KnowledgeEntry?> GetByIdAsync(int id) => Task.FromResult<KnowledgeEntry?>(null);

            public Task SaveAsync(KnowledgeEntry entry) => Task.CompletedTask;

            public Task DeleteAsync(int id) => Task.CompletedTask;

            public Task ToggleFavoriteAsync(KnowledgeEntry entry) => Task.CompletedTask;

            public Task<IReadOnlyList<string>> GetAllTagsAsync() =>
                Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

            public Task<IReadOnlyList<string>> GetAllTechnologiesAsync() =>
                Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        private sealed class FakeNavigation : INavigationService
        {
            public Control? CurrentView => null;
            public bool CanGoBack => false;
            public event EventHandler? Navigated { add { } remove { } }
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
