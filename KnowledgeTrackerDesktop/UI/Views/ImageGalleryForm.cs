using System.Diagnostics;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Modal image gallery for the image attachments of a KnowledgeEntry. It
    /// shows the pictures in a Syncfusion <see cref="Carousel"/> and only
    /// presents formats GDI+ can render. The form is read-only: it never
    /// mutates the entry or its attachments.
    /// </summary>
    public sealed class ImageGalleryForm : SfForm
    {
        // The Syncfusion carousel pre-scales every image to fit a ThumbSize x
        // ThumbSize box (CarouselElement.m_ThumbSize, default 200) before it
        // draws the slide. Raising it makes the images render larger.
        private const int ThumbSize = 200;

        private static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".jpe", ".png", ".gif", ".bmp", ".tif", ".tiff", ".ico",
        };

        private readonly Carousel _carousel = new();
        private readonly Label _caption = new();
        private readonly SfButton _open = new();
        private readonly List<Bitmap> _bitmaps = new();
        private readonly Dictionary<Image, AttachmentInfo> _imageToAttachment = new();

        private AttachmentInfo? _focused;

        public ImageGalleryForm(IReadOnlyList<AttachmentInfo> images)
        {
            ArgumentNullException.ThrowIfNull(images);

            Text = AppLocalization.Translate("Image gallery");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(940, 660);
            MinimumSize = new Size(560, 420);
            BackColor = AppTheme.Background;
            MinimizeBox = false;
            MaximizeBox = false;

            BuildLayout();
            LoadImages(images);
            UpdateCaption(_carousel.ActiveImage);

            CarbonTheme.Apply(this);
        }

        /// <summary>
        /// True when the attachment looks like an image, based on its file
        /// extension. Actual format support is verified again when the picture
        /// is decoded.
        /// </summary>
        public static bool IsImage(AttachmentInfo attachment)
        {
            ArgumentNullException.ThrowIfNull(attachment);
            return IsImagePath(attachment.FilePath);
        }

        private static bool IsImagePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var extension = Path.GetExtension(path.Trim());
            return ImageExtensions.Any(candidate =>
                string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase));
        }

        private void BuildLayout()
        {
            _carousel.Dock = DockStyle.Fill;
            _carousel.BackColor = AppTheme.Background;
            _carousel.ImageshadeColor = AppTheme.Background;
            _carousel.HighlightColor = AppTheme.Accent;
            _carousel.ShowImagePreview = true;
            _carousel.ShowImageShadow = true;
            _carousel.UseOriginalImageinPreview = false;
            _carousel.OnCarouselItemFocused += (_, e) => UpdateCaption(e.FocusedImage);
            _carousel.OnCarouselItemSelectionChanged += (_, e) => UpdateCaption(e.SelectedImage);

            _caption.AutoSize = false;
            _caption.Dock = DockStyle.Fill;
            _caption.TextAlign = ContentAlignment.MiddleLeft;
            _caption.ForeColor = AppTheme.PrimaryText;
            _caption.Font = AppTheme.Fonts.Body;
            _caption.Margin = Padding.Empty;

            _open.AutoSize = false;
            _open.Size = new Size(110, 32);
            _open.Enabled = false;
            _open.Anchor = AnchorStyles.Right;
            _open.Margin = new Padding(8, 0, 0, 0);
            AppLocalization.Bind(_open, "Open");
            _open.Click += (_, _) => OpenFocused();

            var close = new SfButton
            {
                AutoSize = false,
                Size = new Size(110, 32),
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(8, 0, 0, 0),
            };
            AppLocalization.Bind(close, "Close");

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
            };
            actions.Controls.Add(_open);
            actions.Controls.Add(close);

            var bar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = AppTheme.Surface,
            };
            bar.Controls.Add(_caption);
            bar.Controls.Add(actions);

            Controls.Add(_carousel);
            Controls.Add(bar);

            AcceptButton = close;
            CancelButton = close;
        }

        private void LoadImages(IReadOnlyList<AttachmentInfo> images)
        {
            // Must be set before the collection is populated: each element
            // snapshots the static thumb size when it is constructed.
            CarouselElement.m_ThumbSize = ThumbSize;

            foreach (var attachment in images)
            {
                if (!IsImagePath(attachment.FilePath))
                {
                    continue;
                }

                var bitmap = TryLoadBitmap(attachment.FilePath);
                if (bitmap is null)
                {
                    continue;
                }

                _bitmaps.Add(bitmap);
                _imageToAttachment[bitmap] = attachment;
                _carousel.ImageListCollection.Add(new CarouselImage { ItemImage = bitmap });
            }

            // Enabling ImageSlides only after the collection is populated is
            // required: the Syncfusion carousel reloads its images solely from
            // the ImageSlides setter, so enabling it earlier would load an
            // empty list and show nothing.
            _carousel.ImageSlides = _bitmaps.Count > 0;
        }

        private static Bitmap? TryLoadBitmap(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                using var stream = File.OpenRead(path);
                using var image = Image.FromStream(stream);
                return new Bitmap(image);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to load image '{path}': {ex}");
                return null;
            }
        }

        private void UpdateCaption(Image? image)
        {
            _focused = image is not null && _imageToAttachment.TryGetValue(image, out var attachment)
                ? attachment
                : null;

            _caption.Text = _focused?.FileName
                ?? (_bitmaps.Count == 0
                    ? AppLocalization.Translate("No images to display.")
                    : string.Empty);

            _open.Enabled = _focused is not null;
        }

        private void OpenFocused()
        {
            var path = _focused?.FilePath;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _imageToAttachment.Clear();

                foreach (var bitmap in _bitmaps)
                {
                    bitmap.Dispose();
                }

                _bitmaps.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
