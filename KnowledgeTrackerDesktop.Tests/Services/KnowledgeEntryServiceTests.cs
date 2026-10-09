using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.Tests.Data;
using SQLite;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Services
{
    /// <summary>
    /// Service-level integration tests over an isolated database
    /// (TASKS #16: do not mock SQLite where a real test database works).
    /// </summary>
    public class KnowledgeEntryServiceTests : IAsyncLifetime
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
        public async Task SaveAsync_PersistsEntryWithChildren()
        {
            var entry = new KnowledgeEntry { Title = "Service entry" };
            entry.Steps.Add(new Step { Description = "one" });
            entry.Attachments.Add(new AttachmentInfo
            {
                FileName = "a.txt",
                FilePath = @"C:\a.txt",
            });

            await _service.SaveAsync(entry);

            var loaded = await _service.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Service entry", loaded!.Title);
            Assert.Single(loaded.Steps);
            Assert.Single(loaded.Attachments);
        }

        [Fact]
        public async Task SaveAsync_RemovedSteps_AreDeletedFromDatabase()
        {
            var entry = new KnowledgeEntry { Title = "Service steps entry" };
            entry.Steps.Add(new Step { Description = "keep" });
            entry.Steps.Add(new Step { Description = "remove" });
            await _service.SaveAsync(entry);

            entry.Steps.RemoveAll(step => step.Description == "remove");
            await _service.SaveAsync(entry);

            var loaded = await _service.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Equal(new[] { "keep" }, loaded!.Steps.Select(step => step.Description).ToArray());
            Assert.Equal(1, await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = ?", entry.Id));
        }

        [Fact]
        public async Task SaveAsync_EmptyStepsCollection_RemovesAllPersistedSteps()
        {
            var entry = new KnowledgeEntry { Title = "Empty steps entry" };
            entry.Steps.Add(new Step { Description = "one" });
            entry.Steps.Add(new Step { Description = "two" });
            await _service.SaveAsync(entry);

            entry.Steps.Clear();
            await _service.SaveAsync(entry);

            var loaded = await _service.GetByIdAsync(entry.Id);
            Assert.NotNull(loaded);
            Assert.Empty(loaded!.Steps);
            Assert.Equal(0, await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = ?", entry.Id));
        }

        [Fact]
        public async Task SaveAsync_EmptyTitle_ThrowsAndPersistsNothing()
        {
            var entry = new KnowledgeEntry { Title = "   " };

            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveAsync(entry));

            var count = await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"KnowledgeEntry\"");
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task SaveAsync_NullEntry_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.SaveAsync(null!));
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntryAndChildren()
        {
            var entry = new KnowledgeEntry { Title = "Doomed" };
            entry.Steps.Add(new Step { Description = "s" });
            entry.Attachments.Add(new AttachmentInfo { FileName = "f.txt", FilePath = @"C:\f.txt" });
            await _service.SaveAsync(entry);

            await _service.DeleteAsync(entry.Id);

            Assert.Null(await _service.GetByIdAsync(entry.Id));
            Assert.Equal(0, await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = ?", entry.Id));
            Assert.Equal(0, await _connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"AttachmentInfo\" WHERE \"KnowledgeEntryId\" = ?", entry.Id));
        }

        [Fact]
        public async Task DeleteAsync_UnknownId_DoesNotThrow()
        {
            await _service.DeleteAsync(999);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_PersistsImmediately_WithoutTouchingSteps()
        {
            var entry = new KnowledgeEntry { Title = "Favoritable" };
            entry.Steps.Add(new Step { Description = "keep me" });
            entry.Attachments.Add(new AttachmentInfo { FileName = "keep.txt", FilePath = @"C:\keep.txt" });
            await _service.SaveAsync(entry);

            // Simulates a main-list card: only the Id is known, children are
            // NOT loaded. The favorite update must not rewrite the aggregate.
            var detached = new KnowledgeEntry { Id = entry.Id, Title = "Favoritable" };

            await _service.ToggleFavoriteAsync(detached);

            Assert.True(detached.IsFavorite);
            var reloaded = await _service.GetByIdAsync(entry.Id);
            Assert.NotNull(reloaded);
            Assert.True(reloaded!.IsFavorite);
            Assert.Single(reloaded.Steps);
            Assert.Equal("keep me", reloaded.Steps[0].Description);
            Assert.Single(reloaded.Attachments);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_TogglesInBothDirections()
        {
            var entry = new KnowledgeEntry { Title = "Toggle" };
            await _service.SaveAsync(entry);
            Assert.False(entry.IsFavorite);

            await _service.ToggleFavoriteAsync(entry);
            Assert.True(entry.IsFavorite);
            Assert.True((await _service.GetByIdAsync(entry.Id))!.IsFavorite);

            await _service.ToggleFavoriteAsync(entry);
            Assert.False(entry.IsFavorite);
            Assert.False((await _service.GetByIdAsync(entry.Id))!.IsFavorite);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_UnsavedEntry_ThrowsWithoutPersisting()
        {
            var entry = new KnowledgeEntry { Title = "Not saved" };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ToggleFavoriteAsync(entry));
        }

        [Fact]
        public async Task ToggleFavoriteAsync_StaleEntryThrowsAndDoesNotChangeInMemoryState()
        {
            var entry = new KnowledgeEntry { Title = "Will be removed" };
            await _service.SaveAsync(entry);
            await _service.DeleteAsync(entry.Id);
            var stale = new KnowledgeEntry { Id = entry.Id, IsFavorite = false };

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.ToggleFavoriteAsync(stale));

            Assert.False(stale.IsFavorite);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEntriesWithChildren()
        {
            var first = new KnowledgeEntry { Title = "First" };
            first.Steps.Add(new Step { Description = "f" });
            var second = new KnowledgeEntry { Title = "Second" };
            await _service.SaveAsync(first);
            await _service.SaveAsync(second);

            var all = await _service.GetAllAsync();

            Assert.Equal(2, all.Count);
            Assert.Single(all.Single(e => e.Title == "First").Steps);
            Assert.Empty(all.Single(e => e.Title == "Second").Steps);
        }

        [Fact]
        public async Task GetByIdAsync_UnknownId_ReturnsNull()
        {
            Assert.Null(await _service.GetByIdAsync(4242));
        }
    }
}
