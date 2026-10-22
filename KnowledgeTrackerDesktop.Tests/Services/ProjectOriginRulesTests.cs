using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Services
{
    /// <summary>
    /// The rules that decide how a recorded project origin is read: an
    /// absolute path for a local folder, an http(s) link for a repository or
    /// a web page.
    /// </summary>
    public class ProjectOriginRulesTests
    {
        [Theory]
        [InlineData(@"C:\projects\app")]
        [InlineData("C:/projects/app")]
        [InlineData(@"\\server\share\app")]
        public void TryValidate_LocalFolderWithAbsolutePath_IsAccepted(string location)
        {
            Assert.True(ProjectOriginRules.TryValidate(
                ProjectOriginKind.LocalFolder,
                location,
                out _));
        }

        [Theory]
        [InlineData("projects/app")]
        [InlineData("..\\app")]
        [InlineData("https://github.com/owner/repo")]
        [InlineData("")]
        public void TryValidate_LocalFolderWithoutAbsolutePath_IsRejected(string location)
        {
            Assert.False(ProjectOriginRules.TryValidate(
                ProjectOriginKind.LocalFolder,
                location,
                out var message));

            Assert.Equal(
                "The origin location must be an absolute folder path.",
                message);
        }

        [Theory]
        [InlineData("https://github.com/owner/repo")]
        [InlineData("http://github.com/owner/repo")]
        public void TryValidate_GitRepositoryWithHttpUrl_IsAccepted(string location)
        {
            Assert.True(ProjectOriginRules.TryValidate(
                ProjectOriginKind.GitRepository,
                location,
                out _));
        }

        [Theory]
        [InlineData("git@github.com:owner/repo.git")]
        [InlineData("ssh://github.com/owner/repo")]
        [InlineData("owner/repo")]
        [InlineData(@"C:\projects\app")]
        public void TryValidate_GitRepositoryWithNonHttpUrl_IsRejected(string location)
        {
            Assert.False(ProjectOriginRules.TryValidate(
                ProjectOriginKind.GitRepository,
                location,
                out var message));

            Assert.Equal(
                "The repository URL must be a valid http(s) link.",
                message);
        }

        [Theory]
        [InlineData("https://stackoverflow.com/questions/1")]
        [InlineData("http://example.com/page")]
        public void TryValidate_WebPageWithHttpUrl_IsAccepted(string location)
        {
            Assert.True(ProjectOriginRules.TryValidate(
                ProjectOriginKind.WebPage,
                location,
                out _));
        }

        [Fact]
        public void TryValidate_WebPageWithNonHttpUrl_IsRejected()
        {
            Assert.False(ProjectOriginRules.TryValidate(
                ProjectOriginKind.WebPage,
                "file:///C:/page.html",
                out var message));

            Assert.Equal(
                "The page URL must be a valid http(s) link.",
                message);
        }

        [Fact]
        public void TryValidate_TrimsTheLocationBeforeCheckingIt()
        {
            Assert.True(ProjectOriginRules.TryValidate(
                ProjectOriginKind.LocalFolder,
                "  C:\\projects\\app  ",
                out _));
        }

        [Fact]
        public void DescribeKind_ReturnsAReadableName()
        {
            Assert.Equal("Local folder", ProjectOriginRules.DescribeKind(ProjectOriginKind.LocalFolder));
            Assert.Equal("Git repository", ProjectOriginRules.DescribeKind(ProjectOriginKind.GitRepository));
            Assert.Equal("Web page", ProjectOriginRules.DescribeKind(ProjectOriginKind.WebPage));
        }

        [Fact]
        public void DescribeLocation_LocalFolder_ShowsTheFolderName()
        {
            Assert.Equal(
                "app",
                ProjectOriginRules.DescribeLocation(
                    ProjectOriginKind.LocalFolder,
                    @"C:\projects\app"));
        }

        [Fact]
        public void DescribeLocation_LocalFolderWithTrailingSeparator_ShowsTheFolderName()
        {
            Assert.Equal(
                "app",
                ProjectOriginRules.DescribeLocation(
                    ProjectOriginKind.LocalFolder,
                    @"C:\projects\app\"));
        }

        [Fact]
        public void DescribeLocation_Url_ShowsTheLastTwoSegments()
        {
            Assert.Equal(
                "owner/repo",
                ProjectOriginRules.DescribeLocation(
                    ProjectOriginKind.GitRepository,
                    "https://github.com/owner/repo"));
        }

        [Fact]
        public void DescribeLocation_UrlWithoutPath_ShowsTheHost()
        {
            Assert.Equal(
                "example.com",
                ProjectOriginRules.DescribeLocation(
                    ProjectOriginKind.WebPage,
                    "https://example.com"));
        }

        [Fact]
        public void DescribeLocation_EmptyLocation_ReturnsEmptyText()
        {
            Assert.Equal(
                string.Empty,
                ProjectOriginRules.DescribeLocation(ProjectOriginKind.LocalFolder, "   "));
        }
    }
}
