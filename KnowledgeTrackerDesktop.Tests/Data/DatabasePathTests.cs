using KnowledgeTrackerDesktop.Data;
using KnowledgeTrackerDesktop.Tests.Data;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Data
{
    public class DatabasePathTests
    {
        [Fact]
        public void ResolveDefaultPath_DevelopmentBuild_UsesSolutionRoot()
        {
            using var temp = new TempDatabase();
            var solutionRoot = Path.Combine(temp.DirectoryPath, "repo");
            var projectDirectory = Path.Combine(solutionRoot, "KnowledgeTrackerDesktop");
            var applicationDirectory = Path.Combine(
                projectDirectory,
                "bin",
                "Debug",
                "net10.0-windows");
            Directory.CreateDirectory(applicationDirectory);
            File.WriteAllText(
                Path.Combine(solutionRoot, "KnowledgeTrackerDesktop.slnx"),
                string.Empty);
            File.WriteAllText(
                Path.Combine(projectDirectory, "KnowledgeTrackerDesktop.csproj"),
                string.Empty);

            var path = DatabasePath.ResolveDefaultPath(applicationDirectory, isDevelopmentBuild: true);

            Assert.Equal(Path.Combine(solutionRoot, "knowledge.db3"), path);
        }

        [Fact]
        public void ResolveDefaultPath_ProductionBuild_UsesApplicationDirectoryEvenUnderSolution()
        {
            using var temp = new TempDatabase();
            var solutionRoot = Path.Combine(temp.DirectoryPath, "repo");
            var projectDirectory = Path.Combine(solutionRoot, "KnowledgeTrackerDesktop");
            var publishDirectory = Path.Combine(
                projectDirectory,
                "bin",
                "Release",
                "net10.0-windows",
                "publish");
            Directory.CreateDirectory(publishDirectory);
            File.WriteAllText(
                Path.Combine(solutionRoot, "KnowledgeTrackerDesktop.slnx"),
                string.Empty);

            var path = DatabasePath.ResolveDefaultPath(publishDirectory, isDevelopmentBuild: false);

            Assert.Equal(Path.Combine(publishDirectory, "knowledge.db3"), path);
        }

        [Fact]
        public void GetDefaultPath_UsesBuildConfigurationPathPolicy()
        {
            var isDevelopmentBuild = IsDevelopmentBuild;
            var expected = DatabasePath.ResolveDefaultPath(
                AppContext.BaseDirectory,
                isDevelopmentBuild);

            Assert.Equal(expected, DatabasePath.GetDefaultPath());
        }

#if DEBUG
        private const bool IsDevelopmentBuild = true;
#else
        private const bool IsDevelopmentBuild = false;
#endif
    }
}
