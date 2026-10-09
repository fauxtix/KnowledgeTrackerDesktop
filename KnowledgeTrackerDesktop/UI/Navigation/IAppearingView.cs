namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// Implemented by views that need to load data when they become visible.
    /// Invoked by the shell after the view is displayed; the view itself owns
    /// its loading and error presentation (TASKS #9: prefer Task over async void).
    /// </summary>
    public interface IAppearingView
    {
        Task OnAppearingAsync();
    }
}
