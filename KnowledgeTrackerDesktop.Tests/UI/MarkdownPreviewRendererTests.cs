using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    public class MarkdownPreviewRendererTests
    {
        [Fact]
        public void EmptyInputProducesEmptyOutput()
        {
            Assert.Equal(string.Empty, MarkdownPreviewRenderer.ToPlainText(null));
            Assert.Equal(string.Empty, MarkdownPreviewRenderer.ToPlainText(string.Empty));
        }

        [Fact]
        public void HeadingsLoseTheirHashMarkersButKeepText()
        {
            var result = MarkdownPreviewRenderer.ToPlainText("# Title\n\n## Section");

            Assert.Equal("Title\n\nSection", result);
        }

        [Fact]
        public void BulletMarkersBecomeDots()
        {
            var result = MarkdownPreviewRenderer.ToPlainText("- first\n* second\n+ third");

            Assert.Equal("\u2022 first\n\u2022 second\n\u2022 third", result);
        }

        [Fact]
        public void InlineLinksShowLabelAndUrl()
        {
            var result = MarkdownPreviewRenderer.ToPlainText("See [docs](https://example.com) now");

            Assert.Equal("See docs (https://example.com) now", result);
        }

        [Fact]
        public void EmphasisMarkersAreRemoved()
        {
            var result = MarkdownPreviewRenderer.ToPlainText("This is **bold** and _italic_ and `code`");

            Assert.Equal("This is bold and italic and code", result);
        }

        [Fact]
        public void ContentInsideCodeFencesIsPreservedVerbatim()
        {
            const string markdown = "before\n```csharp\nif (a < b) {\n    **not bold**;\n}\n```\nafter";

            var result = MarkdownPreviewRenderer.ToPlainText(markdown);

            Assert.Contains("if (a < b) {", result);
            Assert.Contains("    **not bold**;", result);
            Assert.DoesNotContain("```", result);
        }

        [Fact]
        public void BlankLinesBetweenParagraphsArePreserved()
        {
            var result = MarkdownPreviewRenderer.ToPlainText("one\n\ntwo");

            Assert.Equal("one\n\ntwo", result);
        }
    }
}
