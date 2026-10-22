using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// Business rules every KnowledgeEntry must satisfy before it can be
    /// persisted, for new and edited entries alike. The rules live in exactly
    /// one place: the editor calls the validator for friendly per-field
    /// feedback and <see cref="KnowledgeEntryService"/> enforces it as the
    /// last line of defense before the repository.
    /// </summary>
    public static class EntryValidator
    {
        private static readonly char[] ListSeparators = { ';', ',' };

        public static IReadOnlyList<EntryValidationError> Validate(KnowledgeEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var errors = new List<EntryValidationError>();

            if (string.IsNullOrWhiteSpace(entry.Title))
            {
                errors.Add(new EntryValidationError(EntryField.Title, "A title is required."));
            }

            if (string.IsNullOrWhiteSpace(entry.ProjectName))
            {
                errors.Add(new EntryValidationError(EntryField.ProjectName, "A project name is required."));
            }

            ValidateList(
                entry.Technologies,
                EntryField.Technologies,
                "At least one technology is required.",
                "Technologies must be a comma or semicolon separated list without empty items.",
                "Technologies cannot contain duplicate items.",
                errors);

            if (entry.DateResolved is null)
            {
                errors.Add(new EntryValidationError(EntryField.DateResolved, "A resolution date is required."));
            }
            else if (entry.DateResolved.Value.Date > DateTime.Today)
            {
                errors.Add(new EntryValidationError(
                    EntryField.DateResolved,
                    "The resolution date cannot be in the future."));
            }

            ValidateList(
                entry.Tags,
                EntryField.Tags,
                "At least one tag is required.",
                "Tags must be a comma or semicolon separated list without empty items.",
                "Tags cannot contain duplicate items.",
                errors);

            if (string.IsNullOrWhiteSpace(entry.Description))
            {
                errors.Add(new EntryValidationError(EntryField.Description, "A description is required."));
            }

            // Optional field: a blank value is fine, but a filled one must be
            // an openable http(s) link (same rule the "Open video" action uses).
            if (!string.IsNullOrWhiteSpace(entry.YouTubeUrl) && !IsHttpUrl(entry.YouTubeUrl))
            {
                errors.Add(new EntryValidationError(
                    EntryField.YouTubeUrl,
                    "The YouTube URL must be a valid http(s) link."));
            }

            ValidateProjectOrigin(entry, errors);

            return errors;
        }

        /// <summary>
        /// The origin is optional, but its two parts go together: a location
        /// without a kind cannot be interpreted (is it a folder or a link?) and
        /// a kind without a location points nowhere.
        /// </summary>
        private static void ValidateProjectOrigin(
            KnowledgeEntry entry,
            List<EntryValidationError> errors)
        {
            var kind = entry.ProjectOriginKind;
            var location = entry.ProjectOriginLocation?.Trim();

            if (kind is null)
            {
                if (!string.IsNullOrWhiteSpace(location))
                {
                    errors.Add(new EntryValidationError(
                        EntryField.OriginKind,
                        "Select the origin type for the recorded location."));
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                errors.Add(new EntryValidationError(
                    EntryField.OriginLocation,
                    "The origin location is required when an origin type is selected."));
                return;
            }

            if (!ProjectOriginRules.TryValidate(kind.Value, location, out var message))
            {
                errors.Add(new EntryValidationError(EntryField.OriginLocation, message));
            }
        }

        /// <summary>
        /// Tags and Technologies are single columns stored as separated lists
        /// (see <see cref="KnowledgeEntryTags"/>): every item must be non-empty
        /// and unique, so a typo does not silently duplicate an item.
        /// </summary>
        private static void ValidateList(
            string? value,
            EntryField field,
            string missingMessage,
            string emptyItemMessage,
            string duplicateMessage,
            List<EntryValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new EntryValidationError(field, missingMessage));
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in value.Split(ListSeparators, StringSplitOptions.None))
            {
                var item = part.Trim();
                if (item.Length == 0)
                {
                    errors.Add(new EntryValidationError(field, emptyItemMessage));
                    return;
                }

                if (!seen.Add(item))
                {
                    errors.Add(new EntryValidationError(field, duplicateMessage));
                    return;
                }
            }
        }

        // Mirrors UI.Views.EntryOpenActions.IsHttpUrl without taking a
        // dependency on the UI layer.
        private static bool IsHttpUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url) &&
            Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
