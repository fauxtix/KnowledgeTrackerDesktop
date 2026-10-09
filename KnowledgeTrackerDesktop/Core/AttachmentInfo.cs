using SQLite;

namespace KnowledgeTrackerDesktop.Core
{
    /// <summary>
    /// Metadata of an attachment file belonging to a KnowledgeEntry.
    /// The physical file referenced by FilePath is not managed by the database.
    /// </summary>
    [Table("AttachmentInfo")]
    public class AttachmentInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int KnowledgeEntryId { get; set; }

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public DateTime? DateAdded { get; set; }
    }
}
