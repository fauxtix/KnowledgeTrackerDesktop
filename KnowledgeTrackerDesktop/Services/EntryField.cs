namespace KnowledgeTrackerDesktop.Services
{
    /// <summary>
    /// The KnowledgeEntry fields a validation error can refer to, so the UI
    /// can steer the user to the exact input that needs fixing.
    /// </summary>
    public enum EntryField
    {
        Title,
        ProjectName,
        Technologies,
        DateResolved,
        Tags,
        Description,
        YouTubeUrl,
    }
}
