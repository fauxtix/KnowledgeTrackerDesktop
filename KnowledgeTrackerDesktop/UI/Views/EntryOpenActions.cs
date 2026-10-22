using System.Diagnostics;
using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// The "open" actions a knowledge entry exposes outside the editor: shell
    /// opening an attachment file and an http(s) link. The editor and the
    /// library preview share it, so the safety checks (file exists, http-only
    /// url, guarded <c>Process.Start</c>) live in one place instead of being
    /// copied. It only opens what already exists and never creates files.
    /// </summary>
    internal static class EntryOpenActions
    {
        public static bool IsHttpUrl(string? url) =>
            !string.IsNullOrWhiteSpace(url) &&
            Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        public static void OpenAttachment(AttachmentInfo attachment)
        {
            ArgumentNullException.ThrowIfNull(attachment);

            var path = attachment.FilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                MessageBox.Show(
                    string.Format(
                        AppLocalization.Culture,
                        AppLocalization.Translate("The file could not be found at the recorded location:\r\n\r\n{0}"),
                        path),
                    AppLocalization.Translate("Attachment"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(
                    AppLocalization.Translate("The file could not be opened."),
                    AppLocalization.Translate("Attachment"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        public static void OpenUrl(string? url)
        {
            if (!IsHttpUrl(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url!.Trim()) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(
                    AppLocalization.Translate("The link could not be opened."),
                    AppLocalization.Translate("YouTube"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Opens the recorded project origin: the file explorer for a local
        /// folder, the browser for a repository or a web page. The location is
        /// a record, not a guarantee, so a folder that was moved or deleted is
        /// reported instead of being silently ignored.
        /// </summary>
        public static void OpenProjectOrigin(ProjectOriginKind? kind, string? location)
        {
            if (kind is null || string.IsNullOrWhiteSpace(location))
            {
                return;
            }

            var target = location.Trim();

            if (kind == ProjectOriginKind.LocalFolder)
            {
                OpenLocalFolder(target);
                return;
            }

            OpenUrl(target);
        }

        private static void OpenLocalFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                MessageBox.Show(
                    string.Format(
                        AppLocalization.Culture,
                        AppLocalization.Translate("The folder could not be found at the recorded location:\r\n\r\n{0}"),
                        path),
                    AppLocalization.Translate("Origin"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(
                    AppLocalization.Translate("The folder could not be opened."),
                    AppLocalization.Translate("Origin"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
