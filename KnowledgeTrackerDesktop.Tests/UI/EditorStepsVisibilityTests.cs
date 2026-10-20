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
    public void ExistingStepsAreVisibleInTheEditor()
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

        var stepInputs = Descendants(tabs.TabPages[1])
            .OfType<TextBoxExt>()
            .Where(input => input.Text.EndsWith("resolution step", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(new[] { "First resolution step", "Second resolution step" }, stepInputs.Select(input => input.Text));
        Assert.All(stepInputs, input => Assert.True(input.Bounds.Height > 0));
    }

    [Fact]
    public void AttachmentsAppearInTheEditor()
    {
        var entry = new KnowledgeEntry { Id = 5, Title = "Attachment entry" };
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

        Assert.Contains(
            Descendants(editor).OfType<Label>(),
            label => Equals(label.Tag, "Attachments and references"));
        Assert.Contains(
            Descendants(editor).OfType<SfButton>(),
            button => Equals(button.Tag, "+ Add file"));
        Assert.Contains(
            Descendants(editor).OfType<Label>(),
            label => label.Text == "file.txt");
    }

    [Fact]
    public void YouTubeControlsAppearInTheEditor()
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

        Assert.Contains(
            Descendants(editor).OfType<TextBoxExt>(),
            input => input.Text == entry.YouTubeUrl);
        Assert.Contains(
            Descendants(editor).OfType<SfButton>(),
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
    public void CodeEditorExpandsToFillAvailableSpace()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Title = "Code height test" }))
        {
            Size = new Size(1200, 800),
        };
        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        tabs.SelectedIndex = 2; // Code tab
        editor.PerformLayout();

        var codeEditor = Descendants(editor)
            .OfType<TextBoxExt>()
            .Single(input => input.Multiline && input.ScrollBars == ScrollBars.Both);

        Assert.True(codeEditor.Height > 300, $"Code editor should expand to fill available space, but was {codeEditor.Height}px.");
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
        public void TagsAndFavoriteAreVisibleInTheEditor()
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

        var tagsEditor = (TextBoxExt)typeof(ctlAddEdit)
            .GetField("_tags", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var favorite = (CheckBoxAdv)typeof(ctlAddEdit)
            .GetField("_favorite", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Assert.Contains(tagsEditor, Descendants(editor));
        Assert.Contains(favorite, Descendants(editor));
        Assert.Equal("ui;sqlite", tagsEditor.Text);
    }

    [Fact]
    public void SectionsAreGroupedIntoTabsInAStableOrder()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Id = 6, Title = "Sections test" }))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        string[] expectedTabs =
        {
            "General",
            "Resolutions",
            "Code",
            "Attachments",
        };

        Assert.Equal(
            expectedTabs,
            tabs.TabPages.Cast<TabPageAdv>().Select(page => (string)page.Tag!).ToArray());

        // Every field lives on exactly one tab, so nothing is duplicated or
        // dropped when the single page was split into groups.
        Control Field(string name) => (Control)typeof(ctlAddEdit)
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(editor)!;

        var expectedOwner = new Dictionary<string, int>
        {
            ["_title"] = 0,
            ["_project"] = 0,
            ["_technologies"] = 0,
            ["_date"] = 0,
            ["_tags"] = 0,
            ["_favorite"] = 0,
            ["_description"] = 0,
            ["_comments"] = 0,
            ["_code"] = 2,
            ["_youtube"] = 3,
        };

        foreach (var pair in expectedOwner)
        {
            var control = Field(pair.Key);
            var owner = control.Parent;
            while (owner is not null && owner is not TabPageAdv)
            {
                owner = owner.Parent;
            }

            Assert.True(owner is TabPageAdv, $"{pair.Key} must sit inside a tab page.");
            Assert.Same(tabs.TabPages[pair.Value], owner);

            var hosts = tabs.TabPages.Cast<TabPageAdv>()
                .Count(page => IsInsideTab(control, page));
            Assert.Equal(1, hosts);
        }

        // Steps are rebuilt into the Resolutions tab.
        Assert.True(
            IsInsideTab(Field("_stepRows"), tabs.TabPages[1]),
            "The steps rows must sit inside the Resolutions tab.");
    }

    private static bool IsInsideTab(Control control, Control expectedPage)
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

    [Fact]
    public void TabLabelsAreBoundToLocalisableKeys()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Id = 7, Title = "Tab language test" }));
        editor.PerformLayout();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        string[] englishKeys =
        {
            "General",
            "Resolutions",
            "Code",
            "Attachments",
        };

        var pages = tabs.TabPages.Cast<TabPageAdv>().ToArray();
        Assert.Equal(englishKeys, pages.Select(page => (string)page.Tag!).ToArray());

        foreach (var key in englishKeys)
        {
            var page = pages.Single(candidate => Equals(candidate.Tag, key));

            // The rendered text comes from the translator for the active
            // language, and a Portuguese translation exists for the key.
            Assert.Equal(AppLocalization.Translate(key), page.Text);
            Assert.NotEqual(key, AppLocalization.Translate(key, AppLanguage.Portuguese));
        }
    }

    [Fact]
    public void EveryTabHostsAIndependentlyScrollableBody()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Id = 8, Title = "Scroll test" }))
        {
            Size = new Size(1024, 700),
        };
        editor.PerformLayout();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();

        foreach (TabPageAdv page in tabs.TabPages)
        {
            var body = page.Controls.OfType<Panel>().Single();
            Assert.True(body.AutoScroll, $"The '{page.Text}' tab must scroll on its own.");
        }

        // The editor itself must not be one long scrolling page any more: the
        // tab strip stays put while only the active body scrolls.
        Assert.False(
            tabs.Parent is Panel { AutoScroll: true },
            "The editor must not stack every group into a single global scrollbar.");
    }

    [Fact]
    public void SaveValidationErrorReturnsToTheGeneralTab()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Id = 9, Title = "Validation test" }))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();
        editor.OnAppearingAsync();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        tabs.SelectedIndex = 3;

        var title = (TextBoxExt)FieldOf(editor, "_title");
        title.Text = "   ";

        var save = (SfButton)FieldOf(editor, "_saveButton");
        save.PerformClick();

        Assert.Equal(0, tabs.SelectedIndex);
    }

    [Fact]
    public void SaveValidationErrorForTheDescriptionStaysOnTheGeneralTabAndShowsTheMessage()
    {
        var entry = CompleteEntry(10);
        entry.Description = null;

        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(entry))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();
        editor.OnAppearingAsync();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        tabs.SelectedIndex = 2;

        var save = (SfButton)FieldOf(editor, "_saveButton");
        save.PerformClick();

        var error = (Label)FieldOf(editor, "_errorLabel");
        Assert.True(error.Visible);
        Assert.Equal("A description is required.", error.Tag);
        Assert.Equal(0, tabs.SelectedIndex);
    }

    [Fact]
    public void SaveValidationErrorForTheYouTubeUrlOpensTheAttachmentsTab()
    {
        using var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(CompleteEntry(11)))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();
        editor.OnAppearingAsync();

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        tabs.SelectedIndex = 0;

        var youtube = (TextBoxExt)FieldOf(editor, "_youtube");
        youtube.Text = "not a url";

        var save = (SfButton)FieldOf(editor, "_saveButton");
        save.PerformClick();

        var error = (Label)FieldOf(editor, "_errorLabel");
        Assert.True(error.Visible);
        Assert.Equal("The YouTube URL must be a valid http(s) link.", error.Tag);
        Assert.Equal(3, tabs.SelectedIndex);
    }

    [Fact]
    public async Task SuggestionAutoCompletesAreAttachedToTagsAndTechnologies()
    {
        var service = new FakeEntryService
        {
            Tags = new[] { "sqlite", "winforms" },
            Technologies = new[] { "C#" },
        };
        using var editor = new ctlAddEdit(
            service,
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(CompleteEntry(12)))
        {
            Size = new Size(1200, 800),
        };
        editor.PerformLayout();
        await editor.OnAppearingAsync();

        var tags = (TextBoxExt)FieldOf(editor, "_tags");
        var technologies = (TextBoxExt)FieldOf(editor, "_technologies");

        // Both fields get the official Syncfusion AutoComplete attached in
        // suggest mode, each one fed with the values the library knows.
        var tagsAutoComplete = PrivateField<AutoComplete>(editor, "_tagsAutoComplete");
        var technologiesAutoComplete = PrivateField<AutoComplete>(editor, "_technologiesAutoComplete");
        Assert.NotNull(tagsAutoComplete);
        Assert.NotNull(technologiesAutoComplete);
        Assert.Equal(AutoCompleteModes.AutoSuggest, tagsAutoComplete.GetAutoComplete(tags));
        Assert.Equal(AutoCompleteModes.AutoSuggest, technologiesAutoComplete.GetAutoComplete(technologies));
        Assert.Equal(new[] { "sqlite", "winforms" }, SuggestedList(tagsAutoComplete));
        Assert.Equal(new[] { "C#" }, SuggestedList(technologiesAutoComplete));
    }

    private static IReadOnlyList<string> SuggestedList(AutoComplete autoComplete) =>
        ((IEnumerable<string>)autoComplete.DataSource!).ToArray();

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

    private static Control FieldOf(object instance, string name) => (Control)instance
        .GetType()
        .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .GetValue(instance)!;

    /// <summary>
    /// Reads a private field that is not a control (e.g. the AutoComplete
    /// components, which are <see cref="System.ComponentModel.Component"/>s).
    /// </summary>
    private static T PrivateField<T>(object instance, string name) => (T)instance
        .GetType()
        .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
        .GetValue(instance)!;

    /// <summary>
    /// An entry that satisfies every EntryValidator rule, so a save attempt
    /// only reports the field each test deliberately breaks.
    /// </summary>
    private static KnowledgeEntry CompleteEntry(int id) => new()
    {
        Id = id,
        Title = "Complete entry",
        ProjectName = "KnowledgeTracker",
        Technologies = "C#; WinForms",
        DateResolved = new DateTime(2026, 10, 1),
        Tags = "sqlite, validation",
        Description = "A documented solution.",
    };

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
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

        public IReadOnlyList<string> Technologies { get; set; } = Array.Empty<string>();

        public Task<IReadOnlyList<KnowledgeEntry>> GetAllAsync() =>
            Task.FromResult<IReadOnlyList<KnowledgeEntry>>(Array.Empty<KnowledgeEntry>());

        public Task<KnowledgeEntry?> GetByIdAsync(int id) => Task.FromResult<KnowledgeEntry?>(null);

        public Task SaveAsync(KnowledgeEntry entry) => Task.CompletedTask;

        public Task DeleteAsync(int id) => Task.CompletedTask;

        public Task ToggleFavoriteAsync(KnowledgeEntry entry) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> GetAllTagsAsync() => Task.FromResult(Tags);

        public Task<IReadOnlyList<string>> GetAllTechnologiesAsync() => Task.FromResult(Technologies);

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
