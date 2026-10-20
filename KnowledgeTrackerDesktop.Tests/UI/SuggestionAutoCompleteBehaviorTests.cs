using System.Data;
using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI;

/// <summary>
/// Guards the behaviour of the Tags/Technologies suggestion dropdowns against
/// the real Syncfusion AutoComplete routines: which values are offered while
/// typing and what happens when one is picked. These are the exact scenarios
/// that regressed because the component only raises MatchItem in Manual match
/// mode, only honours AutoCompleteCustomize when the handler marks it handled,
/// and always replaces the whole field text with the selected value.
/// </summary>
public sealed class SuggestionAutoCompleteBehaviorTests
{
    [Fact]
    public async Task ComponentIsConfiguredForManualMatching()
    {
        var (form, _, autoComplete) = await CreateEditorAsync();
        using (form)
        {
            // Automatic mode filters the list internally and never raises
            // MatchItem, so the "do not offer values already in the field"
            // rule would be silently ignored.
            Assert.Equal(AutoCompleteMatchModes.Manual, autoComplete.MatchMode);
        }
    }

    [Fact]
    public async Task AlreadyUsedValuesAreNotOfferedAgain()
    {
        var (form, tags, autoComplete) = await CreateEditorAsync();
        using (form)
        {
            Assert.Equal(
                new[] { ".Net", "SqLite", "Blazor" },
                Offered(autoComplete, tags, "Dapper, "));

            Assert.Equal(
                new[] { ".Net", "Blazor" },
                Offered(autoComplete, tags, "Dapper, SqLite, "));
        }
    }

    [Fact]
    public async Task OnlyValuesMatchingTheTokenBeingTypedAreOffered()
    {
        var (form, tags, autoComplete) = await CreateEditorAsync();
        using (form)
        {
            Assert.Equal(new[] { "SqLite" }, Offered(autoComplete, tags, "Dapper, s"));
            Assert.Equal(new[] { "Blazor" }, Offered(autoComplete, tags, "Dapper, b"));

            // The only value starting with "D" is already in the field.
            Assert.Empty(Offered(autoComplete, tags, "Dapper, D"));

            Assert.Equal(new[] { "SqLite" }, Offered(autoComplete, tags, "Sq"));
        }
    }

    [Fact]
    public async Task TheCustomizeHookTellsTheComponentWhichTokenToMatch()
    {
        var (form, tags, autoComplete) = await CreateEditorAsync();
        using (form)
        {
            string? token = null;
            var handled = false;
            autoComplete.AutoCompleteCustomize += (_, e) =>
            {
                handled = e.Handled;
                token = e.TextForAutoCompletion;
            };

            tags.Text = "Dapper, sq";
            autoComplete.ProcessAutoComplete(tags.Text);

            // The component only applies TextForAutoCompletion when the
            // handler marks the event handled; without it the whole field text
            // is matched and nothing is found from the second value on.
            Assert.True(handled);
            Assert.Equal("sq", token);

            // An empty token (caret right after a separator) is reported as a
            // blank so the component still runs the matching routine.
            tags.Text = "Dapper, ";
            autoComplete.ProcessAutoComplete(tags.Text);

            Assert.True(handled);
            Assert.Equal(" ", token);
        }
    }

    [Fact]
    public async Task PickingAValueKeepsThePreviousOnesAndAppendsASeparator()
    {
        var (form, tags, autoComplete) = await CreateEditorAsync();
        using (form)
        {
            Assert.Equal("Dapper, SqLite, ", Picked(autoComplete, tags, "Dapper, ", "SqLite"));

            // Picking several values in a row keeps every one of them.
            Assert.Equal(
                "Dapper, SqLite, Blazor, ",
                Picked(autoComplete, tags, "Dapper, SqLite, ", "Blazor"));

            Assert.Equal("Dapper, SqLite, ", Picked(autoComplete, tags, "Dapper, sql", "SqLite"));

            // A semicolon-styled list keeps receiving semicolons.
            Assert.Equal("Dapper; Blazor; ", Picked(autoComplete, tags, "Dapper; ", "Blazor"));
        }
    }

    private static async Task<(Form Form, TextBoxExt Tags, AutoComplete AutoComplete)> CreateEditorAsync()
    {
        var service = new FakeEntryService
        {
            Tags = new[] { "Dapper", ".Net", "SqLite", "Blazor" },
            Technologies = new[] { "C#", "Blazor" },
        };

        // The component builds its match table from the control's
        // BindingContext, so the editor has to live inside a form (exactly as
        // it does in the app) for suggestions to work at all.
        var form = new Form { Size = new Size(1280, 900), ShowInTaskbar = false };
        var editor = new ctlAddEdit(
            service,
            new FakeNavigation(),
            new FakeShellHost(),
            new EntryEditorRequest(new KnowledgeEntry { Id = 1, Title = "suggestions" }))
        {
            Dock = DockStyle.Fill,
        };
        form.Controls.Add(editor);
        form.PerformLayout();
        await editor.OnAppearingAsync();

        var tags = (TextBoxExt)FieldOf(editor, "_tags");
        var autoComplete = PrivateField<AutoComplete>(editor, "_tagsAutoComplete");

        // The component writes a picked value to its ActiveFocusControl, which
        // it fills from the field's focus events. The user has focus in the
        // field while typing; the test form is never shown, so mirror it here.
        autoComplete.ActiveFocusControl = tags;

        return (form, tags, autoComplete);
    }

    /// <summary>
    /// The values the component would list for the field text, using its own
    /// matching routine (which consults the MatchItem handler).
    /// </summary>
    private static string[] Offered(AutoComplete autoComplete, TextBoxExt tags, string text)
    {
        tags.Text = text;
        var token = CurrentToken(text);

        var method = typeof(AutoComplete).GetMethod(
            "PopulateListWithMatches",
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[]
            {
                typeof(string),
                typeof(DataView).MakeByRefType(),
                typeof(bool),
                typeof(DataRow).MakeByRefType(),
            },
            modifiers: null)!;

        var arguments = new object?[] { token.Length > 0 ? token : " ", null, false, null };
        method.Invoke(autoComplete, arguments);
        var matches = Assert.IsType<DataView>(arguments[1]);
        var table = matches.Table ?? throw new InvalidOperationException("The component did not build a match table.");
        return table.Rows
            .Cast<DataRow>()
            .Select(row => Convert.ToString(row[0]) ?? string.Empty)
            .ToArray();
    }

    /// <summary>
    /// Picks a value the way the component does when the popup closes: it
    /// raises AutoCompleteItemSelected and writes the resulting value to the
    /// control. Returns the text that ends up in the field.
    /// </summary>
    private static string Picked(AutoComplete autoComplete, TextBoxExt tags, string text, string value)
    {
        tags.Text = text;

        var raise = typeof(AutoComplete).GetMethod(
            "RaiseAutoCompleteItemSelectedEvent",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var arguments = new object?[] { new object[] { value }, 0, value, string.Empty };
        var handled = (bool)raise.Invoke(autoComplete, arguments)!;
        var applied = (string?)arguments[3] ?? string.Empty;

        typeof(AutoComplete)
            .GetMethod("SetSelectedValue", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(autoComplete, new object?[] { handled && applied.Length > 0 ? applied : value });

        return tags.Text;
    }

    private static string CurrentToken(string text)
    {
        var index = text.LastIndexOfAny(new[] { ',', ';' });
        return index < 0 ? text : text[(index + 1)..];
    }

    private static Control FieldOf(object instance, string name) => (Control)instance
        .GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(instance)!;

    private static T PrivateField<T>(object instance, string name) => (T)instance
        .GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetValue(instance)!;

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
