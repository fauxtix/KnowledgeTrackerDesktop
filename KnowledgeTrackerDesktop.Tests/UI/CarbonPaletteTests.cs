using System.Drawing;
using KnowledgeTrackerDesktop.UI.Theme;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Locks the CarbonDark palette to the exact colours from the design spec so
    /// future changes cannot silently drift the whole UI off-brand.
    /// </summary>
    public class CarbonPaletteTests
    {
        [Fact]
        public void ThemeNames_ExposeTheCustomThemeAndItsDarkBase()
        {
            Assert.Equal("CarbonDark", CarbonPalette.ThemeName);
            Assert.Equal("Office2016DarkGray", CarbonPalette.BaseVisualStyle);
        }

        [Theory]
        [InlineData("DeepBackground", 0x12, 0x18, 0x24)]
        [InlineData("Surface", 0x1E, 0x26, 0x40)]
        [InlineData("Accent", 0x7C, 0x3A, 0xED)]
        [InlineData("AccentAlt", 0x06, 0xB6, 0xD4)]
        [InlineData("TextPrimary", 0xF8, 0xFA, 0xFC)]
        [InlineData("TextSecondary", 0x94, 0xA3, 0xB8)]
        [InlineData("Border", 0x2E, 0x37, 0x5E)]
        public void Palette_MatchesSpecifiedHex(string memberName, int r, int g, int b)
        {
            var expected = Color.FromArgb(r, g, b);
            var actual = (Color)typeof(CarbonPalette).GetField(memberName)!.GetValue(null)!;

            Assert.Equal(expected, actual);
        }
    }
}