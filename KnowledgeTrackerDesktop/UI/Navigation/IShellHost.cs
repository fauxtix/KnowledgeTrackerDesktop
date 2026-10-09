namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// The page-chrome side of the shell: the header title and the contextual
    /// header actions (Back/Cancel/Save, "+ New Entry", ...). Kept separate from
    /// <see cref="INavigationHost"/> so views only take what they need.
    /// </summary>
    public interface IShellHost
    {
        void SetPageTitle(string title);

        /// <summary>Replaces the header actions, disposing the previous ones.</summary>
        void SetHeaderActions(params Control[] actions);

        void ClearHeaderActions();
    }
}
