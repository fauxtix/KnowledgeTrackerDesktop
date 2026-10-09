using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Views;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Guards the dependency-injection contract of the shell views: each view
    /// exposes a single public constructor whose parameters are the services
    /// registered in the composition root plus, for the Library, the navigation
    /// request supplied through <see cref="INavigationService"/>.
    /// </summary>
    public class ViewCompositionTests
    {
        [Fact]
        public void Library_HasSingleDiFriendlyConstructor()
        {
            var parameters = SingleConstructorParameters(typeof(ctlMain));

            Assert.Equal(
                new[]
                {
                    typeof(IKnowledgeEntryService),
                    typeof(INavigationService),
                    typeof(IShellHost),
                    typeof(LibraryNavigationRequest),
                },
                parameters);
        }

        [Fact]
        public void Statistics_HasSingleDiFriendlyConstructor()
        {
            var parameters = SingleConstructorParameters(typeof(ctlStatistics));

            Assert.Equal(
                new[]
                {
                    typeof(IStatisticsService),
                    typeof(INavigationService),
                    typeof(IShellHost),
                },
                parameters);
        }

        private static Type[] SingleConstructorParameters(Type viewType)
        {
            var constructors = viewType.GetConstructors();
            var constructor = Assert.Single(constructors);
            return constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        }
    }
}
