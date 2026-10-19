using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Pure helpers behind the AutoComplete integration of the Technologies and
    /// Tags fields. Both fields store "a, b, c" lists while the Syncfusion
    /// component matches one piece of text: these helpers extract the token
    /// being typed (handed to the component through AutoCompleteCustomize),
    /// match the candidates and replace that token in place — without any
    /// WinForms state, so they can be unit tested directly.
    /// </summary>
    internal static class TagSuggestionHelper
    {
        private static readonly char[] Separators = { ',', ';' };

        /// <summary>
        /// The value currently being typed: the text after the last separator
        /// ("sqlite, wi" -> "wi"; "sqlite; " -> ""). A text without separators
        /// is the token itself.
        /// </summary>
        internal static string CurrentToken(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var lastSeparator = text.LastIndexOfAny(Separators);
            return lastSeparator < 0 ? text : text[(lastSeparator + 1)..];
        }

        /// <summary>
        /// Whether a known value matches the token being typed (prefix match,
        /// case-insensitive). An empty token matches every real value.
        /// </summary>
        internal static bool MatchesToken(string? token, string? candidate)
        {
            var trimmedToken = token?.Trim() ?? string.Empty;
            if (trimmedToken.Length == 0)
            {
                return !string.IsNullOrWhiteSpace(candidate);
            }

            return candidate?.Trim()
                .StartsWith(trimmedToken, StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// The complete values already present in the field, excluding the
        /// token currently being typed (the text after the last separator,
        /// which may still be incomplete).
        /// </summary>
        internal static IReadOnlyList<string> UsedValues(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Array.Empty<string>();
            }

            var lastSeparator = text.LastIndexOfAny(Separators);
            var completed = lastSeparator < 0 ? string.Empty : text[..lastSeparator];
            return KnowledgeEntryTags.Split(completed);
        }

        /// <summary>
        /// Whether a known value may be offered for the given field text: it
        /// must match the token being typed and must not be a value the field
        /// already contains completely (picking it would create a duplicate
        /// that the entry validator blocks on save). This is what keeps the
        /// dropdown useful in edit mode: everything still missing is offered,
        /// everything already included stays out.
        /// </summary>
        internal static bool ShouldOffer(string? text, string? candidate)
        {
            if (!MatchesToken(CurrentToken(text), candidate))
            {
                return false;
            }

            var trimmedCandidate = candidate?.Trim() ?? string.Empty;
            return !UsedValues(text).Contains(
                trimmedCandidate,
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Semicolon-style text keeps receiving semicolons; everything else
        /// (including empty or comma-based text) continues with commas.
        /// </summary>
        internal static char PreferredSeparator(string? text) =>
            text is not null && text.Contains(';') && !text.Contains(',') ? ';' : ',';

        /// <summary>
        /// Leaves the field ready for the next value: appends the preferred
        /// separator and one space to the freshly inserted text ("sqlite" ->
        /// "sqlite, "), so several values can be picked in a row.
        /// </summary>
        internal static string AppendSeparator(string? text) =>
            (text ?? string.Empty) + PreferredSeparator(text) + " ";

        /// <summary>
        /// Drops the separator/space left at the end by AppendSeparator, so
        /// the saved value stays clean ("sqlite, " -> "sqlite").
        /// </summary>
        internal static string TrimTrailingSeparators(string? text) =>
            (text ?? string.Empty).TrimEnd(' ', ',', ';');

        /// <summary>
        /// Replaces the token being typed with the picked value, keeping every
        /// previously typed value and the list style ("sqlite, wi" plus
        /// "winforms" -> "sqlite, winforms"; ";"-based lists keep semicolons).
        /// </summary>
        internal static string ReplaceCurrentToken(string? text, string value)
        {
            var current = text ?? string.Empty;
            var lastSeparator = current.LastIndexOfAny(Separators);
            if (lastSeparator < 0)
            {
                return value;
            }

            var prefix = current[..(lastSeparator + 1)].TrimEnd();
            return prefix + " " + value;
        }
    }
}
