using KnowledgeTrackerDesktop.UI.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Unit tests for the navigation service: history, view lifecycle and
    /// layer-boundary compliance (ARCHITECTURE.md section 12).
    /// </summary>
    public class NavigationServiceWinFormsTests
    {
        private sealed class FakeHost : INavigationHost
        {
            public Control? Displayed { get; private set; }
            public List<Control> DisplayLog { get; } = new();

            public void DisplayView(Control view)
            {
                Displayed = view;
                DisplayLog.Add(view);
            }
        }

        private sealed class FakeView : Control
        {
        }

        private sealed class OtherFakeView : Control
        {
        }

        private interface IStubService
        {
        }

        private sealed class StubService : IStubService
        {
        }

        private sealed class DependentView : Control
        {
            public IStubService Dependency { get; }

            public DependentView(IStubService dependency) => Dependency = dependency;
        }

        private sealed class ArgumentView : Control
        {
            public string Label { get; }

            public ArgumentView(string label) => Label = label;
        }

        private sealed class FilteredView : Control
        {
            public string Filter { get; }

            public FilteredView(string filter) => Filter = filter;
        }

        private sealed class TestContext : IDisposable
        {
            public FakeHost Host { get; } = new();
            public ServiceProvider Provider { get; }
            public NavigationServiceWinForms Navigation { get; }

            public TestContext()
            {
                var services = new ServiceCollection();
                services.AddSingleton<IStubService>(new StubService());
                Provider = services.BuildServiceProvider();
                Navigation = new NavigationServiceWinForms(Provider, Host);
            }

            public void Dispose()
            {
                Navigation.Dispose();
                Provider.Dispose();
            }
        }

        [Fact]
        public void NavigateTo_DisplaysNewViewOnHost()
        {
            using var context = new TestContext();

            context.Navigation.NavigateTo<FakeView>();

            Assert.IsType<FakeView>(context.Host.Displayed);
            Assert.Same(context.Host.Displayed, context.Navigation.CurrentView);
        }

        [Fact]
        public void NavigateTo_KeepsPreviousViewInHistory()
        {
            using var context = new TestContext();
            context.Navigation.NavigateTo<FakeView>();
            var previous = context.Navigation.CurrentView;

            context.Navigation.NavigateTo<OtherFakeView>();

            Assert.True(context.Navigation.CanGoBack);
            Assert.IsType<OtherFakeView>(context.Host.Displayed);
            Assert.NotSame(previous, context.Navigation.CurrentView);
        }

        [Fact]
        public void NavigateToPrimary_ReplacesRouteAndDisposesPreviousHistory()
        {
            using var context = new TestContext();
            context.Navigation.NavigateTo<FakeView>();
            var library = context.Navigation.CurrentView;
            context.Navigation.NavigateTo<OtherFakeView>();
            var editor = context.Navigation.CurrentView;

            context.Navigation.NavigateToPrimary<OtherFakeView>();

            Assert.IsType<OtherFakeView>(context.Navigation.CurrentView);
            Assert.Same(context.Navigation.CurrentView, context.Host.Displayed);
            Assert.True(library!.IsDisposed);
            Assert.True(editor!.IsDisposed);
            Assert.False(context.Navigation.CanGoBack);
        }

        [Fact]
        public void NavigateToContextualView_PreservesOriginForBackNavigation()
        {
            using var context = new TestContext();
            context.Navigation.NavigateToPrimary<FakeView>();
            var statistics = context.Navigation.CurrentView;

            context.Navigation.NavigateTo<FilteredView>("project: samples");
            var filtered = Assert.IsType<FilteredView>(context.Navigation.CurrentView);

            Assert.Equal("project: samples", filtered.Filter);
            Assert.True(context.Navigation.GoBack());
            Assert.Same(statistics, context.Navigation.CurrentView);
        }

        [Fact]
        public void GoBack_ReturnsToPreviousView_AndDisposesAbandonedView()
        {
            using var context = new TestContext();
            context.Navigation.NavigateTo<FakeView>();
            var previous = context.Navigation.CurrentView;
            context.Navigation.NavigateTo<OtherFakeView>();
            var abandoned = context.Navigation.CurrentView;

            var result = context.Navigation.GoBack();

            Assert.True(result);
            Assert.Same(previous, context.Navigation.CurrentView);
            Assert.Same(previous, context.Host.Displayed);
            Assert.True(abandoned!.IsDisposed);
            Assert.False(context.Navigation.CanGoBack);
        }

        [Fact]
        public void GoBack_WithoutHistory_ReturnsFalse()
        {
            using var context = new TestContext();

            Assert.False(context.Navigation.GoBack());
            Assert.Null(context.Navigation.CurrentView);
        }

        [Fact]
        public void NavigateTo_ConstructorArgument_IsPassedThrough()
        {
            using var context = new TestContext();

            context.Navigation.NavigateTo<ArgumentView>("hello");

            var view = Assert.IsType<ArgumentView>(context.Navigation.CurrentView);
            Assert.Equal("hello", view.Label);
        }

        [Fact]
        public void NavigateTo_ConstructorDependency_IsResolvedFromProvider()
        {
            using var context = new TestContext();

            context.Navigation.NavigateTo<DependentView>();

            var view = Assert.IsType<DependentView>(context.Navigation.CurrentView);
            Assert.IsType<StubService>(view.Dependency);
        }

        [Fact]
        public void NavigateTo_RaisesNavigatedEvent()
        {
            using var context = new TestContext();
            var raised = 0;
            context.Navigation.Navigated += (_, _) => raised++;

            context.Navigation.NavigateTo<FakeView>();
            context.Navigation.NavigateTo<OtherFakeView>();
            context.Navigation.GoBack();

            Assert.Equal(3, raised);
        }

        [Fact]
        public void NavigationService_DoesNotDependOnDataOrServicesLayer()
        {
            var type = typeof(NavigationServiceWinForms);

            var dependencies = type
                .GetConstructors(System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic)
                .SelectMany(c => c.GetParameters().Select(p => p.ParameterType))
                .Concat(type.GetFields(System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic)
                    .Select(f => f.FieldType))
                .ToArray();

            Assert.NotEmpty(dependencies);

            foreach (var dependency in dependencies)
            {
                var ns = dependency.Namespace ?? string.Empty;
                Assert.False(ns.StartsWith("KnowledgeTrackerDesktop.Data"),
                    $"{type.Name} depends on Data layer type {dependency.FullName}");
                Assert.False(ns.StartsWith("KnowledgeTrackerDesktop.Services"),
                    $"{type.Name} depends on application service type {dependency.FullName}");
            }
        }
    }
}
