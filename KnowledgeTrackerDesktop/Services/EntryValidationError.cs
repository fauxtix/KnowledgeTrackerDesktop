namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// One validation problem found by <see cref="EntryValidator"/> when a
    /// KnowledgeEntry is about to be persisted. <c>Message</c> is an English
    /// localization key; the UI layer translates it before display.
    /// </summary>
    public readonly record struct EntryValidationError(EntryField Field, string Message);
}
