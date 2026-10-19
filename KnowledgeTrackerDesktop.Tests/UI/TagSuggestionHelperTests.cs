using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// The pure rules behind the editor's suggestion dropdowns: which token is
    /// being typed in a "a, b, c" list field, which known values match it and
    /// how a picked value replaces it in place.
    /// </summary>
    public sealed class TagSuggestionHelperTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("sqlite", "sqlite")]
        [InlineData("sqlite, wi", " wi")]
        [InlineData("sqlite, winforms;", "")]
        [InlineData("a; b", " b")]
        public void CurrentToken_ReturnsTheTextAfterTheLastSeparator(string? text, string expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.CurrentToken(text));
        }

        [Theory]
        [InlineData("", "sqlite", true)]
        [InlineData(null, "sqlite", true)]
        [InlineData(" wi", "winforms", true)]
        [InlineData("wi", "WinForms", true)]
        [InlineData("sq", "sqlite", true)]
        [InlineData("in", "sqlite", false)]
        [InlineData("  ", "sqlite", true)]
        [InlineData("win", "", false)]
        public void MatchesToken_IsAPrefixMatchIgnoringCase(
            string? token,
            string? candidate,
            bool expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.MatchesToken(token, candidate));
        }

        [Theory]
        [InlineData(null, new string[0])]
        [InlineData("wi", new string[0])]
        [InlineData("sqlite, validation", new[] { "sqlite" })]
        [InlineData("sqlite, validation, wi", new[] { "sqlite", "validation" })]
        [InlineData("a; b; c", new[] { "a", "b" })]
        public void UsedValues_ExcludesTheTokenBeingTyped(string? text, string[] expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.UsedValues(text));
        }

        [Theory]
        [InlineData("sqlite, sq", "sqlite", false)] // already in the field
        [InlineData("SQLite, sq", "sqlite", false)] // case-insensitive
        [InlineData("sqlite, wi", "winforms", true)] // new value matching the token
        [InlineData("sqlite, ", "validation", true)] // empty token: everything unused
        [InlineData("sqlite, ", "sqlite", false)] // used values stay out
        [InlineData("", "sqlite", true)] // new entry: everything offered
        public void ShouldOffer_MatchesTheTokenAndSkipsAlreadyIncludedValues(
            string text,
            string candidate,
            bool expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.ShouldOffer(text, candidate));
        }

        [Theory]
        [InlineData(null, ", ")]
        [InlineData("sqlite", "sqlite, ")]
        [InlineData("sqlite, winforms", "sqlite, winforms, ")]
        [InlineData("a; b", "a; b; ")]
        public void AppendSeparator_ReadiesTheFieldForTheNextValue(
            string? text,
            string expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.AppendSeparator(text));
        }

        [Theory]
        [InlineData("sqlite, ", "sqlite")]
        [InlineData("sqlite, winforms, ", "sqlite, winforms")]
        [InlineData("a; b; ", "a; b")]
        [InlineData("a", "a")]
        [InlineData(null, "")]
        public void TrimTrailingSeparators_CleansTheValueBeforeSaving(
            string? text,
            string expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.TrimTrailingSeparators(text));
        }

        [Theory]
        [InlineData("a;b", ';')]
        [InlineData("a, b", ',')]
        [InlineData("a", ',')]
        [InlineData(null, ',')]
        public void PreferredSeparator_FollowsTheExistingStyle(string? text, char expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.PreferredSeparator(text));
        }

        [Theory]
        [InlineData(null, "value", "value")]
        [InlineData("wi", "winforms", "winforms")]
        [InlineData("sqlite, wi", "winforms", "sqlite, winforms")]
        [InlineData("sqlite,", "winforms", "sqlite, winforms")]
        [InlineData("first; sec", "second", "first; second")]
        public void ReplaceCurrentToken_KeepsThePreviousValuesAndSeparators(
            string? text,
            string value,
            string expected)
        {
            Assert.Equal(expected, TagSuggestionHelper.ReplaceCurrentToken(text, value));
        }
    }
}
