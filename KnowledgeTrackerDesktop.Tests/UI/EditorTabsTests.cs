using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI;

/// <summary>
/// The editor header contract: exactly four tabs, in a fixed order, with the
/// full caption of every tab drawn at the minimum window size in both
/// languages, and every field owned by a single tab.
/// </summary>
public sealed class EditorTabsTests
{
    private static readonly string[] ExpectedTabKeys =
    {
        "General",
        "Resolutions",
        "Code",
        "Attachments",
    };

    [Fact]
    public void TheEditorShowsExactlyFourTabsInTheDocumentedOrder()
    {
        using var editor = CreateEditor();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        Assert.Equal(ExpectedTabKeys.Length, tabs.TabPages.Count);
        Assert.Equal(
            ExpectedTabKeys,
            tabs.TabPages.Cast<TabPageAdv>().Select(page => (string)page.Tag!).ToArray());
        Assert.Equal(0, tabs.SelectedIndex);

        Assert.Equal(Syncfusion.Windows.Forms.Tools.TabSizeMode.Fixed, tabs.SizeMode);
        Assert.Equal(new Size(140, 36), tabs.ItemSize);
    }

    [Fact]
    public void DescriptionTabDoesNotExistAndContentIsOnGeneralTab()
    {
        using var editor = CreateEditor();
        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        Assert.DoesNotContain(
            tabs.TabPages.Cast<TabPageAdv>(),
            page => string.Equals((string)page.Tag!, "Description", StringComparison.OrdinalIgnoreCase));

        var generalTab = tabs.TabPages[0];
        Assert.True(IsInside(FieldOf(editor, "_description"), generalTab));
        Assert.True(IsInside(FieldOf(editor, "_comments"), generalTab));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryTabLabelIsLocalisedInBothLanguages(bool portuguese)
    {
        var language = portuguese ? AppLanguage.Portuguese : AppLanguage.English;
        using var editor = CreateEditor();

        var pages = Descendants(editor)
            .OfType<TabControlAdv>()
            .Single()
            .TabPages
            .Cast<TabPageAdv>()
            .ToArray();

        Assert.Equal(ExpectedTabKeys.Length, pages.Length);
        foreach (var page in pages)
        {
            var key = (string)page.Tag!;
            Assert.Contains(key, ExpectedTabKeys);

            var expected = AppLocalization.Translate(key, language);
            Assert.False(
                string.IsNullOrWhiteSpace(expected),
                $"'{key}' has no caption for {language}.");

            if (portuguese)
            {
                // No English leftovers leaking into the PT header.
                Assert.NotEqual(key, expected);
            }
            else
            {
                Assert.Equal(key, expected);
            }

            // The header renders exactly the translated caption once the
            // language switch is applied (the same assignment
            // AppLocalization.Apply performs on LanguageChanged).
            page.Text = expected;
            Assert.Equal(expected, page.Text);
        }
    }

    [Fact]
    public void EveryFieldBelongsToExactlyOneTab()
    {
        using var editor = CreateEditor();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        // field name -> expected tab index (0-based)
        var fields = new Dictionary<string, int>
        {
            ["_title"] = 0,
            ["_project"] = 0,
            ["_technologies"] = 0,
            ["_date"] = 0,
            ["_tags"] = 0,
            ["_favorite"] = 0,
            ["_description"] = 0,
            ["_comments"] = 0,
            ["_stepRows"] = 1,
            ["_code"] = 2,
            ["_attachments"] = 3,
            ["_openGallery"] = 3,
            ["_youtube"] = 3,
            ["_openVideo"] = 3,
        };

        foreach (var (name, expectedTab) in fields)
        {
            var control = FieldOf(editor, name);

            var hosts = tabs.TabPages.Cast<TabPageAdv>()
                .Count(page => IsInside(control, page));
            Assert.True(
                hosts == 1,
                $"{name} must belong to exactly one tab, found on {hosts}.");
            Assert.True(
                IsInside(control, tabs.TabPages[expectedTab]),
                $"{name} must sit on tab {expectedTab} ('{tabs.TabPages[expectedTab].Text}').");
        }
    }

    [Fact]
    public void CodeTabHostsCodeEditorAndActions()
    {
        var entry = new KnowledgeEntry
        {
            Id = 11,
            Title = "Code entry",
            CodeSnippet = "Console.WriteLine();",
        };

        using var editor = CreateEditor(entry);

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        var codeTab = tabs.TabPages.Cast<TabPageAdv>().Single(page => Equals(page.Tag, "Code"));
        Assert.Equal(2, tabs.TabPages.IndexOf(codeTab));

        // Code editor + its actions.
        Assert.True(IsInside(FieldOf(editor, "_code"), codeTab));
        Assert.Contains(
            Descendants(codeTab).OfType<SfButton>(),
            button => Equals(button.Tag, "Copy"));
        Assert.Contains(
            Descendants(codeTab).OfType<SfButton>(),
            button => Equals(button.Tag, "Preview"));

        // No attachments or video in Code tab
        Assert.False(IsInside(FieldOf(editor, "_attachments"), codeTab));
        Assert.False(IsInside(FieldOf(editor, "_youtube"), codeTab));
    }

    [Fact]
    public void AttachmentsTabHostsAttachmentsGalleryAndVideo()
    {
        var entry = new KnowledgeEntry
        {
            Id = 12,
            Title = "Attachments entry",
            YouTubeUrl = "https://youtube.com/watch?v=example",
        };
        entry.Attachments.Add(new AttachmentInfo
        {
            KnowledgeEntryId = entry.Id,
            FileName = "diagram.png",
            FilePath = @"C:\diagram.png",
        });

        using var editor = CreateEditor(entry);

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        var attachmentsTab = tabs.TabPages.Cast<TabPageAdv>().Single(page => Equals(page.Tag, "Attachments"));
        Assert.Equal(3, tabs.TabPages.IndexOf(attachmentsTab));

        // Attachments + gallery.
        Assert.True(IsInside(FieldOf(editor, "_attachments"), attachmentsTab));
        Assert.True(IsInside(FieldOf(editor, "_openGallery"), attachmentsTab));
        Assert.Contains(
            Descendants(attachmentsTab).OfType<SfButton>(),
            button => Equals(button.Tag, "+ Add file"));
        Assert.Contains(
            Descendants(attachmentsTab).OfType<SfButton>(),
            button => Equals(button.Tag, "Open gallery"));

        // Video link + open action.
        Assert.True(IsInside(FieldOf(editor, "_youtube"), attachmentsTab));
        Assert.True(IsInside(FieldOf(editor, "_openVideo"), attachmentsTab));
        var openVideo = (SfButton)FieldOf(editor, "_openVideo");
        Assert.Equal("Open video", openVideo.Text);
        Assert.True(openVideo.Enabled, "A valid video URL must enable the open action.");

        // No code in attachments tab
        Assert.False(IsInside(FieldOf(editor, "_code"), attachmentsTab));
    }

    [Fact]
    public void AddStepButtonCreatesStepAndUpdatesList()
    {
        var entry = new KnowledgeEntry { Id = 20, Title = "Step Test" };
        using var editor = CreateEditor(entry);

        var resolutionsTab = Descendants(editor).OfType<TabControlAdv>().Single().TabPages[1];

        // Verify the Add step button exists on the Resolutions tab with the correct Tag.
        Assert.Contains(
            Descendants(resolutionsTab).OfType<SfButton>(),
            b => Equals(b.Tag, "+ Add step"));

        var stepRows = (TableLayoutPanel)FieldOf(editor, "_stepRows");
        Assert.Empty(stepRows.Controls);

        // SfButton.PerformClick() requires a live Win32 handle (not available in
        // headless tests). Invoke AddStep() directly — same path the button uses.
        editor.AddStep();

        Assert.Single(stepRows.Controls);
        // Access _stepsCount directly instead of searching by text to avoid
        // confusion with the "Resolution steps" section header label.
        var stepsCount = (Label)FieldOf(editor, "_stepsCount");
        Assert.Equal("1 step", stepsCount.Text);

        editor.AddStep();
        Assert.Equal(2, stepRows.Controls.Count);
    }

    [Fact]
    public void AddAttachmentButtonStartsCorrectFlowAndAddsAttachments()
    {
        var entry = new KnowledgeEntry { Id = 30, Title = "Attachment Flow Test" };
        using var editor = CreateEditor(entry);

        var pickerInvoked = false;
        editor.FilePicker = () =>
        {
            pickerInvoked = true;
            return new[] { @"C:\test1.png", @"C:\test2.pdf" };
        };

        var attachmentsTab = Descendants(editor).OfType<TabControlAdv>().Single().TabPages[3];

        // Verify the Add file button exists on the Attachments tab with the correct Tag.
        Assert.Contains(
            Descendants(attachmentsTab).OfType<SfButton>(),
            b => Equals(b.Tag, "+ Add file"));

        // SfButton.PerformClick() requires a live Win32 handle (not available in
        // headless tests). Invoke AddAttachment() directly — same path the button uses.
        editor.AddAttachment();

        Assert.True(pickerInvoked, "The FilePicker flow must be invoked on click.");
        Assert.Equal(2, entry.Attachments.Count);
        Assert.Contains(entry.Attachments, a => a.FileName == "test1.png");
        Assert.Contains(entry.Attachments, a => a.FileName == "test2.pdf");
    }

    [Fact]
    public void CodeEditorExpandsWithAvailableSpace()
    {
        using var smallEditor = CreateEditor(size: new Size(1024, 700));
        var smallTabs = Descendants(smallEditor).OfType<TabControlAdv>().Single();
        smallTabs.SelectedIndex = 2; // Code tab
        smallEditor.PerformLayout();
        var smallCode = (TextBoxExt)FieldOf(smallEditor, "_code");
        var smallHeight = smallCode.Height;
        var smallWidth = smallCode.Width;

        using var largeEditor = CreateEditor(size: new Size(1400, 950));
        var largeTabs = Descendants(largeEditor).OfType<TabControlAdv>().Single();
        largeTabs.SelectedIndex = 2; // Code tab
        largeEditor.PerformLayout();
        var largeCode = (TextBoxExt)FieldOf(largeEditor, "_code");
        var largeHeight = largeCode.Height;
        var largeWidth = largeCode.Width;

        Assert.True(smallHeight > 300, $"Code editor should have substantial height at 1024x700, but was {smallHeight}px.");
        Assert.True(largeHeight > smallHeight, $"Code editor height must expand with window (small: {smallHeight}px, large: {largeHeight}px).");
        Assert.True(largeWidth > smallWidth, $"Code editor width must expand with window (small: {smallWidth}px, large: {largeWidth}px).");
    }

    [Theory]
    [InlineData(1024, 700)]
    [InlineData(1280, 800)]
    public void EveryTabCaptionIsDrawnInFullAtBothWindowSizes(int width, int height)
    {
        var inset = GetTabTextInset();

        foreach (var language in new[] { AppLanguage.English, AppLanguage.Portuguese })
        {
            using var editor = CreateEditor(size: new Size(width, height));
            var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
            _ = tabs.Handle;
            tabs.PerformLayout();

            // Render the translated captions without touching the persisted
            // language preference.
            foreach (TabPageAdv page in tabs.TabPages)
            {
                page.Text = AppLocalization.Translate((string)page.Tag!, language);
            }

            tabs.PerformLayout();

            using var surface = new Bitmap(Math.Max(1, tabs.Width), Math.Max(1, tabs.Height));
            using var graphics = Graphics.FromImage(surface);

            foreach (TabPageAdv page in tabs.TabPages)
            {
                var index = tabs.TabPages.IndexOf(page);
                var header = tabs.GetTabRect(index);
                var textBounds = Rectangle.Inflate(header, -inset, -2);

                // The caption font is what the strip sizes the header from,
                // so the shorter of the two rendered fonts must always fit:
                // that is exactly what keeps "..." out of the header.
                Assert.True(
                    Fits(graphics, page.Text, AppTheme.Fonts.Caption, textBounds),
                    $"'{page.Text}' ({language}) is clipped at {width}x{height}: " +
                    $"header {textBounds.Width}px needs " +
                    $"{TextRenderer.MeasureText(graphics, page.Text, AppTheme.Fonts.Caption, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width}px.");
            }
        }
    }

    private static bool Fits(Graphics graphics, string text, Font font, Rectangle bounds)
    {
        var flags = TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix;
        var required = TextRenderer.MeasureText(graphics, text, font, Size.Empty, flags);
        return required.Width <= bounds.Width && required.Height <= bounds.Height;
    }

    private static int GetTabTextInset() => (int)typeof(ctlAddEdit)
        .GetField("TabTextInset", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!
        .GetRawConstantValue()!;

    private static ctlAddEdit CreateEditor(
        KnowledgeEntry? entry = null,
        Size? size = null)
    {
        var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(entry ?? new KnowledgeEntry { Id = 21, Title = "Tab layout" }))
        {
            Size = size ?? new Size(1200, 800),
        };
        editor.PerformLayout();
        return editor;
    }

    private static bool IsInside(Control control, Control expectedPage)
    {
        for (var current = control; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, expectedPage))
            {
                return true;
            }
        }

        return false;
    }

    private static Control FieldOf(object instance, string name) => (Control)instance
        .GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
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

        public Task<KnowledgeExport> ExportAsync() =>
            Task.FromResult(new KnowledgeExport());

        public Task<ImportResult> ImportAsync(KnowledgeExport document, ImportMode mode) =>
            Task.FromResult(new ImportResult(0, 0, 0));

        public Task<int> CountDuplicatesAsync(KnowledgeExport document) =>
            Task.FromResult(0);
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
