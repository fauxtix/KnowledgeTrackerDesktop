using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeTrackerDesktop.UI.Navigation
{
    /// <summary>
    /// WinForms implementation of <see cref="INavigationService"/> (ARCHITECTURE.md section 12).
    /// Owns the view history and view lifecycle (creation via DI, disposal of abandoned
    /// views). Contains no SQLite or business logic.
    /// </summary>
    public sealed class NavigationServiceWinForms : INavigationService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly INavigationHost _host;
        private readonly Stack<Control> _history = new();
        private Control? _current;

        public NavigationServiceWinForms(IServiceProvider services, INavigationHost host)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public Control? CurrentView => _current;

        public bool CanGoBack => _history.Count > 0;

        public event EventHandler? Navigated;

        public void NavigateTo<TView>(params object[] args) where TView : Control
        {
            var view = CreateView<TView>(args);

            if (_current is not null)
            {
                _history.Push(_current);
            }

            _current = view;
            _host.DisplayView(view);
            Navigated?.Invoke(this, EventArgs.Empty);
        }

        public void NavigateToPrimary<TView>(params object[] args) where TView : Control
        {
            if (_current?.GetType() == typeof(TView) && _history.Count == 0)
            {
                return;
            }

            var view = CreateView<TView>(args);
            ClearHistory();

            var abandoned = _current;
            _current = view;
            _host.DisplayView(view);
            if (!ReferenceEquals(abandoned, view))
            {
                abandoned?.Dispose();
            }

            Navigated?.Invoke(this, EventArgs.Empty);
        }

        public bool GoBack()
        {
            if (_history.Count == 0)
            {
                return false;
            }

            var abandoned = _current;
            _current = _history.Pop();
            _host.DisplayView(_current);
            abandoned?.Dispose();
            Navigated?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void Dispose()
        {
            ClearHistory();
        }

        private Control CreateView<TView>(object[]? args) where TView : Control =>
            (Control)ActivatorUtilities.CreateInstance(
                _services, typeof(TView), args ?? Array.Empty<object>());

        private void ClearHistory()
        {
            while (_history.Count > 0)
            {
                _history.Pop().Dispose();
            }
        }
    }
}
