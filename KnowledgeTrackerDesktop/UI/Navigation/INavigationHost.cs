namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// The shell side of navigation: provides the area where views are shown.
    /// Implemented by frmMain.
    /// </summary>
    public interface INavigationHost
    {
        void DisplayView(Control view);
    }
}
