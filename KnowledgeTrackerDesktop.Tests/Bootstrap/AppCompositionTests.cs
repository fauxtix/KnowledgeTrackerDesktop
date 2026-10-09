using KnowledgeTrackerDesktop.Bootstrap;
using KnowledgeTrackerDesktop.Data;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.Tests.Data;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Shell;
using KnowledgeTrackerDesktop.UI.Theme;
using System.Reflection;
using MsDi = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.Bootstrap
{
    /// <summary>
    /// Composition-root tests: every layer resolves and lifetimes are correct
    /// (ARCHITECTURE.md section 10).
    /// </summary>
    public class AppCompositionTests
    {
        [Fact]
        public void WinFormsEntryPointRunsSynchronouslyOnStaThread()
        {
            var entryPoint = typeof(KnowledgeTrackerDesktop.Program).GetMethod(
                "Main",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.NotNull(entryPoint);
            Assert.Equal(typeof(void), entryPoint!.ReturnType);
            Assert.NotNull(entryPoint.GetCustomAttribute<STAThreadAttribute>());
        }

        [Fact]
        public void Build_ResolvesEveryApplicationLayer()
        {
            using var temp = new TempDatabase();
            using var provider = AppComposition.Build(temp.DbPath);

            Assert.NotNull(MsDi.GetRequiredService<IKnowledgeRepository>(provider));
            Assert.NotNull(MsDi.GetRequiredService<SchemaMigrator>(provider));
            Assert.NotNull(MsDi.GetRequiredService<IKnowledgeEntryService>(provider));
            Assert.NotNull(MsDi.GetRequiredService<IStatisticsService>(provider));
            Assert.NotNull(MsDi.GetRequiredService<INavigationService>(provider));
            Assert.NotNull(MsDi.GetRequiredService<frmMain>(provider));
        }

        [Fact]
        public void Build_RegistersServicesAsSingletons()
        {
            using var temp = new TempDatabase();
            using var provider = AppComposition.Build(temp.DbPath);

            Assert.Same(MsDi.GetRequiredService<IKnowledgeRepository>(provider),
                MsDi.GetRequiredService<IKnowledgeRepository>(provider));
            Assert.Same(MsDi.GetRequiredService<IKnowledgeEntryService>(provider),
                MsDi.GetRequiredService<IKnowledgeEntryService>(provider));
            Assert.Same(MsDi.GetRequiredService<INavigationService>(provider),
                MsDi.GetRequiredService<INavigationService>(provider));
        }

        [Fact]
        public void Build_UsesTheShellAsNavigationHost()
        {
            using var temp = new TempDatabase();
            using var provider = AppComposition.Build(temp.DbPath);

            var shell = MsDi.GetRequiredService<frmMain>(provider);

            Assert.Same(shell, MsDi.GetRequiredService<INavigationHost>(provider));
        }

        [Fact]
        public void ShellShowsFlagLanguageOptionsInNavigationMenu()
        {
            using var temp = new TempDatabase();
            using var provider = AppComposition.Build(temp.DbPath);
            using var shell = MsDi.GetRequiredService<frmMain>(provider);
            shell.Size = new Size(1280, 820);
            shell.PerformLayout();

            var navigationPanel = (Panel)typeof(frmMain)
                .GetField("_navigationPanel", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(shell)!;
            var navigation = navigationPanel.Controls.OfType<TableLayoutPanel>().Single();
            navigation.PerformLayout();
            var languageOptions = navigation.Controls.OfType<TableLayoutPanel>().Single();
            languageOptions.PerformLayout();
            var options = languageOptions.Controls.OfType<Button>().ToArray();

            Assert.Equal(2, navigation.GetRow(languageOptions));
            Assert.Equal(2, options.Length);
            Assert.All(options, option =>
            {
                Assert.NotNull(option.Image);
                Assert.True(option.Width >= 80);
                Assert.Contains(option.AccessibleName, new[]
                {
                    "English (United States)",
                    "Português (Portugal)",
                });
            });
        }

        [Fact]
        public void Build_ProvidesNavigationServiceOfExpectedType()
        {
            using var temp = new TempDatabase();
            using var provider = AppComposition.Build(temp.DbPath);

            var navigation = MsDi.GetRequiredService<INavigationService>(provider);

            Assert.IsType<NavigationServiceWinForms>(navigation);
        }
    }
}
