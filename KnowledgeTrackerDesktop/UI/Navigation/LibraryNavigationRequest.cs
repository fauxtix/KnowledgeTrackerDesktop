namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// Optional filter state supplied when opening the Library from another
    /// screen. Search, sorting and in-session filter changes remain in the
    /// Library view itself.
    /// </summary>
    public sealed record LibraryNavigationRequest(string? ProjectName = null, string? Tag = null);
}
