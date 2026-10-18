using KnowledgeTrackerDesktop.Core;
using SQLite;

namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// Creates/updates the schema additively. A Step table without its expected
    /// primary key is rebuilt transactionally after backing up the existing file.
    /// A timestamped backup is also taken before raising the schema version.
    /// A database written by a newer application version is never modified.
    /// </summary>
    public sealed class SchemaMigrator
    {
        /// <summary>
        /// Version written to PRAGMA user_version.
        /// Increment only when a real schema change is introduced together with
        /// its migration and tests.
        /// </summary>
        public const int CurrentVersion = 1;

        private readonly DatabasePath _databasePath;

        public SchemaMigrator(DatabasePath databasePath)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
        }

        public async Task InitializeAsync()
        {
            var path = _databasePath.FilePath;
            _databasePath.CopyLegacyDatabaseIfNeeded();

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var databaseExists = File.Exists(path);
            var version = 0;
            var malformedStepTable = false;
            var hasOrphanedChildren = false;
            if (databaseExists)
            {
                version = await ReadUserVersionAsync(path);

                // A database written by a newer application version must be left
                // untouched: no backup, no repair, no orphan cleanup and no
                // user_version rewrite (which would silently downgrade it).
                if (version > CurrentVersion)
                {
                    throw new SchemaVersionTooNewException(version, CurrentVersion);
                }

                malformedStepTable = await HasMalformedStepTableAsync(path);
                hasOrphanedChildren = await HasOrphanedChildrenAsync(path);
            }

            string? backupPath = null;
            if (databaseExists &&
                (version < CurrentVersion || malformedStepTable || hasOrphanedChildren))
            {
                // Required before any operation that may alter or repair data.
                // A failure here aborts the migration via DatabaseBackupException.
                backupPath = CreateBackup(path);
            }

            var connection = new SQLiteAsyncConnection(path);
            try
            {
                // CreateTables is additive: existing tables keep their data,
                // missing columns are added, existing columns are untouched.
                await connection.CreateTableAsync<KnowledgeEntry>();
                if (malformedStepTable)
                {
                    await connection.RunInTransactionAsync(RebuildStepTable);
                }
                await connection.CreateTableAsync<Step>();
                await connection.CreateTableAsync<AttachmentInfo>();

                await connection.RunInTransactionAsync(conn =>
                {
                    conn.Execute(
                        "DELETE FROM \"Step\" WHERE NOT EXISTS (" +
                        "SELECT 1 FROM \"KnowledgeEntry\" " +
                        "WHERE \"KnowledgeEntry\".\"Id\" = \"Step\".\"KnowledgeEntryId\")");
                    conn.Execute(
                        "DELETE FROM \"AttachmentInfo\" WHERE NOT EXISTS (" +
                        "SELECT 1 FROM \"KnowledgeEntry\" " +
                        "WHERE \"KnowledgeEntry\".\"Id\" = \"AttachmentInfo\".\"KnowledgeEntryId\")");
                    conn.Execute($"PRAGMA user_version = {CurrentVersion}");
                });
            }
            catch (Exception ex)
            {
                // Stop instead of starting with a partially repaired database;
                // when a safety copy exists, point the caller at it.
                throw new SchemaMigrationException(backupPath, ex);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task<int> ReadUserVersionAsync(string path)
        {
            var connection = new SQLiteAsyncConnection(path);
            try
            {
                return await connection.ExecuteScalarAsync<int>("PRAGMA user_version");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task<bool> HasMalformedStepTableAsync(string path)
        {
            var connection = new SQLiteAsyncConnection(path);
            try
            {
                var exists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Step'");
                if (exists == 0)
                {
                    return false;
                }

                var columns = await connection.QueryAsync<TableColumn>("PRAGMA table_info(\"Step\")");
                var primaryKeyColumns = columns.Where(column => column.pk > 0).ToList();
                return primaryKeyColumns.Count != 1 ||
                    !string.Equals(primaryKeyColumns[0].name, "Id", StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task<bool> HasOrphanedChildrenAsync(string path)
        {
            var connection = new SQLiteAsyncConnection(path);
            try
            {
                var hasEntries = await TableExistsAsync(connection, "KnowledgeEntry");
                if (await TableExistsAsync(connection, "Step"))
                {
                    var orphanedSteps = hasEntries
                        ? await connection.ExecuteScalarAsync<int>(
                            "SELECT COUNT(*) FROM \"Step\" child " +
                            "WHERE NOT EXISTS (SELECT 1 FROM \"KnowledgeEntry\" parent " +
                            "WHERE parent.\"Id\" = child.\"KnowledgeEntryId\")")
                        : await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\"");
                    if (orphanedSteps > 0)
                    {
                        return true;
                    }
                }

                if (!await TableExistsAsync(connection, "AttachmentInfo"))
                {
                    return false;
                }

                var orphanedAttachments = hasEntries
                    ? await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM \"AttachmentInfo\" child " +
                        "WHERE NOT EXISTS (SELECT 1 FROM \"KnowledgeEntry\" parent " +
                        "WHERE parent.\"Id\" = child.\"KnowledgeEntryId\")")
                    : await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"AttachmentInfo\"");
                return orphanedAttachments > 0;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task<bool> TableExistsAsync(SQLiteAsyncConnection connection, string table) =>
            await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", table) > 0;

        private static void RebuildStepTable(SQLiteConnection connection)
        {
            const string temporaryTable = "__KnowledgeTracker_Step_rebuild";
            var columns = connection.Query<TableColumn>("PRAGMA table_info(\"Step\")");
            var columnNames = columns.Select(column => column.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var requiredColumns = new[] { "Description", "Order", "KnowledgeEntryId" };
            if (requiredColumns.Any(column => !columnNames.Contains(column)))
            {
                throw new InvalidDataException(
                    "The existing Step table is missing columns required to safely repair its primary key.");
            }

            var tempExists = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", temporaryTable);
            if (tempExists > 0)
            {
                throw new InvalidDataException(
                    $"Cannot repair the Step table because '{temporaryTable}' already exists.");
            }

            var rowCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM \"Step\"");
            var hasUniqueIds = columnNames.Contains("Id") &&
                connection.ExecuteScalar<int>(
                    "SELECT CASE WHEN COUNT(*) = COUNT(DISTINCT \"Id\") " +
                    "AND COUNT(*) = COUNT(\"Id\") THEN 1 ELSE 0 END FROM \"Step\"") == 1;

            connection.Execute($"ALTER TABLE \"Step\" RENAME TO \"{temporaryTable}\"");
            connection.CreateTable<Step>();

            var insertColumns = hasUniqueIds
                ? "\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\""
                : "\"Description\", \"Order\", \"KnowledgeEntryId\"";
            var selectColumns = hasUniqueIds
                ? insertColumns
                : "\"Description\", \"Order\", \"KnowledgeEntryId\"";
            connection.Execute(
                $"INSERT INTO \"Step\" ({insertColumns}) " +
                $"SELECT {selectColumns} FROM \"{temporaryTable}\"");

            var rebuiltCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM \"Step\"");
            if (rebuiltCount != rowCount)
            {
                throw new InvalidDataException(
                    $"Step table repair copied {rebuiltCount} of {rowCount} records.");
            }

            connection.Execute($"DROP TABLE \"{temporaryTable}\"");
        }

        /// <summary>
        /// Writes a timestamped safety copy next to the database and returns its
        /// path. Uses SQLite's backup API (as <see cref="DatabasePath"/> does for
        /// the legacy copy) so the snapshot stays consistent even if the file is
        /// open or a rollback journal/WAL exists. An existing backup is never
        /// overwritten: any failure surfaces as <see cref="DatabaseBackupException"/>.
        /// </summary>
        private static string CreateBackup(string path)
        {
            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var backupPath = Path.Combine(directory, $"{name}.backup.{timestamp}{extension}");

            if (File.Exists(backupPath))
            {
                throw new DatabaseBackupException(
                    backupPath,
                    new IOException($"A backup already exists at '{backupPath}'."));
            }

            try
            {
                using var source = new SQLiteConnection(path);
                source.Backup(backupPath, "main");
            }
            catch (Exception ex)
            {
                throw new DatabaseBackupException(backupPath, ex);
            }

            return backupPath;
        }

        private sealed class TableColumn
        {
            public int cid { get; set; }
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public int notnull { get; set; }
            public string? dflt_value { get; set; }
            public int pk { get; set; }
        }
    }
}
