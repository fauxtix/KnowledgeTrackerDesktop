using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    public class ModernLayoutHelperTests
    {
        [Fact]
        public void ApplyRoundedCornersLeavesInputsAndLayoutControlsUnclipped()
        {
            using var root = new Panel { Size = new Size(320, 180) };
            using var textBox = new TextBoxExt { Size = new Size(180, 28) };
            using var button = new SfButton { Size = new Size(100, 32) };
            root.Region = new Region(new Rectangle(0, 0, 320, 180));
            textBox.Region = new Region(new Rectangle(0, 0, 180, 28));
            button.Region = new Region(new Rectangle(0, 0, 100, 32));
            root.Controls.Add(textBox);
            root.Controls.Add(button);
            _ = textBox.Handle;

            ModernLayoutHelper.ApplyRoundedCorners(root);

            using var bitmap = new Bitmap(320, 180);
            using Graphics graphics = Graphics.FromImage(bitmap);
            Assert.Equal(new RectangleF(0, 0, 320, 180), root.Region.GetBounds(graphics));
            Assert.Equal(new RectangleF(0, 0, 180, 28), textBox.Region.GetBounds(graphics));
            Assert.Equal(new RectangleF(0, 0, 100, 32), button.Region.GetBounds(graphics));
            Assert.Equal(0, textBox.CornerRadius);
        }

        [Fact]
        public void ApplyRoundedRegionRoundsOnlyTheExplicitlyRequestedControl()
        {
            using var button = new SfButton { Size = new Size(100, 32) };

            ModernLayoutHelper.ApplyRoundedRegion(button);

            Assert.NotNull(button.Region);
        }

        [Fact]
        public void WrapInRoundedPanelParentsAndReturnsCompoundInputWrapper()
        {
            using var input = new SfButton { Size = new Size(100, 28) };
            using var wrapper = ModernLayoutHelper.WrapInRoundedPanel(input);

            Assert.IsType<GradientPanelExt>(wrapper);
            Assert.Same(wrapper, input.Parent);
            Assert.Equal(CarbonPalette.CornerRadius, wrapper.CornerRadius);
        }

        [Fact]
        public void RoundedPathCreatesAValidPathAndClampsLargeRadius()
        {
            using var path = ModernLayoutHelper.RoundedPath(new Rectangle(0, 0, 40, 20), 100);

            Assert.True(path.PointCount > 0);
            RectangleF bounds = path.GetBounds();
            Assert.InRange(Math.Abs(bounds.X), 0f, 0.001f);
            Assert.InRange(Math.Abs(bounds.Y), 0f, 0.001f);
            Assert.InRange(Math.Abs(bounds.Width - 40f), 0f, 0.001f);
            Assert.InRange(Math.Abs(bounds.Height - 20f), 0f, 0.001f);
        }
    }
}
