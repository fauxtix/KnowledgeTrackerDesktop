using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;
using SQLite;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Data
{
    /// <summary>
    /// Integration tests for the aggregate persistence rules described in
    /// ARCHITECTURE.md sections 7-9, TASKS #1-#2 and CODING_STANDARDS
    /// sections 7-10: save/delete are atomic and children never become
    /// orphaned, because the schema has no foreign-key cascade.
    /// </summary>
    public class KnowledgeRepositorySQLiteTests : IAsyncLifetime
    {
        private TempDatabase _db = null!;
        private SQLiteAsyncConnection _connection = null!;
        private KnowledgeRepositorySQLite _repository = null!;

        public async Task InitializeAsync()
        {
            _db = new TempDatabase();
            _connection = _db.OpenConnection();
            await new SchemaMigrator(_db.DatabasePath).InitializeAsync();
            _repository = new KnowledgeRepositorySQLite(_connection);
        }

        public async Task DisposeAsync()
        {
            await _connection.CloseAsync();
            _db.Dispose();
        }

        [Fact]
        public async Task SaveAsync_NewEntry_PersistsAggregate()
        {
            var entry = new KnowledgeEntry
            {
                Title = "New entry",
                Description = "Details",
                ProjectName = "Project",
                IsFavorite = true,
                Tags = "a,b",
            };
            entry.Steps.Add(new Step { Description = "step one" });
            entry.Steps.Add(new Step { Description = "step two" });
            entry.Attachments.Add(new AttachmentInfo
            {
                FileName = "file.txt",
                FilePath = @"C:\tmp\file.txt",
                DateAdded = new DateTime(2026, 10, 8),
            });

            await _repository.SaveAsync(entry);

            Assert.NotEqual(0, entry.Id);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal("New entry", loaded!.Title);
            Assert.True(loaded.IsFavorite);
            Assert.Equal(2, loaded.Steps.Count);
            Assert.Single(loaded.Attachments);
            Assert.Equal("file.txt", loaded.Attachments[0].FileName);
            Assert.All(loaded.Steps, s => Assert.Equal(entry.Id, s.KnowledgeEntryId));

            var listed = Assert.Single(await _repository.GetAllAsync());
            Assert.Equal(2, listed.Steps.Count);
            Assert.Equal(entry.Id, listed.Id);
        }

        [Fact]
        public async Task SaveAsync_PreservesMarkdownAndCodeWhitespace()
        {
            const string description = "\n  ## Heading  \n\n    indented line\ntrailing spaces  \n";
            const string code = "\n\tif (ready)\n\t{\n\t    Run();  \n\t}\n\n";
            var entry = new KnowledgeEntry
            {
                Title = "Whitespace fidelity",
                Description = description,
                CodeSnippet = code,
            };

            await _repository.SaveAsync(entry);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal(description, loaded!.Description);
            Assert.Equal(code, loaded.CodeSnippet);
        }

        [Fact]
        public async Task SetFavoriteAsync_MissingEntryThrows()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _repository.SetFavoriteAsync(9999, true));
        }

        [Fact]
        public async Task SaveAsync_AddedStep_IsPersisted()
        {
            var entry = new KnowledgeEntry { Title = "Step add entry" };
            entry.Steps.Add(new Step { Description = "A" });
            await _repository.SaveAsync(entry);

            entry.Steps.Add(new Step { Description = "B" });
            await _repository.SaveAsync(entry);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal(new[] { "A", "B" }, loaded!.Steps.Select(s => s.Description).ToArray());

            var rows = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = ?", entry.Id);
            Assert.Equal(2, rows);
        }

        [Fact]
        public async Task SaveAsync_RemovedStep_IsDeletedFromDatabase()
        {
            var entry = new KnowledgeEntry { Title = "Steps entry" };
            entry.Steps.Add(new Step { Description = "A" });
            entry.Steps.Add(new Step { Description = "B" });
            entry.Steps.Add(new Step { Description = "C" });
            await _repository.SaveAsync(entry);

            var stepBId = entry.Steps.Single(s => s.Description == "B").Id;

            entry.Steps.RemoveAll(s => s.Description == "B");
            await _repository.SaveAsync(entry);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal(new[] { "A", "C" }, loaded!.Steps.Select(s => s.Description).ToArray());

            var orphanedRow = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"Step\" WHERE \"Id\" = ?", stepBId);
            Assert.Equal(0, orphanedRow);
        }

        [Fact]
        public async Task SaveAsync_ReorderedSteps_PersistNewSequence()
        {
            var entry = new KnowledgeEntry { Title = "Reorder entry" };
            entry.Steps.Add(new Step { Description = "A" });
            entry.Steps.Add(new Step { Description = "B" });
            entry.Steps.Add(new Step { Description = "C" });
            await _repository.SaveAsync(entry);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);

            var c = loaded!.Steps.Single(s => s.Description == "C");
            loaded.Steps.Remove(c);
            loaded.Steps.Insert(0, c);

            await _repository.SaveAsync(loaded);

            var reloaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(
                new[] { "C", "A", "B" },
                reloaded!.Steps.Select(s => s.Description).ToArray());
            Assert.Equal(new[] { 1, 2, 3 }, reloaded.Steps.Select(s => s.Order).ToArray());
        }

        [Fact]
        public async Task SaveAsync_NormalizesStepOrderToSequentialNumbers()
        {
            var entry = new KnowledgeEntry { Title = "Order entry" };
            entry.Steps.Add(new Step { Description = "first", Order = 99 });
            entry.Steps.Add(new Step { Description = "second", Order = 5 });
            entry.Steps.Add(new Step { Description = "third", Order = 7 });

            await _repository.SaveAsync(entry);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal(new[] { 1, 2, 3 }, loaded!.Steps.Select(s => s.Order).ToArray());
        }

        [Fact]
        public async Task SaveAsync_AddedAttachment_IsPersisted()
        {
            var entry = new KnowledgeEntry { Title = "Attachment add entry" };
            await _repository.SaveAsync(entry);

            entry.Attachments.Add(new AttachmentInfo { FileName = "one.txt", FilePath = @"C:\one.txt" });
            await _repository.SaveAsync(entry);

            entry.Attachments.Add(new AttachmentInfo { FileName = "two.txt", FilePath = @"C:\two.txt" });
            await _repository.SaveAsync(entry);

            var rows = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"AttachmentInfo\" WHERE \"KnowledgeEntryId\" = ?", entry.Id);
            Assert.Equal(2, rows);
        }

        [Fact]
        public async Task SaveAsync_RemovedAttachment_RowIsDeletedFromDatabase()
        {
            var entry = new KnowledgeEntry { Title = "Attachment remove entry" };
            entry.Attachments.Add(new AttachmentInfo { FileName = "keep.txt", FilePath = @"C:\keep.txt" });
            entry.Attachments.Add(new AttachmentInfo { FileName = "drop.txt", FilePath = @"C:\drop.txt" });
            await _repository.SaveAsync(entry);

            var dropId = entry.Attachments.Single(a => a.FileName == "drop.txt").Id;

            entry.Attachments.RemoveAll(a => a.FileName == "drop.txt");
            await _repository.SaveAsync(entry);

            var orphanedRow = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"AttachmentInfo\" WHERE \"Id\" = ?", dropId);
            Assert.Equal(0, orphanedRow);

            var reloaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(reloaded);
            Assert.Equal("keep.txt", reloaded!.Attachments.Single().FileName);
        }

        [Fact]
        public async Task SaveAsync_SynchronizesAttachmentsToExactCollection()
        {
            var entry = new KnowledgeEntry { Title = "Attachment entry" };
            entry.Attachments.Add(new AttachmentInfo { FileName = "a.txt", FilePath = @"C:\a.txt" });
            entry.Attachments.Add(new AttachmentInfo { FileName = "b.txt", FilePath = @"C:\b.txt" });
            await _repository.SaveAsync(entry);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal(2, loaded!.Attachments.Count);

            var removed = loaded.Attachments.Single(a => a.FileName == "a.txt");
            loaded.Attachments.Remove(removed);
            loaded.Attachments.Add(new AttachmentInfo { FileName = "c.txt", FilePath = @"C:\c.txt" });

            await _repository.SaveAsync(loaded);

            var reloaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(reloaded);
            Assert.Equal(
                new[] { "b.txt", "c.txt" },
                reloaded!.Attachments.Select(a => a.FileName).OrderBy(n => n).ToArray());
            Assert.All(reloaded.Attachments, a => Assert.Equal(entry.Id, a.KnowledgeEntryId));
        }

        [Fact]
        public async Task SaveAsync_ExistingEntry_UpdatesWithoutDuplicating()
        {
            var entry = new KnowledgeEntry { Title = "Original title" };
            await _repository.SaveAsync(entry);

            entry.Title = "Updated title";
            await _repository.SaveAsync(entry);

            // Saving again without changes must not create a second row.
            await _repository.SaveAsync(entry);

            var count = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"KnowledgeEntry\"");
            Assert.Equal(1, count);

            var loaded = await _repository.GetByIdAsync(entry.Id);
            Assert.Equal("Updated title", loaded!.Title);
        }

        [Fact]
        public async Task GetByIdAsync_UnknownId_ReturnsNull()
        {
            Assert.Null(await _repository.GetByIdAsync(999));
        }

        [Fact]
        public async Task GetByIdAsync_EntryWithoutChildren_ReturnsEmptyCollections()
        {
            var entry = new KnowledgeEntry { Title = "Childless" };
            await _repository.SaveAsync(entry);

            var loaded = await _repository.GetByIdAsync(entry.Id);

            Assert.NotNull(loaded);
            Assert.Empty(loaded!.Steps);
            Assert.Empty(loaded.Attachments);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntriesWithTheirOwnChildren()
        {
            var first = new KnowledgeEntry { Title = "First" };
            first.Steps.Add(new Step { Description = "f1" });
            var second = new KnowledgeEntry { Title = "Second" };
            second.Steps.Add(new Step { Description = "s1" });
            second.Steps.Add(new Step { Description = "s2" });
            second.Attachments.Add(new AttachmentInfo { FileName = "s.txt", FilePath = @"C:\s.txt" });

            await _repository.SaveAsync(first);
            await _repository.SaveAsync(second);

            var all = await _repository.GetAllAsync();

            Assert.Equal(2, all.Count);
            Assert.Single(all.Single(e => e.Title == "First").Steps);
            Assert.Equal(2, all.Single(e => e.Title == "Second").Steps.Count);
            Assert.Single(all.Single(e => e.Title == "Second").Attachments);
            Assert.Empty(all.Single(e => e.Title == "First").Attachments);
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntryAndChildren_ButNotOtherEntries()
        {
            var doomed = new KnowledgeEntry { Title = "Doomed" };
            doomed.Steps.Add(new Step { Description = "d1" });
            doomed.Attachments.Add(new AttachmentInfo { FileName = "d.txt", FilePath = @"C:\d.txt" });
            var survivor = new KnowledgeEntry { Title = "Survivor" };
            survivor.Steps.Add(new Step { Description = "keep" });

            await _repository.SaveAsync(doomed);
            await _repository.SaveAsync(survivor);
            var doomedId = doomed.Id;
            var survivorId = survivor.Id;

            await _repository.DeleteAsync(doomedId);

            Assert.Null(await _repository.GetByIdAsync(doomedId));

            Assert.Equal(0, await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = ?", doomedId));
            Assert.Equal(0, await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"AttachmentInfo\" WHERE \"KnowledgeEntryId\" = ?", doomedId));

            // Unrelated data untouched (no over-deletion).
            var remaining = await _repository.GetByIdAsync(survivorId);
            Assert.NotNull(remaining);
            Assert.Equal("keep", remaining!.Steps.Single().Description);
        }

        [Fact]
        public async Task SaveAsync_WhenStepInsertFails_TransactionRollsBack()
        {
            var entry = new KnowledgeEntry { Title = "original title" };
            entry.Steps.Add(new Step { Description = "A" });
            await _repository.SaveAsync(entry);

            // Force a failure inside the transaction on the next save.
            await _connection.ExecuteAsync(
                "CREATE TRIGGER \"Step_RejectFail\" BEFORE INSERT ON \"Step\" " +
                "WHEN NEW.\"Description\" = 'FAIL' " +
                "BEGIN SELECT RAISE(ABORT, 'forced failure'); END");

            entry.Title = "changed title";
            entry.Steps.Add(new Step { Description = "FAIL" });

            await Assert.ThrowsAnyAsync<Exception>(() => _repository.SaveAsync(entry));

            // Nothing from the failed operation may remain:
            var reloaded = await _repository.GetByIdAsync(entry.Id);
            Assert.NotNull(reloaded);
            Assert.Equal("original title", reloaded!.Title);
            Assert.Single(reloaded.Steps);
            Assert.Equal("A", reloaded.Steps[0].Description);

            var entryCount = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"KnowledgeEntry\"");
            Assert.Equal(1, entryCount);
        }

        [Fact]
        public async Task DeleteAsync_MissingEntry_DoesNotThrow()
        {
            await _repository.DeleteAsync(12345);
        }

        [Fact]
        public async Task GetAllTagsAsync_DeduplicatesAcrossEntries_AndOrdersByFrequency()
        {
            await _repository.SaveAsync(new KnowledgeEntry
            {
                Title = "first",
                Tags = "sqlite;winforms",
            });
            await _repository.SaveAsync(new KnowledgeEntry
            {
                Title = "second",
                Tags = "sqlite, SQLite;di",
            });

            var tags = await _repository.GetAllTagsAsync();

            // 'sqlite' appears three times (case-insensitive), 'winforms' and
            // 'di' once; ties break alphabetically and the first spelling wins.
            Assert.Equal(new[] { "sqlite", "di", "winforms" }, tags);
        }

        [Fact]
        public async Task GetAllTechnologiesAsync_ReturnsDistinctValues_EmptyWhenNoEntries()
        {
            Assert.Empty(await _repository.GetAllTechnologiesAsync());

            await _repository.SaveAsync(new KnowledgeEntry
            {
                Title = "technologies",
                Technologies = "C#;C#; WinForms",
            });

            Assert.Equal(new[] { "C#", "WinForms" }, await _repository.GetAllTechnologiesAsync());
        }
    }
}
