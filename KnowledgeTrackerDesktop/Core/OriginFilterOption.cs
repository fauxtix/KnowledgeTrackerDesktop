namespace KnowledgeTrackerDesktop.Core
{
    /// <summary>
    /// The options of the library origin filter. "All" shows every entry;
    /// "NotRecorded" shows the entries whose origin was never filled in.
    /// </summary>
    public enum OriginFilterOption
    {
        All,
        LocalFolder,
        GitRepository,
        WebPage,
        NotRecorded,
    }
}
