using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Data;

namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// Coordinates application operations over the KnowledgeEntry aggregate.
    /// Holds no SQL: persistence is delegated to <see cref="IKnowledgeRepository"/>.
    /// </summary>
    public sealed class KnowledgeEntryService : IKnowledgeEntryService
    {
        private readonly IKnowledgeRepository _repository;

        public KnowledgeEntryService(IKnowledgeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IReadOnlyList<KnowledgeEntry>> GetAllAsync() =>
            await _repository.GetAllAsync();

        public Task<KnowledgeEntry?> GetByIdAsync(int id) =>
            _repository.GetByIdAsync(id);

        public async Task SaveAsync(KnowledgeEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (string.IsNullOrWhiteSpace(entry.Title))
            {
                throw new ArgumentException("A title is required.", nameof(entry));
            }

            await _repository.SaveAsync(entry);
        }

        public Task DeleteAsync(int id) => _repository.DeleteAsync(id);

        public async Task ToggleFavoriteAsync(KnowledgeEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (entry.Id <= 0)
            {
                throw new ArgumentException(
                    "Cannot toggle the favorite state of an entry that has not been saved.",
                    nameof(entry));
            }

            // Persist only the flag: a favorite toggle must never rewrite
            // child collections of an entry that may not have them loaded.
            var newValue = !entry.IsFavorite;
            await _repository.SetFavoriteAsync(entry.Id, newValue);
            entry.IsFavorite = newValue;
        }
    }
}
