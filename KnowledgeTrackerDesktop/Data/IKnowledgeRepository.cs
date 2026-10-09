using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.Data
{
    /// <summary>
    /// Persistence contract for the KnowledgeEntry aggregate
    /// (KnowledgeEntry + Steps + Attachments).
    /// </summary>
    public interface IKnowledgeRepository
    {
        Task<List<KnowledgeEntry>> GetAllAsync();

        Task<KnowledgeEntry?> GetByIdAsync(int id);

        /// <summary>
        /// Persists the entry and synchronizes its Steps and Attachments
        /// so the database ends up matching the in-memory collections.
        /// </summary>
        Task SaveAsync(KnowledgeEntry entry);

        /// <summary>
        /// Deletes the entry together with its Steps and AttachmentInfo rows.
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Updates only the IsFavorite flag without touching child collections.
        /// </summary>
        Task SetFavoriteAsync(int id, bool isFavorite);
    }
}
