using KnowledgeTrackerDesktop.Data;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Shell;
using Microsoft.Extensions.DependencyInjection;
using SQLite;
using MsDi = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;

namespace KnowledgeTrackerDesktop.Bootstrap
{
    /// <summary>
    /// Composition root (ARCHITECTURE.md section 10): the only place where
    /// repositories, application services and UI objects are wired together.
    /// </summary>
    public static class AppComposition
    {
        public static ServiceProvider Build(string? databasePath = null)
        {
            var services = new ServiceCollection();
            Register(services, databasePath);
            return services.BuildServiceProvider();
        }

        public static void Register(IServiceCollection services, string? databasePath = null)
        {
            // Persistence ------------------------------------------------
            services.AddSingleton(new DatabasePath(databasePath));
            services.AddSingleton(sp => new SQLiteAsyncConnection(
                MsDi.GetRequiredService<DatabasePath>(sp).FilePath));
            services.AddSingleton<IKnowledgeRepository, KnowledgeRepositorySQLite>();
            services.AddSingleton<SchemaMigrator>();

            // Application services --------------------------------------
            services.AddSingleton<IKnowledgeEntryService, KnowledgeEntryService>();
            services.AddSingleton<IStatisticsService, StatisticsService>();

            // WinForms shell and navigation -----------------------------
            // The shell resolves and owns the hosted views, so it is registered
            // once as the singleton navigation host.
            services.AddSingleton<frmMain>();
            services.AddSingleton<INavigationHost>(sp => MsDi.GetRequiredService<frmMain>(sp));
            services.AddSingleton<IShellHost>(sp => MsDi.GetRequiredService<frmMain>(sp));
            services.AddSingleton<INavigationService, NavigationServiceWinForms>();
        }
    }
}
