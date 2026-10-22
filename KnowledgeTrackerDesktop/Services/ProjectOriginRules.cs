using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// The rules behind a recorded project origin. The location is
    /// interpreted according to the kind, so every kind has its own format
    /// rule: a local folder needs an absolute path, a repository or a web
    /// page needs an http(s) URL.
    ///
    /// The location is a record, not a guarantee: the folder may have been
    /// moved and the site may be gone. That is why nothing here checks
    /// whether the location still exists; the open action reports a missing
    /// target instead of failing silently, exactly like an attachment.
    /// </summary>
    public static class ProjectOriginRules
    {
        /// <summary>
        /// The human readable name of a kind. The UI localizes it.
        /// </summary>
        public static string DescribeKind(ProjectOriginKind kind) => kind switch
        {
            ProjectOriginKind.LocalFolder => "Local folder",
            ProjectOriginKind.GitRepository => "Git repository",
            ProjectOriginKind.WebPage => "Web page",
            _ => "Unknown",
        };

        /// <summary>
        /// The kinds a user can pick, in the order the editor lists them.
        /// </summary>
        public static IReadOnlyList<ProjectOriginKind> Kinds { get; } = new[]
        {
            ProjectOriginKind.LocalFolder,
            ProjectOriginKind.GitRepository,
            ProjectOriginKind.WebPage,
        };

        /// <summary>
        /// Checks that a location can be interpreted as the given kind.
        /// Returns false and the reason when it cannot.
        /// </summary>
        public static bool TryValidate(ProjectOriginKind kind, string? location, out string message)
        {
            var trimmed = location?.Trim() ?? string.Empty;

            switch (kind)
            {
                case ProjectOriginKind.LocalFolder:
                    if (!IsAbsolutePath(trimmed))
                    {
                        message = "The origin location must be an absolute folder path.";
                        return false;
                    }

                    break;

                case ProjectOriginKind.GitRepository:
                    if (!IsHttpUrl(trimmed))
                    {
                        message = "The repository URL must be a valid http(s) link.";
                        return false;
                    }

                    break;

                case ProjectOriginKind.WebPage:
                    if (!IsHttpUrl(trimmed))
                    {
                        message = "The page URL must be a valid http(s) link.";
                        return false;
                    }

                    break;

                default:
                    message = "The origin type is not supported.";
                    return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// True when the location is an absolute path, either a drive path
        /// (<c>C:\folder</c>) or a UNC path (<c>\\server\share</c>).
        /// </summary>
        public static bool IsAbsolutePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                return Path.IsPathRooted(path.Trim());
            }
            catch (ArgumentException)
            {
                // A path with characters the file system does not allow can
                // never be opened, so it is treated as invalid.
                return false;
            }
        }

        /// <summary>
        /// True for an absolute http(s) URL. SSH style locations
        /// (<c>git@host:owner/repo.git</c>) are deliberately not accepted:
        /// the application opens an origin, it does not clone it.
        /// </summary>
        public static bool IsHttpUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url) &&
            Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        /// <summary>
        /// A short, readable form of a location, for a card or a preview: the
        /// folder name for a local folder, the last two segments of the path
        /// for a link ("owner/repo").
        /// </summary>
        public static string DescribeLocation(ProjectOriginKind kind, string? location)
        {
            var trimmed = location?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            return kind == ProjectOriginKind.LocalFolder
                ? DescribeFolder(trimmed)
                : DescribeUrl(trimmed);
        }

        private static string DescribeFolder(string path)
        {
            var withoutSeparators = path.TrimEnd('\\', '/');
            if (withoutSeparators.Length == 0)
            {
                return path;
            }

            var name = Path.GetFileName(withoutSeparators);
            return string.IsNullOrWhiteSpace(name) ? withoutSeparators : name;
        }

        private static string DescribeUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return url;
            }

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                return $"{segments[^2]}/{segments[^1]}";
            }

            if (segments.Length == 1)
            {
                return $"{uri.Host}/{segments[0]}";
            }

            return uri.Host;
        }
    }
}
