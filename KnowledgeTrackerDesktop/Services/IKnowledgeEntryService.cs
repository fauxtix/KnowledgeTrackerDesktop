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

        /// <summary>
        /// Builds the portable export document for the whole knowledge base.
        /// Read-only: it never touches the database beyond loading entries.
        /// </summary>
        Task<KnowledgeExport> ExportAsync();

        /// <summary>
        /// Writes the entries of an import document through the repository in a
        /// single transaction, skipping entries that violate the entry rules and
        /// applying <paramref name="mode"/> to those that already exist locally.
        /// </summary>
        Task<ImportResult> ImportAsync(KnowledgeExport document, ImportMode mode);

        /// <summary>
        /// How many entries in <paramref name="document"/> already exist locally
        /// (matched on title + project) and are valid for import. The UI asks
        /// this before importing so it only prompts about duplicates when there
        /// really are some.
        /// </summary>
        Task<int> CountDuplicatesAsync(KnowledgeExport document);
    }
}
