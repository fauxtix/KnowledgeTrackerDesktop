using KnowledgeTrackerDesktop.Core;
using SQLite;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Core
{
    /// <summary>
    /// Guarantees that the sqlite-net mappings produce exactly the legacy
    /// schema found in the existing knowledge.db3 (ARCHITECTURE.md section 5,
    /// AGENTS.md "Current Schema"). A mismatch here would silently break
    /// existing user data.
    /// </summary>
    public class ModelSchemaTests : IDisposable
    {
        private sealed class TableInfo
        {
            public int cid { get; set; }
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public int notnull { get; set; }
            public string? dflt_value { get; set; }
            public int pk { get; set; }
        }

        private readonly SQLiteConnection _connection = new(":memory:");

        public void Dispose() => _connection.Dispose();

        private List<TableInfo> Columns(string table) =>
            _connection.Query<TableInfo>($"PRAGMA table_info(\"{table}\")");

        // SQLite type affinity is case-insensitive ("INTEGER" == "integer").
        // The legacy database was created by an older sqlite-net that emitted
        // lowercase types; current sqlite-net-pcl emits INTEGER for int/bool.
        // Names and affinity must match exactly, casing must not.
        private static (string Name, string Type)[] Normalized(IEnumerable<TableInfo> columns) =>
            columns.Select(c => (c.name, c.type.ToUpperInvariant())).ToArray();

        [Fact]
        public void KnowledgeEntry_MatchesLegacySchema()
        {
            _connection.CreateTable<KnowledgeEntry>();

            var columns = Columns(nameof(KnowledgeEntry));

            var expected = new (string Name, string Type)[]
            {
                ("Id", "INTEGER"),
                ("Title", "VARCHAR"),
                ("Description", "VARCHAR"),
                ("DateResolved", "BIGINT"),
                ("ProjectName", "VARCHAR"),
                ("ResolutionSteps", "VARCHAR"),
                ("Technologies", "VARCHAR"),
                ("Tags", "VARCHAR"),
                ("Comments", "VARCHAR"),
                ("CodeSnippet", "VARCHAR"),
                ("IsFavorite", "INTEGER"),
                ("YouTubeUrl", "VARCHAR"),
                ("ProjectOriginKind", "INTEGER"),
                ("ProjectOriginLocation", "VARCHAR"),
            };

            Assert.Equal(expected, Normalized(columns));

            Assert.Equal(1, columns.Single(c => c.name == "Id").pk);

            Assert.DoesNotContain(columns, c => c.name == "Steps");
            Assert.DoesNotContain(columns, c => c.name == "Attachments");
        }

        [Fact]
        public void Step_MatchesLegacySchema()
        {
            _connection.CreateTable<Step>();

            var columns = Columns(nameof(Step));

            var expected = new (string Name, string Type)[]
            {
                ("Id", "INTEGER"),
                ("Description", "VARCHAR"),
                ("Order", "INTEGER"),
                ("KnowledgeEntryId", "INTEGER"),
            };

            Assert.Equal(expected, Normalized(columns));

            Assert.Equal(1, columns.Single(c => c.name == "Id").pk);
        }

        [Fact]
        public void AttachmentInfo_MatchesLegacySchema()
        {
            _connection.CreateTable<AttachmentInfo>();

            var columns = Columns(nameof(AttachmentInfo));

            var expected = new (string Name, string Type)[]
            {
                ("Id", "INTEGER"),
                ("KnowledgeEntryId", "INTEGER"),
                ("FileName", "VARCHAR"),
                ("FilePath", "VARCHAR"),
                ("DateAdded", "BIGINT"),
            };

            Assert.Equal(expected, Normalized(columns));

            Assert.Equal(1, columns.Single(c => c.name == "Id").pk);
        }

        [Fact]
        public void KnowledgeEntry_RoundTrip_PreservesFields()
        {
            _connection.CreateTable<KnowledgeEntry>();

            var entry = new KnowledgeEntry
            {
                Title = "Entry title",
                Description = "Markdown **content**",
                DateResolved = new DateTime(2026, 10, 8, 12, 34, 56, DateTimeKind.Unspecified),
                ProjectName = "KnowledgeTracker",
                ResolutionSteps = "legacy free-text steps",
                Technologies = "C#;SQLite",
                Tags = "bug,sqlite",
                Comments = "works",
                CodeSnippet = "Console.WriteLine();",
                IsFavorite = true,
                YouTubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            };

            _connection.Insert(entry);
            var loaded = _connection.Table<KnowledgeEntry>().Single();

            Assert.NotEqual(0, loaded.Id);
            Assert.Equal(entry.Title, loaded.Title);
            Assert.Equal(entry.Description, loaded.Description);
            Assert.Equal(entry.DateResolved, loaded.DateResolved);
            Assert.Equal(entry.ProjectName, loaded.ProjectName);
            Assert.Equal(entry.ResolutionSteps, loaded.ResolutionSteps);
            Assert.Equal(entry.Technologies, loaded.Technologies);
            Assert.Equal(entry.Tags, loaded.Tags);
            Assert.Equal(entry.Comments, loaded.Comments);
            Assert.Equal(entry.CodeSnippet, loaded.CodeSnippet);
            Assert.Equal(entry.IsFavorite, loaded.IsFavorite);
            Assert.Equal(entry.YouTubeUrl, loaded.YouTubeUrl);

            Assert.Empty(loaded.Steps);
            Assert.Empty(loaded.Attachments);
        }

        [Fact]
        public void KnowledgeEntry_RoundTrip_PreservesProjectOrigin()
        {
            _connection.CreateTable<KnowledgeEntry>();

            var entry = new KnowledgeEntry
            {
                Title = "Entry title",
                ProjectName = "KnowledgeTracker",
                ProjectOriginKind = ProjectOriginKind.GitRepository,
                ProjectOriginLocation = "https://github.com/owner/repo",
            };

            _connection.Insert(entry);
            var loaded = _connection.Table<KnowledgeEntry>().Single();

            Assert.Equal(ProjectOriginKind.GitRepository, loaded.ProjectOriginKind);
            Assert.Equal("https://github.com/owner/repo", loaded.ProjectOriginLocation);
        }

        [Fact]
        public void KnowledgeEntry_RoundTrip_PreservesMissingProjectOrigin()
        {
            _connection.CreateTable<KnowledgeEntry>();

            _connection.Insert(new KnowledgeEntry { Title = "Entry title" });
            var loaded = _connection.Table<KnowledgeEntry>().Single();

            // A null kind means "the origin was not recorded".
            Assert.Null(loaded.ProjectOriginKind);
            Assert.Null(loaded.ProjectOriginLocation);
        }

        [Fact]
        public void Step_RoundTrip_PreservesReservedOrderColumn()
        {
            _connection.CreateTable<Step>();

            _connection.Insert(new Step { Description = "first", Order = 1, KnowledgeEntryId = 42 });
            _connection.Insert(new Step { Description = "second", Order = 2, KnowledgeEntryId = 42 });

            var steps = _connection.Table<Step>().OrderBy(s => s.Order).ToList();

            Assert.Equal(2, steps.Count);
            Assert.Equal(new[] { "first", "second" }, steps.Select(s => s.Description).ToArray());
            Assert.Equal(new[] { 1, 2 }, steps.Select(s => s.Order).ToArray());
            Assert.All(steps, s => Assert.Equal(42, s.KnowledgeEntryId));
        }

        [Fact]
        public void AttachmentInfo_RoundTrip_PreservesFields()
        {
            _connection.CreateTable<AttachmentInfo>();

            var attachment = new AttachmentInfo
            {
                KnowledgeEntryId = 7,
                FileName = "log.txt",
                FilePath = @"C:\temp\log.txt",
                DateAdded = new DateTime(2026, 10, 7, 9, 0, 0, DateTimeKind.Unspecified),
            };

            _connection.Insert(attachment);
            var loaded = _connection.Table<AttachmentInfo>().Single();

            Assert.NotEqual(0, loaded.Id);
            Assert.Equal(attachment.KnowledgeEntryId, loaded.KnowledgeEntryId);
            Assert.Equal(attachment.FileName, loaded.FileName);
            Assert.Equal(attachment.FilePath, loaded.FilePath);
            Assert.Equal(attachment.DateAdded, loaded.DateAdded);
        }
    }
}
