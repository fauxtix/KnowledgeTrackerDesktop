using SQLite;

namespace KnowledgeTrackerDesktop.Core
{
    /// <summary>
    /// Aggregate root persisted in the KnowledgeEntry table.
    /// Column names, order and types must stay identical to the legacy schema.
    /// </summary>
    [Table("KnowledgeEntry")]
    public class KnowledgeEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

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

        [Ignore]
        public List<Step> Steps { get; set; } = new();

        [Ignore]
        public List<AttachmentInfo> Attachments { get; set; } = new();
    }
}
