using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.Windows.Forms.Tools;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    public class CarbonComboBoxThemeTests
    {
        [Fact]
        public void FrameDisplaysSelectedItemAndProvidesVisibleDropdownIndicator()
        {
            using var comboBox = new ComboBoxAdv();
            comboBox.Items.AddRange(new object[] { "All projects", "Development" });
            comboBox.SelectedIndex = 1;
            using var frame = new ComboBoxFrame(comboBox, 214);
            frame.ApplyCarbonStyle();

            var display = frame.Controls.OfType<Label>()
                .Single(label => label.AccessibleName == "Selected option");
            var indicator = frame.Controls.OfType<Label>()
                .Single(label => label.Text == "\u25be");

            Assert.Equal("Development", display.Text);
            Assert.Equal(CarbonPalette.TextPrimary, display.ForeColor);
            Assert.Equal(new Padding(2), frame.Padding);
            Assert.Equal(48, frame.Height);
            Assert.False(comboBox.Visible);

            comboBox.SelectedIndex = 0;
            Assert.Equal("All projects", display.Text);
            Assert.Same(frame, comboBox.Parent);
            Assert.True(indicator.Visible);
        }
    }
}
