using SQLite;

namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// Resolves the location of the SQLite database.
    /// Debug builds use the solution root; non-Debug builds use the per-user
    /// LocalAppData folder (<c>%LOCALAPPDATA%\KnowledgeTrackerApp</c>). The
    /// latter is required for MSIX-packaged builds because the install
    /// directory (<c>C:\Program Files\WindowsApps</c>) is read-only; packaged
    /// apps also redirect LocalAppData to the package's private folder.
    /// Existing user data is still copied from the legacy LocalAppData location
    /// when it differs from the resolved path (relevant to Debug builds).
    /// </summary>
    public sealed class DatabasePath
    {
        /// <summary>Folder name used under LocalAppData for the database and settings.</summary>
        internal const string DataFolderName = "KnowledgeTrackerApp";

        private const string DatabaseFileName = "knowledge.db3";

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
            ResolveDefaultPath(AppContext.BaseDirectory, IsDevelopmentBuild);

        internal static string ResolveDefaultPath(
            string applicationBaseDirectory,
            bool isDevelopmentBuild,
            string? localAppDataDirectory = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(applicationBaseDirectory);

            if (isDevelopmentBuild)
            {
                return Path.Combine(
                    FindApplicationRoot(applicationBaseDirectory),
                    DatabaseFileName);
            }

            var localAppData = string.IsNullOrWhiteSpace(localAppDataDirectory)
                ? GetLocalAppDataDirectory()
                : localAppDataDirectory!;
            return Path.Combine(localAppData, DataFolderName, DatabaseFileName);
        }

        public static string GetLegacyPath() =>
            Path.Combine(GetLocalAppDataDirectory(), DataFolderName, DatabaseFileName);

        private static string GetLocalAppDataDirectory() =>
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        private static string FindApplicationRoot(string applicationBaseDirectory)
        {
            var directory = new DirectoryInfo(applicationBaseDirectory);
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

            return projectDirectory ?? applicationBaseDirectory;
        }

#if DEBUG
        private const bool IsDevelopmentBuild = true;
#else
        private const bool IsDevelopmentBuild = false;
#endif

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
