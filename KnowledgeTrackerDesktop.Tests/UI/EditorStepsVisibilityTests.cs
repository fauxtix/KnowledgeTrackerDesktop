using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI;

public sealed class EditorStepsVisibilityTests
{
    [Fact]
    public void ExistingStepsAreVisibleOnTheResolutionsTab()
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
        Assert.Equal(0, tabs.SelectedIndex);

        var stepInputs = Descendants(tabs.TabPages[3])
            .OfType<TextBoxExt>()
            .Where(input => input.Text.EndsWith("resolution step", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(new[] { "First resolution step", "Second resolution step" }, stepInputs.Select(input => input.Text));
        Assert.All(stepInputs, input => Assert.True(input.Bounds.Height > 0));
    }

    [Fact]
    public void AttachmentsAreOnTheSecondTab()
    {
        var entry = new KnowledgeEntry { Id = 5, Title = "Attachment tab entry" };
        entry.Attachments.Add(new AttachmentInfo
        {
            KnowledgeEntryId = entry.Id,
            FileName = "file.txt",
            FilePath = @"C:\file.txt",
        });

        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(entry));

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        Assert.Equal("Attachments", tabs.TabPages[1].Text);
        Assert.Contains(
            Descendants(tabs.TabPages[1]).OfType<SfButton>(),
            button => Equals(button.Tag, "+ Add file"));
        Assert.Contains(
            Descendants(tabs.TabPages[1]).OfType<Label>(),
            label => label.Text == "file.txt");
    }

    [Fact]
    public void YouTubeControlsAreOnTheThirdTab()
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

        Assert.Equal("YouTube", tabs.TabPages[2].Text);
        Assert.Contains(
            Descendants(tabs.TabPages[2]).OfType<TextBoxExt>(),
            input => input.Text == entry.YouTubeUrl);
        Assert.Contains(
            Descendants(tabs.TabPages[2]).OfType<SfButton>(),
            button => button.Text == "Open video");
    }

    [Fact]
    public void ManageStepsAddButtonExpandsForPortugueseText()
    {
        using var manageSteps = new ctlManageSteps(
            new FakeNavigation(),
            new FakeShellHost(),
            new ManageStepsRequest(new KnowledgeEntry { Title = "Steps" }));

        var addStep = Descendants(manageSteps)
            .OfType<SfButton>()
            .Single(button => Equals(button.Tag, "+ Add step"));
        addStep.Text = AppLocalization.Translate("+ Add step", AppLanguage.Portuguese);
        Assert.Equal("+ Adicionar", addStep.Text);
        typeof(ctlManageSteps)
            .GetMethod("UpdateAddStepButtonSize", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(manageSteps, null);
        addStep.PerformLayout();

        var textWidth = TextRenderer.MeasureText(
            addStep.Text,
            addStep.Font,
            Size.Empty,
            TextFormatFlags.NoPadding).Width;

        Assert.False(addStep.AutoSize);
        Assert.True(addStep.Width > 120);
        Assert.True(addStep.Width >= textWidth + 40);
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
    public void CodeEditorHasCompactHeight()
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

        Assert.InRange(codeEditor.Height, 100, 140);
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

[Fact]
        public void PreviewCodeIsOnlyEnabledWhenCodeIsNotEmpty()
        {
            using var editor = new ctlAddEdit(
                new FakeEntryService(),
                new FakeNavigation(),
                new FakeShellHost(),
                new EntryEditorRequest(new KnowledgeEntry { Title = "Preview test" }));

            var codeEditor = (TextBoxExt)typeof(ctlAddEdit)
                .GetField("_code", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(editor)!;
            var previewButton = Descendants(editor)
                .OfType<SfButton>()
                .Single(button => Equals(button.Tag, "Preview"));

            Assert.False(previewButton.Enabled);
            codeEditor.Text = "Console.WriteLine();";
            Assert.True(previewButton.Enabled);
            codeEditor.Clear();
            Assert.False(previewButton.Enabled);
        }

        [Fact]
        public void CodePreviewFormShowsTheSnippetAndACloseButton()
        {
            using var form = new CodePreviewForm("var x = 1;");

            var preview = Descendants(form)
                .OfType<TextBox>()
                .Single(textBox => textBox.Text == "var x = 1;");
            Assert.True(preview.Multiline);
            Assert.True(preview.ReadOnly);

            Assert.Contains(
                Descendants(form).OfType<SfButton>(),
                button => Equals(button.Tag, "Close"));
        }

        [Fact]
        public void TagsAndFavoriteAreVisibleOnTheGeneralTab()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Id = 3, Title = "Tags test", Tags = "ui;sqlite" }))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        var generalTab = tabs.TabPages[0];

        var tagsEditor = (TextBoxExt)typeof(ctlAddEdit)
            .GetField("_tags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var favorite = (CheckBoxAdv)typeof(ctlAddEdit)
            .GetField("_favorite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.Contains(tagsEditor, Descendants(generalTab));
        Assert.Contains(favorite, Descendants(generalTab));
        Assert.Equal("ui;sqlite", tagsEditor.Text);
    }

    [Fact]
    public void MetadataInputsShareTheSameHeight()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Id = 4, Title = "Metadata height test" }))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();

        Control Field(string name) => (Control)typeof(ctlAddEdit)
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;

        var technologies = Field("_technologies");
        var date = Field("_date");

        Assert.InRange(date.Height, technologies.Height - 1, technologies.Height + 1);
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
