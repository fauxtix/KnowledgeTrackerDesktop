using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;
using SQLite;
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

            // Non-destructive: the pre-existing table definition is untouched.
            Assert.Equal(originalColumns, await ReadColumnInfoAsync(db, "KnowledgeEntry"));
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

        private static async Task<string> ReadColumnInfoAsync(TempDatabase db, string table)
        {
            var connection = db.OpenConnection();
            try
            {
                var rows = await connection.QueryAsync<ColumnInfo>(
                    $"PRAGMA table_info(\"{table}\")");
                return string.Join("|", rows.Select(r => $"{r.name}:{r.type}:{r.pk}"));
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
    }
}
