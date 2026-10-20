using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.Tests.Data;
using SQLite;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Services
{
    /// <summary>
    /// Export/import round trips over a real database (TASKS #16: prefer a real
    /// test database over mocking SQLite). These cover the portability rules:
    /// an export is a faithful document, an import never overwrites local data,
    /// duplicates are reported instead of silently created, and entries that do
    /// not satisfy the entry rules are counted and skipped.
    /// </summary>
    public class KnowledgeExportImportTests : IAsyncLifetime
    {
        private TempDatabase _db = null!;
        private SQLiteAsyncConnection _connection = null!;
        private KnowledgeEntryService _service = null!;

        public async Task InitializeAsync()
        {
            _db = new TempDatabase();
            _connection = _db.OpenConnection();
            await new SchemaMigrator(_db.DatabasePath).InitializeAsync();
            _service = new KnowledgeEntryService(new KnowledgeRepositorySQLite(_connection));
        }

        public async Task DisposeAsync()
        {
            await _connection.CloseAsync();
            _db.Dispose();
        }

        [Fact]
        public async Task ExportAsync_CapturesEveryFieldAndChild()
        {
            var entry = CreateValidEntry("Exported entry");
            entry.CodeSnippet = "var x = 1;";
            entry.Comments = "A comment";
            entry.YouTubeUrl = "https://youtu.be/abc";
            entry.IsFavorite = true;
            entry.Steps.Add(new Step { Description = "first" });
            entry.Steps.Add(new Step { Description = "second" });
            entry.Attachments.Add(new AttachmentInfo
            {
                FileName = "diagram.png",
                FilePath = @"C:\docs\diagram.png",
                DateAdded = new DateTime(2026, 10, 2),
            });
            await _service.SaveAsync(entry);

            var document = await _service.ExportAsync();

            Assert.Equal(KnowledgeExport.CurrentFormatVersion, document.FormatVersion);
            var exported = Assert.Single(document.Entries);
            Assert.Equal("Exported entry", exported.Title);
            Assert.Equal("KnowledgeTracker", exported.ProjectName);
            Assert.Equal("C#; WinForms", exported.Technologies);
            Assert.Equal("sqlite; validation", exported.Tags);
            Assert.Equal("A documented solution.", exported.Description);
            Assert.Equal("var x = 1;", exported.CodeSnippet);
            Assert.Equal("A comment", exported.Comments);
            Assert.Equal("https://youtu.be/abc", exported.YouTubeUrl);
            Assert.True(exported.IsFavorite);
            Assert.Equal(2, exported.Steps.Count);
            Assert.Equal("first", exported.Steps[0].Description);
            Assert.Equal("second", exported.Steps[1].Description);
            var attachment = Assert.Single(exported.Attachments);
            Assert.Equal("diagram.png", attachment.FileName);
            Assert.Equal(@"C:\docs\diagram.png", attachment.FilePath);
            Assert.Equal(new DateTime(2026, 10, 2), attachment.DateAdded);
        }

        [Fact]
        public async Task JsonRoundTrip_PreservesTheDocument()
        {
            var entry = CreateValidEntry("Round trip");
            entry.Steps.Add(new Step { Description = "one" });
            entry.Attachments.Add(new AttachmentInfo
            {
                FileName = "a.txt",
                FilePath = @"C:\a.txt",
            });
            await _service.SaveAsync(entry);

            var document = await _service.ExportAsync();
            var parsed = KnowledgeExport.FromJson(document.ToJson());

            var exported = Assert.Single(parsed.Entries);
            Assert.Equal("Round trip", exported.Title);
            Assert.Single(exported.Steps);
            Assert.Single(exported.Attachments);
        }

        [Fact]
        public void FromJson_RejectsGarbageAndNewerFormats()
        {
            Assert.Throws<KnowledgeExportFormatException>(() => KnowledgeExport.FromJson("not json"));
            Assert.Throws<KnowledgeExportFormatException>(() => KnowledgeExport.FromJson(string.Empty));

            var newer = new KnowledgeExport
            {
                FormatVersion = KnowledgeExport.CurrentFormatVersion + 1,
            }.ToJson();

            var error = Assert.Throws<KnowledgeExportFormatException>(
                () => KnowledgeExport.FromJson(newer));
            Assert.True(error.IsNewerFormat);
        }

        [Fact]
        public async Task ImportAsync_WritesEveryEntryAsANewRecord()
        {
            var source = new KnowledgeExport();
            source.Entries.Add(EntryDocument.FromEntry(CreateValidEntry("Imported one")));
            source.Entries.Add(EntryDocument.FromEntry(CreateValidEntry("Imported two")));

            var result = await _service.ImportAsync(source, ImportMode.SkipDuplicates);

            Assert.Equal(2, result.Imported);
            Assert.Equal(0, result.SkippedDuplicates);

            var stored = await _service.GetAllAsync();
            Assert.Equal(2, stored.Count);
            Assert.Contains(stored, entry => entry.Title == "Imported one");
        }

        [Fact]
        public async Task ImportAsync_SkipDuplicates_KeepsTheLocalEntry()
        {
            await _service.SaveAsync(CreateValidEntry("Shared title"));

            var source = new KnowledgeExport();
            var duplicate = EntryDocument.FromEntry(CreateValidEntry("Shared title"));
            duplicate.Description = "Changed description";
            source.Entries.Add(duplicate);
            source.Entries.Add(EntryDocument.FromEntry(CreateValidEntry("Brand new")));

            var result = await _service.ImportAsync(source, ImportMode.SkipDuplicates);

            Assert.Equal(1, result.Imported);
            Assert.Equal(1, result.SkippedDuplicates);

            var stored = await _service.GetAllAsync();
            Assert.Equal(2, stored.Count);
            // The local entry is untouched: the imported description did not win.
            var local = stored.Single(entry => entry.Title == "Shared title");
            Assert.Equal("A documented solution.", local.Description);
        }

        [Fact]
        public async Task ImportAsync_ImportAsCopies_AddsASecondEntry()
        {
            await _service.SaveAsync(CreateValidEntry("Repeated"));

            var source = new KnowledgeExport();
            source.Entries.Add(EntryDocument.FromEntry(CreateValidEntry("Repeated")));

            var result = await _service.ImportAsync(source, ImportMode.ImportAsCopies);

            Assert.Equal(1, result.Imported);
            Assert.Equal(0, result.SkippedDuplicates);

            var stored = await _service.GetAllAsync();
            Assert.Equal(2, stored.Count(entry => entry.Title == "Repeated"));
        }

        [Fact]
        public async Task ImportAsync_SkipDuplicates_DoesNotRepeatWithinTheSameFile()
        {
            var document = EntryDocument.FromEntry(CreateValidEntry("Twice in one file"));
            var source = new KnowledgeExport();
            source.Entries.Add(document);
            source.Entries.Add(document);

            var result = await _service.ImportAsync(source, ImportMode.SkipDuplicates);

            Assert.Equal(1, result.Imported);
            Assert.Equal(1, result.SkippedDuplicates);
            Assert.Single(await _service.GetAllAsync());
        }

        [Fact]
        public async Task ImportAsync_DuplicateMatchIgnoresCaseAndPadding()
        {
            await _service.SaveAsync(CreateValidEntry("Spaced Title"));

            var duplicate = EntryDocument.FromEntry(CreateValidEntry("  spaced title  "));
            var source = new KnowledgeExport();
            source.Entries.Add(duplicate);

            var result = await _service.ImportAsync(source, ImportMode.SkipDuplicates);

            Assert.Equal(0, result.Imported);
            Assert.Equal(1, result.SkippedDuplicates);
        }

        [Fact]
        public async Task ImportAsync_SkipsEntriesThatBreakTheEntryRules()
        {
            var incomplete = EntryDocument.FromEntry(CreateValidEntry("No project"));
            incomplete.ProjectName = null;

            var source = new KnowledgeExport();
            source.Entries.Add(incomplete);
            source.Entries.Add(EntryDocument.FromEntry(CreateValidEntry("Valid")));

            var result = await _service.ImportAsync(source, ImportMode.SkipDuplicates);

            Assert.Equal(1, result.Imported);
            Assert.Equal(1, result.Invalid);
            Assert.Single(await _service.GetAllAsync());
        }

        [Fact]
        public async Task CountDuplicatesAsync_ReportsOnlyEntriesThatAlreadyExist()
        {
            await _service.SaveAsync(CreateValidEntry("Already here"));

            var source = new KnowledgeExport();
            source.Entries.Add(EntryDocument.FromEntry(CreateValidEntry("Already here")));
            source.Entries.Add(EntryDocument.FromEntry(CreateValidEntry("Something new")));

            // The incomplete entry neither exists nor can be imported, so it is
            // not counted as a duplicate.
            var incomplete = EntryDocument.FromEntry(CreateValidEntry("Already here"));
            incomplete.DateResolved = null;
            incomplete.Title = "Broken and existing";
            source.Entries.Add(incomplete);

            Assert.Equal(1, await _service.CountDuplicatesAsync(source));
        }

        private static KnowledgeEntry CreateValidEntry(string title) => new()
        {
            Title = title,
            ProjectName = "KnowledgeTracker",
            Technologies = "C#; WinForms",
            DateResolved = new DateTime(2026, 10, 1),
            Tags = "sqlite; validation",
            Description = "A documented solution.",
        };
    }
}
