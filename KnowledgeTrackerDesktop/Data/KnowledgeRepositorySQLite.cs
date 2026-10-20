using KnowledgeTrackerDesktop.Core;
using SQLite;

namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// SQLite implementation of <see cref="IKnowledgeRepository"/>.
    /// Owns all SQL, transactions and schema-specific handling.
    /// The schema defines no foreign keys, so referential integrity is
    /// enforced here: every multi-table write runs in a single transaction
    /// and child rows are always synchronized explicitly.
    /// </summary>
    public sealed class KnowledgeRepositorySQLite : IKnowledgeRepository
    {
        private readonly SQLiteAsyncConnection _connection;

        public KnowledgeRepositorySQLite(SQLiteAsyncConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public async Task<List<KnowledgeEntry>> GetAllAsync()
        {
            var entries = await _connection.Table<KnowledgeEntry>()
                .OrderBy(e => e.Id)
                .ToListAsync();

            var steps = await _connection.Table<Step>().ToListAsync();
            var attachments = await _connection.Table<AttachmentInfo>().ToListAsync();

            var byId = new Dictionary<int, KnowledgeEntry>();
            foreach (var entry in entries)
            {
                entry.Steps = new List<Step>();
                entry.Attachments = new List<AttachmentInfo>();
                byId[entry.Id] = entry;
            }

            foreach (var step in steps)
            {
                if (byId.TryGetValue(step.KnowledgeEntryId, out var owner))
                {
                    owner.Steps.Add(step);
                }
            }

            foreach (var attachment in attachments)
            {
                if (byId.TryGetValue(attachment.KnowledgeEntryId, out var owner))
                {
                    owner.Attachments.Add(attachment);
                }
            }

            foreach (var entry in entries)
            {
                entry.Steps.Sort((a, b) => a.Order.CompareTo(b.Order));
            }

            return entries;
        }

        public async Task<KnowledgeEntry?> GetByIdAsync(int id)
        {
            var entry = await _connection.Table<KnowledgeEntry>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();

            if (entry is null)
            {
                return null;
            }

            var steps = await _connection.Table<Step>()
                .Where(s => s.KnowledgeEntryId == id)
                .ToListAsync();

            entry.Steps = steps.OrderBy(s => s.Order).ToList();

            entry.Attachments = await _connection.Table<AttachmentInfo>()
                .Where(a => a.KnowledgeEntryId == id)
                .ToListAsync();

            return entry;
        }

        public async Task SaveAsync(KnowledgeEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var steps = entry.Steps ?? new List<Step>();
            var attachments = entry.Attachments ?? new List<AttachmentInfo>();

            await _connection.RunInTransactionAsync(conn =>
            {
                UpsertEntry(conn, entry);

                // No foreign keys / cascade in this schema (ARCHITECTURE section 6):
                // children are explicitly synchronized inside the same transaction.
                conn.Execute(
                    "DELETE FROM \"Step\" WHERE KnowledgeEntryId = ?", entry.Id);
                conn.Execute(
                    "DELETE FROM \"AttachmentInfo\" WHERE KnowledgeEntryId = ?", entry.Id);

                var order = 0;
                foreach (var step in steps)
                {
                    step.KnowledgeEntryId = entry.Id;
                    step.Order = ++order;
                    conn.Insert(step);
                }

                foreach (var attachment in attachments)
                {
                    attachment.KnowledgeEntryId = entry.Id;
                    conn.Insert(attachment);
                }
            });
        }

        public async Task DeleteAsync(int id)
        {
            await _connection.RunInTransactionAsync(conn =>
            {
                // Children first, parent last. No cascade is assumed.
                conn.Execute("DELETE FROM \"Step\" WHERE KnowledgeEntryId = ?", id);
                conn.Execute(
                    "DELETE FROM \"AttachmentInfo\" WHERE KnowledgeEntryId = ?", id);
                conn.Execute("DELETE FROM \"KnowledgeEntry\" WHERE Id = ?", id);
            });
        }

        public async Task SetFavoriteAsync(int id, bool isFavorite)
        {
            var affectedRows = await _connection.ExecuteAsync(
                "UPDATE \"KnowledgeEntry\" SET \"IsFavorite\" = ? WHERE \"Id\" = ?",
                isFavorite,
                id);
            if (affectedRows == 0)
            {
                throw new KeyNotFoundException($"Knowledge entry {id} no longer exists.");
            }
        }

        public Task<IReadOnlyList<string>> GetAllTagsAsync() =>
            GetDistinctListValuesAsync("Tags");

        public Task<IReadOnlyList<string>> GetAllTechnologiesAsync() =>
            GetDistinctListValuesAsync("Technologies");

        public async Task<IReadOnlyList<EntryIdentity>> GetIdentitiesAsync()
        {
            var rows = await _connection.QueryAsync<IdentityRow>(
                "SELECT \"Title\" AS \"Title\", \"ProjectName\" AS \"ProjectName\" FROM \"KnowledgeEntry\"");

            return rows
                .Select(row => EntryIdentity.Of(row.Title, row.ProjectName))
                .ToList();
        }

        public async Task InsertBatchAsync(IReadOnlyList<KnowledgeEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            if (entries.Count == 0)
            {
                return;
            }

            await _connection.RunInTransactionAsync(conn =>
            {
                foreach (var entry in entries)
                {
                    // An import always creates new rows; ids from the document
                    // are not restored, so a local entry is never overwritten.
                    entry.Id = 0;
                    foreach (var step in entry.Steps ?? new List<Step>())
                    {
                        step.Id = 0;
                    }

                    foreach (var attachment in entry.Attachments ?? new List<AttachmentInfo>())
                    {
                        attachment.Id = 0;
                    }

                    UpsertEntry(conn, entry);

                    var order = 0;
                    foreach (var step in entry.Steps ?? new List<Step>())
                    {
                        step.KnowledgeEntryId = entry.Id;
                        step.Order = ++order;
                        conn.Insert(step);
                    }

                    foreach (var attachment in entry.Attachments ?? new List<AttachmentInfo>())
                    {
                        attachment.KnowledgeEntryId = entry.Id;
                        conn.Insert(attachment);
                    }
                }
            });
        }

        /// <summary>
        /// Reads one list-valued column (Tags / Technologies) across all
        /// entries and returns its distinct items ordered by usage frequency
        /// (most used first, then alphabetically), preserving the first seen
        /// spelling. The column is split with <see cref="KnowledgeEntryTags"/>
        /// so both accepted separators are handled.
        /// </summary>
        private async Task<IReadOnlyList<string>> GetDistinctListValuesAsync(string column)
        {
            var rows = await _connection.QueryAsync<ListValueRow>(
                $"SELECT \"{column}\" AS \"Value\" FROM \"KnowledgeEntry\"");

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var spellings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                foreach (var item in KnowledgeEntryTags.Split(row.Value))
                {
                    counts[item] = counts.TryGetValue(item, out var count) ? count + 1 : 1;
                    if (!spellings.ContainsKey(item))
                    {
                        spellings[item] = item;
                    }
                }
            }

            return counts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => spellings[pair.Key])
                .ToList();
        }

        /// <summary>
        /// Single-column projection row (sqlite-net requires a mapped type;
        /// the queried column is aliased to <c>Value</c>).
        /// </summary>
        private sealed class ListValueRow
        {
            public string? Value { get; set; }
        }

        /// <summary>
        /// Two-column projection row for the title/project identity query.
        /// </summary>
        private sealed class IdentityRow
        {
            public string? Title { get; set; }

            public string? ProjectName { get; set; }
        }

        private static void UpsertEntry(SQLiteConnection conn, KnowledgeEntry entry)
        {
            if (entry.Id == 0)
            {
                conn.Insert(entry);
                return;
            }

            if (conn.Update(entry) > 0)
            {
                return;
            }

            // Update affected no rows: re-insert only if the row is truly gone,
            // otherwise the entry was saved unchanged.
            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM \"KnowledgeEntry\" WHERE Id = ?", entry.Id);

            if (exists == 0)
            {
                conn.Insert(entry);
            }
        }
    }
}
