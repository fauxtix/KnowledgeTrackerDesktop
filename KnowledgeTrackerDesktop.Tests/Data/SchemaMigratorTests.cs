using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;
using SQLite;
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Data
{
    public class SchemaMigratorTests
    {
        private const string LegacyKnowledgeEntryDdl = """
            CREATE TABLE "KnowledgeEntry" (
            "Id" integer primary key autoincrement not null ,
            "Title" varchar ,
            "Description" varchar ,
            "DateResolved" bigint ,
            "ProjectName" varchar ,
            "ResolutionSteps" varchar ,
            "Technologies" varchar ,
            "Tags" varchar ,
            "Comments" varchar ,
            "CodeSnippet" varchar ,
            "IsFavorite" integer ,
            "YouTubeUrl" varchar )
            """;

        private const string LegacyStepDdl = """
            CREATE TABLE "Step" (
            "Id" integer primary key autoincrement not null ,
            "Description" varchar ,
            "Order" integer ,
            "KnowledgeEntryId" integer )
            """;

        private const string StepWithoutPrimaryKeyDdl = """
            CREATE TABLE "Step" (
            "Id" integer ,
            "Description" varchar ,
            "Order" integer ,
            "KnowledgeEntryId" integer )
            """;

        private const string LegacyAttachmentInfoDdl = """
            CREATE TABLE "AttachmentInfo" (
            "Id" integer primary key autoincrement not null ,
            "KnowledgeEntryId" integer ,
            "FileName" varchar ,
            "FilePath" varchar ,
            "DateAdded" bigint )
            """;

        [Fact]
        public async Task Initialize_FreshDatabase_CreatesAllTablesAndVersion()
        {
            using var db = new TempDatabase();

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            var connection = db.OpenConnection();
            try
            {
                var tables = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' " +
                    "AND name IN ('KnowledgeEntry', 'Step', 'AttachmentInfo')");
                Assert.Equal(3, tables);

                Assert.Equal(
                    SchemaMigrator.CurrentVersion,
                    await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
            }
            finally
            {
                await connection.CloseAsync();
            }

            Assert.Empty(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));
        }

        [Fact]
        public async Task Initialize_ExistingLegacyDatabase_CreatesBackupAndPreservesData()
        {
            using var db = new TempDatabase();
            await CreateLegacyDatabaseAsync(db);
            var originalColumns = await ReadColumnInfoAsync(db, "KnowledgeEntry");

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            var backups = Directory.GetFiles(db.DirectoryPath, "*.backup.*");
            Assert.Single(backups);

            var connection = db.OpenConnection();
            try
            {
                Assert.Equal(
                    SchemaMigrator.CurrentVersion,
                    await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));

                var title = await connection.ExecuteScalarAsync<string>(
                    "SELECT \"Title\" FROM \"KnowledgeEntry\" WHERE \"Id\" = 1");
                Assert.Equal("Legacy entry", title);

                var stepCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = 1");
                Assert.Equal(2, stepCount);

                var attachmentCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM \"AttachmentInfo\" WHERE \"KnowledgeEntryId\" = 1");
                Assert.Equal(1, attachmentCount);
            }
            finally
            {
                await connection.CloseAsync();
            }

            // Non-destructive: the pre-existing columns are preserved, in the
            // same order, and the new origin columns are appended after them.
            Assert.Equal(
                originalColumns.Concat(new[]
                {
                    "ProjectOriginKind:INTEGER:0",
                    "ProjectOriginLocation:VARCHAR:0",
                }),
                await ReadColumnInfoAsync(db, "KnowledgeEntry"));
        }

        [Fact]
        public async Task Initialize_Version1Database_AddsProjectOriginColumnsAndKeepsRows()
        {
            using var db = new TempDatabase();
            await CreateVersion1DatabaseAsync(db);

            var connection = db.OpenConnection();
            try
            {
                Assert.Equal(1, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
            }
            finally
            {
                await connection.CloseAsync();
            }

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            // A safety copy of the pre-migration file is always taken.
            var backupPath = Assert.Single(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));

            connection = db.OpenConnection();
            try
            {
                Assert.Equal(
                    SchemaMigrator.CurrentVersion,
                    await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));

                var columns = await connection.QueryAsync<ColumnInfo>(
                    "PRAGMA table_info(\"KnowledgeEntry\")");

                // The origin columns are appended at the end, so a fresh
                // database and a migrated one keep the same column order.
                Assert.Equal("ProjectOriginKind", columns[^2].name);
                Assert.Equal("ProjectOriginLocation", columns[^1].name);
                Assert.Equal(0, columns[^2].notnull);
                Assert.Equal(0, columns[^1].notnull);

                // The existing row survives and the new columns start empty.
                var row = Assert.Single(await connection.QueryAsync<EntryRow>(
                    "SELECT \"Title\", \"ProjectOriginKind\", \"ProjectOriginLocation\" " +
                    "FROM \"KnowledgeEntry\""));
                Assert.Equal("Legacy entry", row.Title);
                Assert.Null(row.ProjectOriginKind);
                Assert.Null(row.ProjectOriginLocation);
            }
            finally
            {
                await connection.CloseAsync();
            }

            // The backup keeps the pre-migration state: no origin columns yet.
            var backup = new SQLiteAsyncConnection(backupPath);
            try
            {
                var backupColumns = await backup.QueryAsync<ColumnInfo>(
                    "PRAGMA table_info(\"KnowledgeEntry\")");
                Assert.DoesNotContain(backupColumns, column => column.name == "ProjectOriginKind");
                Assert.DoesNotContain(backupColumns, column => column.name == "ProjectOriginLocation");
                Assert.Equal(
                    1,
                    await backup.ExecuteScalarAsync<int>("PRAGMA user_version"));
            }
            finally
            {
                await backup.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_WhenRunTwice_IsIdempotentAndBacksUpOnlyOnce()
        {
            using var db = new TempDatabase();
            await CreateLegacyDatabaseAsync(db);

            var migrator = new SchemaMigrator(db.DatabasePath);
            await migrator.InitializeAsync();
            await migrator.InitializeAsync();

            Assert.Single(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));

            var connection = db.OpenConnection();
            try
            {
                var entryCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM \"KnowledgeEntry\"");
                Assert.Equal(1, entryCount);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_MalformedStepPrimaryKey_RebuildsTableAndPreservesRows()
        {
            using var db = new TempDatabase();
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync(LegacyKnowledgeEntryDdl);
                await connection.ExecuteAsync(StepWithoutPrimaryKeyDdl);
                await connection.ExecuteAsync(LegacyAttachmentInfoDdl);
                await connection.ExecuteAsync(
                    "INSERT INTO \"KnowledgeEntry\" (\"Id\", \"Title\") VALUES (1, ?)", "Existing");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\") " +
                    "VALUES (10, ?, 1, 1)", "first");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\") " +
                    "VALUES (11, ?, 2, 1)", "second");
                await connection.ExecuteAsync($"PRAGMA user_version = {SchemaMigrator.CurrentVersion}");
            }
            finally
            {
                await connection.CloseAsync();
            }

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            Assert.Single(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));
            connection = db.OpenConnection();
            try
            {
                var columns = await connection.QueryAsync<ColumnInfo>("PRAGMA table_info(\"Step\")");
                Assert.Equal(1, columns.Single(column => column.name == "Id").pk);
                var steps = await connection.QueryAsync<StepRow>(
                    "SELECT \"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\" " +
                    "FROM \"Step\" ORDER BY \"Order\"");
                Assert.Equal(new[] { "first", "second" }, steps.Select(step => step.Description));
                Assert.Equal(
                    new[] { 10, 11 },
                    steps.Select(step => step.Id));
                Assert.Equal(2, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\""));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_MalformedStepPrimaryKeyWithDuplicateIds_PreservesEveryRow()
        {
            using var db = new TempDatabase();
            await CreateLegacyDatabaseAsync(db);
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync("DROP TABLE \"Step\"");
                await connection.ExecuteAsync(StepWithoutPrimaryKeyDdl);
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\") " +
                    "VALUES (5, ?, 1, 1)", "duplicate one");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\") " +
                    "VALUES (5, ?, 2, 1)", "duplicate two");
                await connection.ExecuteAsync($"PRAGMA user_version = {SchemaMigrator.CurrentVersion}");
            }
            finally
            {
                await connection.CloseAsync();
            }

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            connection = db.OpenConnection();
            try
            {
                var steps = await connection.QueryAsync<StepRow>(
                    "SELECT \"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\" " +
                    "FROM \"Step\" ORDER BY \"Order\"");
                Assert.Equal(new[] { "duplicate one", "duplicate two" }, steps.Select(step => step.Description));
                Assert.Equal(2, steps.Select(step => step.Id).Distinct().Count());
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_RemovesOrphanedChildrenAndBacksUpDatabase()
        {
            using var db = new TempDatabase();
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync(LegacyKnowledgeEntryDdl);
                await connection.ExecuteAsync(LegacyStepDdl);
                await connection.ExecuteAsync(LegacyAttachmentInfoDdl);
                await connection.ExecuteAsync(
                    "INSERT INTO \"KnowledgeEntry\" (\"Id\", \"Title\") VALUES (1, ?)", "Existing");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Description\", \"Order\", \"KnowledgeEntryId\") VALUES (?, 1, 1)",
                    "valid");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Description\", \"Order\", \"KnowledgeEntryId\") VALUES (?, 1, 999)",
                    "orphan");
                await connection.ExecuteAsync(
                    "INSERT INTO \"AttachmentInfo\" (\"KnowledgeEntryId\", \"FileName\", \"FilePath\") " +
                    "VALUES (1, ?, ?)", "valid.txt", @"C:\valid.txt");
                await connection.ExecuteAsync(
                    "INSERT INTO \"AttachmentInfo\" (\"KnowledgeEntryId\", \"FileName\", \"FilePath\") " +
                    "VALUES (999, ?, ?)", "orphan.txt", @"C:\orphan.txt");
                await connection.ExecuteAsync($"PRAGMA user_version = {SchemaMigrator.CurrentVersion}");
            }
            finally
            {
                await connection.CloseAsync();
            }

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            Assert.Single(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));
            connection = db.OpenConnection();
            try
            {
                Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\""));
                Assert.Equal(
                    1,
                    await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"AttachmentInfo\""));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_MissingDirectory_IsCreated()
        {
            using var db = new TempDatabase();
            var nestedPath = Path.Combine(db.DirectoryPath, "nested", "deep", "knowledge.db3");
            var migrator = new SchemaMigrator(new DatabasePath(nestedPath));

            await migrator.InitializeAsync();

            Assert.True(File.Exists(nestedPath));
        }

        [Fact]
        public async Task Initialize_CopiesLegacyDatabaseToNewPath_AndKeepsOriginal()
        {
            using var db = new TempDatabase();
            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            var source = db.OpenConnection();
            try
            {
                await source.InsertAsync(new KnowledgeEntry { Title = "Existing entry" });
            }
            finally
            {
                await source.CloseAsync();
            }

            var destinationPath = Path.Combine(db.DirectoryPath, "project", "knowledge.db3");
            await new SchemaMigrator(new DatabasePath(destinationPath, db.DbPath)).InitializeAsync();

            Assert.True(File.Exists(db.DbPath));
            Assert.True(File.Exists(destinationPath));
            var destination = new SQLiteAsyncConnection(destinationPath);
            try
            {
                Assert.Equal(
                    "Existing entry",
                    await destination.ExecuteScalarAsync<string>("SELECT \"Title\" FROM \"KnowledgeEntry\""));
            }
            finally
            {
                await destination.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_CurrentVersionValidDatabase_StartsWithoutBackupOrRepair()
        {
            using var db = new TempDatabase();
            // A fresh database is created with the current schema version.
            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            var connection = db.OpenConnection();
            try
            {
                await connection.InsertAsync(new KnowledgeEntry { Title = "Kept entry" });
            }
            finally
            {
                await connection.CloseAsync();
            }

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            // A valid, up-to-date database is neither backed up nor repaired.
            Assert.Empty(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));

            connection = db.OpenConnection();
            try
            {
                Assert.Equal(
                    SchemaMigrator.CurrentVersion,
                    await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
                Assert.Equal(
                    "Kept entry",
                    await connection.ExecuteScalarAsync<string>(
                        "SELECT \"Title\" FROM \"KnowledgeEntry\""));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_LegacyDatabaseWithoutUserVersion_UpgradesToCurrentVersion()
        {
            using var db = new TempDatabase();
            // The legacy helper never writes PRAGMA user_version, so it stays 0.
            await CreateLegacyDatabaseAsync(db);

            var connection = db.OpenConnection();
            try
            {
                Assert.Equal(0, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
            }
            finally
            {
                await connection.CloseAsync();
            }

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            connection = db.OpenConnection();
            try
            {
                Assert.Equal(
                    SchemaMigrator.CurrentVersion,
                    await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
            }
            finally
            {
                await connection.CloseAsync();
            }

            Assert.Single(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));
        }

        [Fact]
        public async Task Initialize_DatabaseFromNewerVersion_ThrowsAndLeavesDatabaseUntouched()
        {
            using var db = new TempDatabase();
            await CreateLegacyDatabaseAsync(db);
            var originalColumns = await ReadColumnInfoAsync(db, "KnowledgeEntry");

            var futureVersion = SchemaMigrator.CurrentVersion + 3;
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync($"PRAGMA user_version = {futureVersion}");
            }
            finally
            {
                await connection.CloseAsync();
            }

            // A version mismatch is reported specifically, never mistaken for a
            // backup failure.
            var exception = await Assert.ThrowsAsync<SchemaVersionTooNewException>(
                () => new SchemaMigrator(db.DatabasePath).InitializeAsync());
            Assert.Equal(futureVersion, exception.StoredVersion);
            Assert.Equal(SchemaMigrator.CurrentVersion, exception.SupportedVersion);

            // No backup is taken and the incompatible file is left verbatim.
            Assert.Empty(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));

            connection = db.OpenConnection();
            try
            {
                Assert.Equal(
                    futureVersion,
                    await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
                Assert.Equal(
                    "Legacy entry",
                    await connection.ExecuteScalarAsync<string>(
                        "SELECT \"Title\" FROM \"KnowledgeEntry\" WHERE \"Id\" = 1"));
                Assert.Equal(
                    2,
                    await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = 1"));
                Assert.Equal(
                    1,
                    await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM \"AttachmentInfo\" WHERE \"KnowledgeEntryId\" = 1"));
            }
            finally
            {
                await connection.CloseAsync();
            }

            // Nothing was added to or removed from the schema either.
            Assert.Equal(originalColumns, await ReadColumnInfoAsync(db, "KnowledgeEntry"));
        }

        [Fact]
        public async Task Initialize_WhenBackupCannotBeCreated_AbortsWithoutMigratingOrRepairing()
        {
            using var db = new TempDatabase();
            await CreateLegacyDatabaseAsync(db);

            // An orphaned child survives only if the destructive cleanup never runs.
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Description\", \"Order\", \"KnowledgeEntryId\") " +
                    "VALUES (?, 3, 999)", "orphan");
            }
            finally
            {
                await connection.CloseAsync();
            }

            var originalAccess = DenyFileCreationIn(db.DirectoryPath);
            try
            {
                // A backup failure is reported specifically, never mistaken for a
                // version mismatch.
                var exception = await Assert.ThrowsAsync<DatabaseBackupException>(
                    () => new SchemaMigrator(db.DatabasePath).InitializeAsync());
                Assert.NotNull(exception.BackupPath);

                // The required backup failed before any repair or cleanup.
                Assert.Empty(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));
            }
            finally
            {
                RestoreAccessControl(db.DirectoryPath, originalAccess);
            }

            connection = db.OpenConnection();
            try
            {
                Assert.Equal(0, await connection.ExecuteScalarAsync<int>("PRAGMA user_version"));
                Assert.Equal(
                    "Legacy entry",
                    await connection.ExecuteScalarAsync<string>(
                        "SELECT \"Title\" FROM \"KnowledgeEntry\" WHERE \"Id\" = 1"));
                Assert.Equal(3, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\""));
                Assert.Equal(
                    1,
                    await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM \"Step\" WHERE \"KnowledgeEntryId\" = 999"));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_MalformedStepRepair_BackupCapturesPreRepairState()
        {
            using var db = new TempDatabase();
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync(LegacyKnowledgeEntryDdl);
                await connection.ExecuteAsync(StepWithoutPrimaryKeyDdl);
                await connection.ExecuteAsync(LegacyAttachmentInfoDdl);
                await connection.ExecuteAsync(
                    "INSERT INTO \"KnowledgeEntry\" (\"Id\", \"Title\") VALUES (1, ?)", "Existing");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\") " +
                    "VALUES (10, ?, 1, 1)", "first");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\") " +
                    "VALUES (11, ?, 2, 1)", "second");
                await connection.ExecuteAsync($"PRAGMA user_version = {SchemaMigrator.CurrentVersion}");
            }
            finally
            {
                await connection.CloseAsync();
            }

            await new SchemaMigrator(db.DatabasePath).InitializeAsync();

            // The safety copy is a consistent snapshot of the state *before* the
            // repair: the Step table still lacks its primary key and keeps both rows.
            var backupPath = Assert.Single(Directory.GetFiles(db.DirectoryPath, "*.backup.*"));

            // Backups keep the same name pattern and location as before.
            Assert.Equal(db.DirectoryPath, Path.GetDirectoryName(backupPath));
            var backupName = Path.GetFileName(backupPath);
            Assert.StartsWith("knowledge.backup.", backupName);
            Assert.EndsWith(".db3", backupName);
            var timestamp = backupName["knowledge.backup.".Length..^".db3".Length];
            Assert.Equal(17, timestamp.Length);
            Assert.True(timestamp.All(char.IsAsciiDigit));

            connection = new SQLiteAsyncConnection(backupPath);
            try
            {
                var backupColumns = await connection.QueryAsync<ColumnInfo>("PRAGMA table_info(\"Step\")");
                Assert.Equal(0, backupColumns.Single(column => column.name == "Id").pk);
                Assert.Equal(2, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\""));
            }
            finally
            {
                await connection.CloseAsync();
            }

            // The live database was repaired afterwards.
            connection = db.OpenConnection();
            try
            {
                var liveColumns = await connection.QueryAsync<ColumnInfo>("PRAGMA table_info(\"Step\")");
                Assert.Equal(1, liveColumns.Single(column => column.name == "Id").pk);
                Assert.Equal(2, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\""));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        [Fact]
        public async Task Initialize_RepairFailureAfterBackup_ReportsBackupAndLeavesDatabaseUnrepaired()
        {
            using var db = new TempDatabase();
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync(LegacyKnowledgeEntryDdl);
                // No primary key (so a repair is attempted) but missing a column the
                // repair requires (so the repair fails). KnowledgeEntryId is kept so
                // the pre-repair orphan check can still run.
                await connection.ExecuteAsync(
                    "CREATE TABLE \"Step\" (\"Id\" integer, \"Order\" integer, \"KnowledgeEntryId\" integer)");
                await connection.ExecuteAsync(
                    "INSERT INTO \"KnowledgeEntry\" (\"Id\", \"Title\") VALUES (1, ?)", "Existing");
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Id\", \"Order\", \"KnowledgeEntryId\") VALUES (1, 1, 1)");
                await connection.ExecuteAsync($"PRAGMA user_version = {SchemaMigrator.CurrentVersion}");
            }
            finally
            {
                await connection.CloseAsync();
            }

            var exception = await Assert.ThrowsAsync<SchemaMigrationException>(
                () => new SchemaMigrator(db.DatabasePath).InitializeAsync());

            Assert.IsType<InvalidDataException>(exception.InnerException);
            Assert.NotNull(exception.BackupPath);
            Assert.True(File.Exists(exception.BackupPath));

            // The database is not left partially repaired.
            connection = db.OpenConnection();
            try
            {
                var columns = await connection.QueryAsync<ColumnInfo>("PRAGMA table_info(\"Step\")");
                Assert.Equal(3, columns.Count);
                Assert.Equal(0, columns.Single(column => column.name == "Id").pk);
                Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\""));
                Assert.Equal(
                    "Existing",
                    await connection.ExecuteScalarAsync<string>(
                        "SELECT \"Title\" FROM \"KnowledgeEntry\" WHERE \"Id\" = 1"));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        /// <summary>
        /// Makes creating new files in <paramref name="directory"/> fail so the
        /// pre-migration backup cannot be written, and returns the original access
        /// control so it can be restored.
        /// </summary>
        private static DirectorySecurity DenyFileCreationIn(string directory)
        {
            var info = new DirectoryInfo(directory);
            var original = info.GetAccessControl();
            var restricted = info.GetAccessControl();
            restricted.AddAccessRule(new FileSystemAccessRule(
                WindowsIdentity.GetCurrent().User!,
                FileSystemRights.CreateFiles,
                AccessControlType.Deny));
            info.SetAccessControl(restricted);
            return original;
        }

        private static void RestoreAccessControl(string directory, DirectorySecurity security) =>
            new DirectoryInfo(directory).SetAccessControl(security);

        private static async Task CreateLegacyDatabaseAsync(TempDatabase db)
        {
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync(LegacyKnowledgeEntryDdl);
                await connection.ExecuteAsync(LegacyStepDdl);
                await connection.ExecuteAsync(LegacyAttachmentInfoDdl);

                await connection.ExecuteAsync(
                    "INSERT INTO \"KnowledgeEntry\" " +
                    "(\"Title\", \"Description\", \"ProjectName\", \"ResolutionSteps\", \"IsFavorite\") " +
                    "VALUES (?, ?, ?, ?, ?)",
                    "Legacy entry",
                    "Legacy description",
                    "LegacyProject",
                    "old free-text steps",
                    1);

                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Description\", \"Order\", \"KnowledgeEntryId\") VALUES (?, ?, ?)",
                    "first", 1, 1);
                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Description\", \"Order\", \"KnowledgeEntryId\") VALUES (?, ?, ?)",
                    "second", 2, 1);

                await connection.ExecuteAsync(
                    "INSERT INTO \"AttachmentInfo\" (\"KnowledgeEntryId\", \"FileName\", \"FilePath\") VALUES (?, ?, ?)",
                    1, "log.txt", @"C:\temp\log.txt");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        /// <summary>
        /// Creates a database with the version 1 schema (the one shipped before
        /// the typed project origin existed) and one entry, so the v1 -&gt; v2
        /// migration can be exercised.
        /// </summary>
        private static async Task CreateVersion1DatabaseAsync(TempDatabase db)
        {
            var connection = db.OpenConnection();
            try
            {
                await connection.ExecuteAsync(LegacyKnowledgeEntryDdl);
                await connection.ExecuteAsync(LegacyStepDdl);
                await connection.ExecuteAsync(LegacyAttachmentInfoDdl);

                await connection.ExecuteAsync(
                    "INSERT INTO \"KnowledgeEntry\" " +
                    "(\"Title\", \"Description\", \"ProjectName\", \"ResolutionSteps\", \"IsFavorite\") " +
                    "VALUES (?, ?, ?, ?, ?)",
                    "Legacy entry",
                    "Legacy description",
                    "LegacyProject",
                    "old free-text steps",
                    1);

                await connection.ExecuteAsync(
                    "INSERT INTO \"Step\" (\"Description\", \"Order\", \"KnowledgeEntryId\") VALUES (?, ?, ?)",
                    "first", 1, 1);

                await connection.ExecuteAsync(
                    "INSERT INTO \"AttachmentInfo\" (\"KnowledgeEntryId\", \"FileName\", \"FilePath\") VALUES (?, ?, ?)",
                    1, "log.txt", @"C:\temp\log.txt");

                await connection.ExecuteAsync("PRAGMA user_version = 1");
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task<string[]> ReadColumnInfoAsync(TempDatabase db, string table)
        {
            var connection = db.OpenConnection();
            try
            {
                var rows = await connection.QueryAsync<ColumnInfo>(
                    $"PRAGMA table_info(\"{table}\")");
                // The type is compared case-insensitively: SQLite type affinity
                // does not care about the casing used in the table definition.
                return rows
                    .Select(r => $"{r.name}:{r.type.ToUpperInvariant()}:{r.pk}")
                    .ToArray();
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private sealed class ColumnInfo
        {
            public int cid { get; set; }
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public int notnull { get; set; }
            public string? dflt_value { get; set; }
            public int pk { get; set; }
        }

        private sealed class StepRow
        {
            public int Id { get; set; }
            public string? Description { get; set; }
            public int Order { get; set; }
            public int KnowledgeEntryId { get; set; }
        }

        private sealed class EntryRow
        {
            public string? Title { get; set; }
            public int? ProjectOriginKind { get; set; }
            public string? ProjectOriginLocation { get; set; }
        }
    }
}
