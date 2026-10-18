using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Guards the url validation that decides whether the "open video" action is
    /// offered. Only the pure predicate is asserted here: actually shell-opening
    /// a file or a link shows a dialog / starts an external process and stays in
    /// the manual checklist.
    /// </summary>
    public class EntryOpenActionsTests
    {
        [Theory]
        [InlineData("https://youtu.be/abc", true)]
        [InlineData("http://example.com/watch?v=1", true)]
        [InlineData("  https://example.com/x  ", true)]
        [InlineData("HTTPS://EXAMPLE.COM", true)]
        [InlineData("javascript:alert(1)", false)]
        [InlineData("file:///C:/temp/a.txt", false)]
        [InlineData("ftp://example.com/a", false)]
        [InlineData("mailto:someone@example.com", false)]
        [InlineData("www.example.com", false)]
        [InlineData("youtu.be/abc", false)]
        [InlineData("http://", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void OnlyHttpAndHttpsUrlsAreAccepted(string? url, bool expected) =>
            Assert.Equal(expected, EntryOpenActions.IsHttpUrl(url));
    }
}
