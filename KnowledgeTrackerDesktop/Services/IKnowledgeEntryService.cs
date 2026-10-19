using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// Application operations over the KnowledgeEntry aggregate.
    /// UI layers talk to this service, never to the repository.
    /// </summary>
    public interface IKnowledgeEntryService
    {
        Task<IReadOnlyList<KnowledgeEntry>> GetAllAsync();

        Task<KnowledgeEntry?> GetByIdAsync(int id);

        /// <summary>
        /// Persists the entry together with its Steps and Attachments.
        /// Throws <see cref="ArgumentException"/> when the entry does not
        /// satisfy <see cref="EntryValidator"/>.
        /// </summary>
        Task SaveAsync(KnowledgeEntry entry);

        /// <summary>
        /// Deletes the entry together with its Steps and AttachmentInfo rows.
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Flips IsFavorite and persists the change immediately.
        /// On failure the in-memory entry is left unchanged.
        /// </summary>
        Task ToggleFavoriteAsync(KnowledgeEntry entry);

        /// <summary>
        /// Distinct tags across entries, most used first (editor suggestions).
        /// </summary>
        Task<IReadOnlyList<string>> GetAllTagsAsync();

        /// <summary>
        /// Distinct technologies across entries, most used first.
        /// </summary>
        Task<IReadOnlyList<string>> GetAllTechnologiesAsync();
    }
}
