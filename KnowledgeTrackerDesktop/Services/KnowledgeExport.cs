using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// Portable JSON representation of the whole knowledge base.
    ///
    /// It is deliberately a separate DTO layer instead of serialising the
    /// SQLite models directly: the persisted models carry database concerns
    /// (primary keys, <c>KnowledgeEntryId</c> links), while the export format
    /// is a document meant to be read by a human, imported on another machine
    /// or fed to a script. The two are kept apart so a schema change does not
    /// silently change the file format.
    /// </summary>
    public sealed class KnowledgeExport
    {
        /// <summary>
        /// Format version, independent from the application version. Readers
        /// ignore files newer than they understand instead of guessing.
        /// </summary>
        /// <remarks>
        /// History:
        /// <list type="bullet">
        /// <item>1 — the original document.</item>
        /// <item>2 — the entries carry the typed project origin
        /// (<c>ProjectOriginKind</c> and <c>ProjectOriginLocation</c>). A
        /// version 1 file still imports: it simply has no origin, which is
        /// the same as "not recorded".</item>
        /// </list>
        /// </remarks>
        public const int CurrentFormatVersion = 2;

        public int FormatVersion { get; set; } = CurrentFormatVersion;

        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        public List<EntryDocument> Entries { get; set; } = new();

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // The origin kind is written as its name ("LocalFolder") so the
            // file stays readable and a script can consume it.
            Converters = { new JsonStringEnumConverter() },
        };

        public string ToJson() => JsonSerializer.Serialize(this, Options);

        /// <summary>
        /// Parses an export document. Throws <see cref="KnowledgeExportFormatException"/>
        /// when the text is not a readable export, so the UI can show a clear
        /// message instead of a raw serialiser error.
        /// </summary>
        public static KnowledgeExport FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new KnowledgeExportFormatException();
            }

            KnowledgeExport? document;
            try
            {
                document = JsonSerializer.Deserialize<KnowledgeExport>(json, Options);
            }
            catch (JsonException)
            {
                throw new KnowledgeExportFormatException();
            }

            if (document is null)
            {
                throw new KnowledgeExportFormatException();
            }

            if (document.FormatVersion > CurrentFormatVersion)
            {
                throw new KnowledgeExportFormatException(newerFormat: true);
            }

            document.Entries ??= new List<EntryDocument>();
            return document;
        }

        /// <summary>Projects the persisted aggregate into the portable document.</summary>
        public static KnowledgeExport FromEntries(IEnumerable<KnowledgeEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            var document = new KnowledgeExport();
            foreach (var entry in entries)
            {
                document.Entries.Add(EntryDocument.FromEntry(entry));
            }

            return document;
        }

        /// <summary>
        /// Rebuilds a persisted aggregate from a document entry. Primary keys
        /// are intentionally not restored: an imported entry is always a new
        /// record, so importing twice can never overwrite local data by id.
        /// </summary>
        public static KnowledgeEntry ToEntry(EntryDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            var entry = new KnowledgeEntry
            {
                Title = document.Title ?? string.Empty,
                Description = document.Description,
                DateResolved = document.DateResolved,
                ProjectName = document.ProjectName,
                ResolutionSteps = document.ResolutionSteps,
                Technologies = document.Technologies,
                Tags = document.Tags,
                Comments = document.Comments,
                CodeSnippet = document.CodeSnippet,
                IsFavorite = document.IsFavorite,
                YouTubeUrl = document.YouTubeUrl,
                ProjectOriginKind = document.ProjectOriginKind,
                ProjectOriginLocation = document.ProjectOriginLocation,
            };

            var order = 0;
            foreach (var step in document.Steps ?? new List<StepDocument>())
            {
                entry.Steps.Add(new Step
                {
                    Description = step.Description ?? string.Empty,
                    Order = ++order,
                });
            }

            foreach (var attachment in document.Attachments ?? new List<AttachmentDocument>())
            {
                entry.Attachments.Add(new AttachmentInfo
                {
                    FileName = attachment.FileName ?? string.Empty,
                    FilePath = attachment.FilePath ?? string.Empty,
                    DateAdded = attachment.DateAdded,
                });
            }

            return entry;
        }
    }

    /// <summary>One entry inside an export document.</summary>
    public sealed class EntryDocument
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? DateResolved { get; set; }

        public string? ProjectName { get; set; }

        public string? ResolutionSteps { get; set; }

        public string? Technologies { get; set; }

        public string? Tags { get; set; }

        public string? Comments { get; set; }

        public string? CodeSnippet { get; set; }

        public bool IsFavorite { get; set; }

        public string? YouTubeUrl { get; set; }

        /// <summary>
        /// The kind of source the knowledge came from, or null when the origin
        /// was not recorded. Written as its name, not as a number.
        /// </summary>
        public ProjectOriginKind? ProjectOriginKind { get; set; }

        /// <summary>
        /// The origin location: an absolute folder path for a local folder, an
        /// http(s) URL for a repository or a web page.
        /// </summary>
        public string? ProjectOriginLocation { get; set; }

        public List<StepDocument> Steps { get; set; } = new();

        public List<AttachmentDocument> Attachments { get; set; } = new();

        public static EntryDocument FromEntry(KnowledgeEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var document = new EntryDocument
            {
                Title = entry.Title,
                Description = entry.Description,
                DateResolved = entry.DateResolved,
                ProjectName = entry.ProjectName,
                ResolutionSteps = entry.ResolutionSteps,
                Technologies = entry.Technologies,
                Tags = entry.Tags,
                Comments = entry.Comments,
                CodeSnippet = entry.CodeSnippet,
                IsFavorite = entry.IsFavorite,
                YouTubeUrl = entry.YouTubeUrl,
                ProjectOriginKind = entry.ProjectOriginKind,
                ProjectOriginLocation = entry.ProjectOriginLocation,
            };

            foreach (var step in entry.Steps ?? new List<Step>())
            {
                document.Steps.Add(new StepDocument { Description = step.Description });
            }

            foreach (var attachment in entry.Attachments ?? new List<AttachmentInfo>())
            {
                document.Attachments.Add(new AttachmentDocument
                {
                    FileName = attachment.FileName,
                    FilePath = attachment.FilePath,
                    DateAdded = attachment.DateAdded,
                });
            }

            return document;
        }
    }

    /// <summary>One resolution step inside an export document.</summary>
    public sealed class StepDocument
    {
        public string? Description { get; set; }
    }

    /// <summary>
    /// One attachment inside an export document. Only the recorded location is
    /// exported; the file itself is never copied, so an import on another
    /// machine keeps the reference and reports it if the file is missing.
    /// </summary>
    public sealed class AttachmentDocument
    {
        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public DateTime? DateAdded { get; set; }
    }
}
