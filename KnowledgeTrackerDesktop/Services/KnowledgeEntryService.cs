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

            var errors = EntryValidator.Validate(entry);
            if (errors.Count > 0)
            {
                throw new ArgumentException(
                    string.Join(Environment.NewLine, errors.Select(error => error.Message)),
                    nameof(entry));
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

        public Task<IReadOnlyList<string>> GetAllTagsAsync() =>
            _repository.GetAllTagsAsync();

        public Task<IReadOnlyList<string>> GetAllTechnologiesAsync() =>
            _repository.GetAllTechnologiesAsync();

        public async Task<KnowledgeExport> ExportAsync()
        {
            var entries = await _repository.GetAllAsync();
            return KnowledgeExport.FromEntries(entries);
        }

        public async Task<ImportResult> ImportAsync(KnowledgeExport document, ImportMode mode)
        {
            ArgumentNullException.ThrowIfNull(document);

            var classified = await ClassifyAsync(document);

            // SkipDuplicates imports only the new entries; ImportAsCopies also
            // writes the ones that already existed.
            var toInsert = mode == ImportMode.ImportAsCopies
                ? classified.Accepted.Concat(classified.Duplicates).ToList()
                : classified.Accepted;

            await _repository.InsertBatchAsync(toInsert);

            var skipped = mode == ImportMode.SkipDuplicates
                ? classified.Duplicates.Count
                : 0;

            return new ImportResult(toInsert.Count, skipped, classified.Invalid);
        }

        public async Task<int> CountDuplicatesAsync(KnowledgeExport document)
        {
            ArgumentNullException.ThrowIfNull(document);

            var classified = await ClassifyAsync(document);
            return classified.Duplicates.Count;
        }

        /// <summary>
        /// Splits a document's entries into the ones that can be imported, the
        /// ones that already exist locally and the ones that break the entry
        /// rules. Existence is read once and then extended as valid entries are
        /// accepted, so a file that repeats the same entry does not count it
        /// twice. Shared by <see cref="ImportAsync"/> and
        /// <see cref="CountDuplicatesAsync"/> so the prompt and the write agree.
        /// </summary>
        private async Task<ClassifiedImport> ClassifyAsync(KnowledgeExport document)
        {
            var candidates = document.Entries ?? new List<EntryDocument>();
            var identities = (await _repository.GetIdentitiesAsync()).ToList();

            var accepted = new List<KnowledgeEntry>();
            var duplicates = new List<KnowledgeEntry>();
            var invalid = 0;

            foreach (var candidate in candidates)
            {
                var entry = KnowledgeExport.ToEntry(candidate);

                if (EntryValidator.Validate(entry).Count > 0)
                {
                    invalid++;
                    continue;
                }

                var identity = EntryIdentity.Of(entry.Title, entry.ProjectName);
                if (identities.Any(existing => existing.Matches(identity)))
                {
                    duplicates.Add(entry);
                    continue;
                }

                identities.Add(identity);
                accepted.Add(entry);
            }

            return new ClassifiedImport(accepted, duplicates, invalid);
        }

        private sealed record ClassifiedImport(
            List<KnowledgeEntry> Accepted,
            List<KnowledgeEntry> Duplicates,
            int Invalid);
    }
}
