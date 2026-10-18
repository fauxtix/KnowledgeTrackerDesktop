using System.Reflection;
using KnowledgeTrackerDesktop.Bootstrap;
using KnowledgeTrackerDesktop.Data;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Shell;
using KnowledgeTrackerDesktop.UI.Views;
// The WinForms Designer SDK (transitive via Syncfusion) puts a conflicting
// System.ServiceExtensions.GetRequiredService in scope; the alias keeps the
// Microsoft.Extensions.DependencyInjection implementation unambiguous.
using MsDi = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;

namespace KnowledgeTrackerDesktop
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppLocalization.Initialize();
            var licenseKey = ReadSyncfusionLicenseKey() ?? ReadEmbeddedLicenseKey();
            if (licenseKey is null)
            {
                MessageBox.Show(
                    AppLocalization.Translate("The Syncfusion license key was not found. Set SYNCFUSION_LICENSE_KEY in your User or Machine environment variables, then restart the application."),
                    "KnowledgeTracker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            AppTheme.Fonts.RegisterAsDefault();

            using var services = AppComposition.Build();
            var schemaMigrator = MsDi.GetRequiredService<SchemaMigrator>(services);

            try
            {
                schemaMigrator.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (SchemaVersionTooNewException ex)
            {
                // The database belongs to a newer application version; it was left
                // untouched, so the user only needs to update the application.
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(AppLocalization.Translate(
                    "The knowledge base was created by a newer version of KnowledgeTracker. " +
                    "Update the application and try again."));
                return;
            }
            catch (DatabaseBackupException ex)
            {
                // The safety copy required before upgrading could not be written,
                // so nothing was changed.
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(AppLocalization.Translate(
                    "A safety copy of the knowledge base could not be created, so no changes were made. " +
                    "Check that there is enough free disk space and try again."));
                return;
            }
            catch (SchemaMigrationException ex)
            {
                // The upgrade or repair failed. The database is not left partially
                // repaired and any existing safety copy is reported, never restored.
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(ex.BackupPath is null
                    ? AppLocalization.Translate("The knowledge base could not be upgraded. Please try again.")
                    : string.Format(
                        AppLocalization.Culture,
                        AppLocalization.Translate(
                            "The knowledge base could not be upgraded. A safety copy was saved here:\r\n\r\n{0}"),
                        ex.BackupPath));
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(AppLocalization.Translate(
                    "The knowledge base could not be opened. Please try again."));
                return;
            }

            var shell = MsDi.GetRequiredService<frmMain>(services);
            var navigation = MsDi.GetRequiredService<INavigationService>(services);
            shell.Navigation = navigation;

            shell.Shown += (_, _) =>
                navigation.NavigateToPrimary<ctlMain>(new LibraryNavigationRequest());

            Application.Run(shell);
        }

        private static void ShowStartupError(string message) =>
            MessageBox.Show(
                message,
                "KnowledgeTracker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

        private static string? ReadSyncfusionLicenseKey()
        {
            foreach (var scope in new[]
            {
                EnvironmentVariableTarget.Process,
                EnvironmentVariableTarget.User,
                EnvironmentVariableTarget.Machine,
            })
            {
                var value = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY", scope);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Reads the license key that was embedded as assembly metadata at build
        /// time from the <c>SYNCFUSION_LICENSE_KEY</c> environment variable, so
        /// published builds do not require the variable to be set on the target
        /// machine. Returns <c>null</c> when it was not injected.
        /// </summary>
        private static string? ReadEmbeddedLicenseKey()
        {
            var value = typeof(Program).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "SyncfusionLicenseKey")
                ?.Value;

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
