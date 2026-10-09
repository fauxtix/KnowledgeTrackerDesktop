using SQLite;

namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// Resolves the location of the SQLite database.
    /// Development builds use the solution root; deployed builds use the
    /// application directory. Existing user data is copied from the legacy
    /// LocalAppData location on first startup.
    /// </summary>
    public sealed class DatabasePath
    {
        public string FilePath { get; }

        public string? LegacyFilePath { get; }

        public DatabasePath(string? filePath = null, string? legacyFilePath = null)
        {
            var isDefaultPath = string.IsNullOrWhiteSpace(filePath);
            FilePath = isDefaultPath ? GetDefaultPath() : filePath!;
            LegacyFilePath = string.IsNullOrWhiteSpace(legacyFilePath)
                ? isDefaultPath ? GetLegacyPath() : null
                : legacyFilePath;
        }

        public static string GetDefaultPath() =>
            Path.Combine(FindApplicationRoot(), "knowledge.db3");

        public static string GetLegacyPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KnowledgeTrackerApp",
                "knowledge.db3");

        private static string FindApplicationRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            string? projectDirectory = null;

            while (directory is not null)
            {
                if (directory.EnumerateFiles("*.sln").Any() ||
                    directory.EnumerateFiles("*.slnx").Any())
                {
                    return directory.FullName;
                }

                if (projectDirectory is null && directory.EnumerateFiles("*.csproj").Any())
                {
                    projectDirectory = directory.FullName;
                }

                directory = directory.Parent;
            }

            return projectDirectory ?? AppContext.BaseDirectory;
        }

        public void CopyLegacyDatabaseIfNeeded()
        {
            if (LegacyFilePath is null ||
                !File.Exists(LegacyFilePath) ||
                File.Exists(FilePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var source = new SQLiteConnection(LegacyFilePath);
            source.Backup(FilePath, "main");
        }
    }
}
