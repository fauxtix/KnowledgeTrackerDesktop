using SQLite;

namespace KnowledgeTrackerDesktop.Core
{
    /// <summary>
    /// One structured resolution step of a KnowledgeEntry.
    /// "Order" is a SQLite keyword; sqlite-net quotes identifiers automatically.
    /// </summary>
    [Table("Step")]
    public class Step
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string? Description { get; set; }

        public int Order { get; set; }

        public int KnowledgeEntryId { get; set; }
    }
}
