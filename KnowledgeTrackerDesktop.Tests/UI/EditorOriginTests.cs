using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI;

/// <summary>
/// The editor half of the typed project origin: the kind and the location are
/// loaded into the form, written back to the entry on save, and the browse
/// action only offers a folder for a local origin.
/// </summary>
public sealed class EditorOriginTests
{
    [Fact]
    public void TheOriginInputsBelongToTheGeneralTab()
    {
        using var editor = CreateEditor(new KnowledgeEntry { Id = 1, Title = "Origin" });

        var tabs = Descendants(editor).OfType<TabControlAdv>().Single();
        var general = tabs.TabPages.Cast<TabPageAdv>().First(page => Equals(page.Tag, "General"));

        Assert.True(IsInside(FieldOf(editor, "_originKind"), general));
        Assert.True(IsInside(FieldOf(editor, "_originLocation"), general));
    }

    [Fact]
    public void AnEntryWithoutOriginLoadsAsNotRecorded()
    {
        using var editor = CreateEditor(new KnowledgeEntry { Id = 2, Title = "No origin" });

        Assert.Equal(0, OriginKindBox(editor).SelectedIndex);
        Assert.Equal(string.Empty, OriginLocationBox(editor).Text);
    }

    [Theory]
    [InlineData(ProjectOriginKind.LocalFolder, 1)]
    [InlineData(ProjectOriginKind.GitRepository, 2)]
    [InlineData(ProjectOriginKind.WebPage, 3)]
    public void AnEntryWithOriginLoadsIntoTheInputs(ProjectOriginKind kind, int expectedIndex)
    {
        var entry = new KnowledgeEntry
        {
            Id = 3,
            Title = "With origin",
            ProjectOriginKind = kind,
            ProjectOriginLocation = kind == ProjectOriginKind.LocalFolder
                ? @"C:\projects\app"
                : "https://example.com/origin",
        };

        using var editor = CreateEditor(entry);

        Assert.Equal(expectedIndex, OriginKindBox(editor).SelectedIndex);
        Assert.Equal(entry.ProjectOriginLocation, OriginLocationBox(editor).Text);
    }

    [Theory]
    [InlineData(ProjectOriginKind.LocalFolder, 1)]
    [InlineData(ProjectOriginKind.GitRepository, 2)]
    [InlineData(ProjectOriginKind.WebPage, 3)]
    public void SavingWritesTheOriginBackToTheEntry(ProjectOriginKind kind, int index)
    {
        var entry = new KnowledgeEntry { Id = 4, Title = "Edited" };
        using var editor = CreateEditor(entry);

        OriginKindBox(editor).SelectedIndex = index;
        OriginLocationBox(editor).Text = kind == ProjectOriginKind.LocalFolder
            ? @"C:\projects\app"
            : "https://example.com/origin";

        ApplyToEntry(editor);

        Assert.Equal(kind, entry.ProjectOriginKind);
        Assert.Equal(
            kind == ProjectOriginKind.LocalFolder
                ? @"C:\projects\app"
                : "https://example.com/origin",
            entry.ProjectOriginLocation);
    }

    [Fact]
    public void SavingWithoutAnOriginLeavesTheEntryWithoutOne()
    {
        var entry = new KnowledgeEntry
        {
            Id = 5,
            Title = "Edited",
            ProjectOriginKind = ProjectOriginKind.WebPage,
            ProjectOriginLocation = "https://example.com/origin",
        };
        using var editor = CreateEditor(entry);

        OriginKindBox(editor).SelectedIndex = 0;
        OriginLocationBox(editor).Text = string.Empty;

        ApplyToEntry(editor);

        Assert.Null(entry.ProjectOriginKind);
        Assert.Null(entry.ProjectOriginLocation);
    }

    [Fact]
    public void TheBrowseActionFillsTheLocation()
    {
        using var editor = CreateEditor(new KnowledgeEntry { Id = 6, Title = "Browse" });
        editor.FolderPicker = () => @"C:\projects\app";

        BrowseOriginFolder(editor);

        Assert.Equal(@"C:\projects\app", OriginLocationBox(editor).Text);
    }

    [Fact]
    public void TheBrowseActionDoesNothingWhenNoFolderIsPicked()
    {
        using var editor = CreateEditor(new KnowledgeEntry { Id = 7, Title = "Browse" });
        editor.FolderPicker = () => null;

        BrowseOriginFolder(editor);

        Assert.Equal(string.Empty, OriginLocationBox(editor).Text);
    }

    private static ComboBoxAdv OriginKindBox(ctlAddEdit editor) =>
        (ComboBoxAdv)FieldOf(editor, "_originKind");

    private static TextBoxExt OriginLocationBox(ctlAddEdit editor) =>
        (TextBoxExt)FieldOf(editor, "_originLocation");

    private static void ApplyToEntry(ctlAddEdit editor)
    {
        typeof(ctlAddEdit)
            .GetMethod("ApplyToEntry", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, null);
    }

    private static void BrowseOriginFolder(ctlAddEdit editor)
    {
        typeof(ctlAddEdit)
            .GetMethod("BrowseOriginFolder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(editor, null);
    }

    private static ctlAddEdit CreateEditor(KnowledgeEntry entry)
    {
        var editor = new ctlAddEdit(
            new FakeEntryService(),
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(entry))
        {
            Size = new Size(1200, 800),
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
