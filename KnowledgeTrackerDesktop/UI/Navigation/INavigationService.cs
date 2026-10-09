namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// Presentation-only navigation contract (ARCHITECTURE.md section 12).
    /// Implementations must never touch SQLite, repositories or business rules.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>The view currently displayed in the shell, if any.</summary>
        Control? CurrentView { get; }

        bool CanGoBack { get; }

        /// <summary>Raised after every successful navigation (used to refresh shell state).</summary>
        event EventHandler? Navigated;

        /// <summary>
        /// Creates <typeparamref name="TView"/> through dependency injection
        /// (constructor arguments can be supplied via <paramref name="args"/>)
        /// and displays it in the shell.
        /// </summary>
        void NavigateTo<TView>(params object[] args) where TView : Control;

        /// <summary>
        /// Navigates to a primary destination, replacing the current route and
        /// clearing contextual history. Constructor arguments can be supplied via
        /// <paramref name="args"/>.
        /// </summary>
        void NavigateToPrimary<TView>(params object[] args) where TView : Control;

        /// <summary>Returns to the previous view. False when there is no history.</summary>
        bool GoBack();
    }
}
