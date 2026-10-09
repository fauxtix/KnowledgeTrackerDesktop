using KnowledgeTrackerDesktop.Data;
using SQLite;

namespace KnowledgeTrackerDesktop.Tests.Data
{
    /// <summary>
    /// Creates an isolated SQLite database under %TEMP% so tests never touch
    /// the application's database.
    /// </summary>
    internal sealed class TempDatabase : IDisposable
    {
        public string DirectoryPath { get; }

        public string DbPath => Path.Combine(DirectoryPath, "knowledge.db3");

        public TempDatabase()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "knowledgetracker-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public DatabasePath DatabasePath => new(DbPath);

        public SQLiteAsyncConnection OpenConnection() => new(DbPath);

        public void Dispose()
        {
            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch (IOException)
            {
                // The sqlite-net connection pool can still hold the file
                // briefly; leftovers in %TEMP% are harmless.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
