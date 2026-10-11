using System.Drawing.Imaging;
using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    public sealed class ImageGalleryFormTests
    {
        [Fact]
        public void LoadsOnlyImageAttachmentsIntoTheCarousel()
        {
            var directory = CreateTempDirectory();
            try
            {
                var image1 = CreatePng(directory, "one.png");
                var image2 = CreatePng(directory, "two.jpg");
                var textPath = Path.Combine(directory, "notes.txt");
                File.WriteAllText(textPath, "not an image");

                var attachments = new List<AttachmentInfo>
                {
                    NewAttachment(image1),
                    NewAttachment(image2),
                    NewAttachment(textPath),
                };

                using var form = new ImageGalleryForm(attachments);

                var carousel = GetCarousel(form);
                Assert.True(carousel.ImageSlides);
                Assert.Equal(2, carousel.ImageListCollection.Count);

                // The gallery intentionally renders images larger than the
                // carousel's built-in 200 px thumb size.
                Assert.True(CarouselElement.m_ThumbSize > 200);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void KeepsImageSlidesDisabledWhenNoImageCanBeLoaded()
        {
            var directory = CreateTempDirectory();
            try
            {
                var corrupt = Path.Combine(directory, "broken.png");
                File.WriteAllBytes(corrupt, new byte[] { 1, 2, 3, 4 });

                using var form = new ImageGalleryForm(new[]
                {
                    NewAttachment(corrupt),
                });

                var carousel = GetCarousel(form);
                Assert.False(carousel.ImageSlides);
                Assert.Empty(carousel.ImageListCollection);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "ImageGalleryFormTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static string CreatePng(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            using var bitmap = new Bitmap(120, 80);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Red);
            }

            bitmap.Save(path, ImageFormat.Png);
            return path;
        }

        private static AttachmentInfo NewAttachment(string path) => new()
        {
            FileName = Path.GetFileName(path),
            FilePath = path,
            DateAdded = DateTime.Now,
        };

        private static Carousel GetCarousel(ImageGalleryForm form)
        {
            var field = typeof(ImageGalleryForm).GetField(
                "_carousel",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("The _carousel field was not found.");

            return (Carousel)field.GetValue(form)!;
        }
    }
}