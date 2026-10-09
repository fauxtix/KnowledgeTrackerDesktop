using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI;

public sealed class EditorStepsVisibilityTests
{
    [Fact]
    public void ExistingStepsOpenTheResolutionsTabWithRowsVisible()
    {
        var entry = new KnowledgeEntry { Id = 1, Title = "Entry with steps" };
        entry.Steps.Add(new Step { Description = "First resolution step", Order = 1, KnowledgeEntryId = entry.Id });
        entry.Steps.Add(new Step { Description = "Second resolution step", Order = 2, KnowledgeEntryId = entry.Id });

        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(entry))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        Assert.Equal(2, tabs.SelectedIndex);

        var stepInputs = Descendants(tabs.TabPages[2])
            .OfType<TextBoxExt>()
            .Where(input => input.Text.EndsWith("resolution step", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(new[] { "First resolution step", "Second resolution step" }, stepInputs.Select(input => input.Text));
        Assert.All(stepInputs, input => Assert.True(input.Bounds.Height > 0));
    }

    [Fact]
    public void YouTubeControlsAreOnTheSecondTab()
    {
        var entry = new KnowledgeEntry
        {
            Id = 2,
            Title = "Video entry",
            YouTubeUrl = "https://youtube.com/watch?v=example",
        };
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(entry));

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        Assert.Equal("YouTube", tabs.TabPages[1].Text);
        Assert.Contains(
            Descendants(tabs.TabPages[1]).OfType<TextBoxExt>(),
            input => input.Text == entry.YouTubeUrl);
        Assert.Contains(
            Descendants(tabs.TabPages[1]).OfType<SfButton>(),
            button => button.Text == "Open video");
    }

    [Fact]
    public void AddAttachmentButtonExpandsForLocalizedText()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Title = "Attachment test" }))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();

        var addAttachment = Descendants(editor)
            .OfType<SfButton>()
            .Single(button => Equals(button.Tag, "+ Add file"));
        addAttachment.Text = "+ Adicionar ficheiro";
        addAttachment.PerformLayout();

        Assert.True(addAttachment.AutoSize);
        Assert.True(addAttachment.Width > 110);
        Assert.True(addAttachment.Width >= TextRenderer.MeasureText(
            addAttachment.Text,
            addAttachment.Font).Width);
    }

    [Fact]
    public void CodeEditorHasIncreasedHeight()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Title = "Code height test" }))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        var codeEditor = Descendants(tabs.TabPages[0])
            .OfType<TextBoxExt>()
            .Single(input => input.Multiline && input.ScrollBars == ScrollBars.Both);

        Assert.True(codeEditor.Height >= 210);
    }

    [Fact]
    public void CommentsEditorHasReducedHeight()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Title = "Comments height test" }))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();

        var commentsEditor = (TextBoxExt)typeof(ctlAddEdit)
            .GetField("_comments", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.InRange(commentsEditor.Height, 1, 56);
    }

    [Fact]
    public void CopyCodeButtonIsEnabledOnlyWhenCodeIsNotEmpty()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Title = "Copy code test" }));

        var codeEditor = (TextBoxExt)typeof(ctlAddEdit)
            .GetField("_code", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var copyButton = Descendants(editor)
            .OfType<SfButton>()
            .Single(button => button.Text == "Copy");

        Assert.False(copyButton.Enabled);
        codeEditor.Text = "Console.WriteLine();";
        Assert.True(copyButton.Enabled);
        codeEditor.Clear();
        Assert.False(copyButton.Enabled);
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
        public Task<IReadOnlyList<KnowledgeEntry>> GetAllAsync() =>
            Task.FromResult<IReadOnlyList<KnowledgeEntry>>(Array.Empty<KnowledgeEntry>());

        public Task<KnowledgeEntry?> GetByIdAsync(int id) => Task.FromResult<KnowledgeEntry?>(null);

        public Task SaveAsync(KnowledgeEntry entry) => Task.CompletedTask;

        public Task DeleteAsync(int id) => Task.CompletedTask;

        public Task ToggleFavoriteAsync(KnowledgeEntry entry) => Task.CompletedTask;
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
