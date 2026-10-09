using System.Text;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Minimal, dependency-free Markdown-to-text rendering used by the Add/Edit
    /// preview. It never mutates the source (UI_DESIGN_SPEC section 5.6: the
    /// editor must preserve the exact Markdown source), it only produces a
    /// human-friendly read of it. Rich HTML rendering is a later concern.
    /// </summary>
    public static class MarkdownPreviewRenderer
    {
        public static string ToPlainText(string? markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(markdown.Length);
            var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var inFence = false;

            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                {
                    inFence = !inFence;
                    builder.Append('\n');
                    continue;
                }

                if (inFence)
                {
                    builder.Append(line);
                    builder.Append('\n');
                    continue;
                }

                builder.Append(RenderLine(line));
                builder.Append('\n');
            }

            return builder.ToString().TrimEnd('\n');
        }

        private static string RenderLine(string line)
        {
            var trimmed = line.TrimStart();
            var indent = line.Substring(0, line.Length - trimmed.Length);

            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            var headingLevel = 0;
            while (headingLevel < trimmed.Length && trimmed[headingLevel] == '#')
            {
                headingLevel++;
            }

            if (headingLevel is > 0 and <= 6 && headingLevel < trimmed.Length && trimmed[headingLevel] == ' ')
            {
                return indent + Inline(trimmed[(headingLevel + 1)..]);
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                trimmed.StartsWith("* ", StringComparison.Ordinal) ||
                trimmed.StartsWith("+ ", StringComparison.Ordinal))
            {
                return indent + "\u2022 " + Inline(trimmed[2..]);
            }

            return indent + Inline(trimmed);
        }

        private static string Inline(string text)
        {
            text = ReplaceLinks(text);
            text = text.Replace("**", string.Empty, StringComparison.Ordinal)
                .Replace("__", string.Empty, StringComparison.Ordinal);
            text = text.Replace("`", string.Empty, StringComparison.Ordinal);

            // Remove single-character emphasis markers left by *italic* / _italic_.
            var builder = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                if (ch is '*' or '_')
                {
                    continue;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }

        private static string ReplaceLinks(string text)
        {
            var builder = new StringBuilder(text.Length);
            var index = 0;

            while (index < text.Length)
            {
                var open = text.IndexOf('[', index);
                if (open < 0)
                {
                    builder.Append(text, index, text.Length - index);
                    break;
                }

                var close = text.IndexOf(']', open + 1);
                if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
                {
                    builder.Append(text, index, open - index + 1);
                    index = open + 1;
                    continue;
                }

                var end = text.IndexOf(')', close + 2);
                if (end < 0)
                {
                    builder.Append(text, index, open - index + 1);
                    index = open + 1;
                    continue;
                }

                var label = text[(open + 1)..close];
                var url = text[(close + 2)..end];

                builder.Append(text, index, open - index);
                builder.Append(label);
                if (url.Length > 0)
                {
                    builder.Append(" (");
                    builder.Append(url);
                    builder.Append(')');
                }

                index = end + 1;
            }

            return builder.ToString();
        }
    }
}
