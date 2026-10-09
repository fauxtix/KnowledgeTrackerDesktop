namespace KnowledgeTrackerDesktop.Core
{
    /// <summary>
    /// Centralised list sorting options (PRD section 12: options stay
    /// centralized and easy to extend; sorting never touches the database).
    /// </summary>
    public enum EntrySortMode
    {
        DateResolvedDescending = 0,
        DateResolvedAscending,
        TitleAscending,
        TitleDescending,
        ProjectAscending,
    }
}
