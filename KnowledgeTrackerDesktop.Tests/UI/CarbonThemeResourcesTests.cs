using System.Reflection;
using KnowledgeTrackerDesktop.UI.Theme;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Guards the Syncfusion theme contract: the CarbonDark skins must ship as
    /// embedded XML resources under the exact names Syncfusion expects for
    /// <see cref="CarbonPalette.ThemeName"/>.
    /// </summary>
    public class CarbonThemeResourcesTests
    {
        private const string ResourcePrefix = "Syncfusion.WinForms.Themes.Resources.Themes.CarbonDark.";

        [Theory]
        [InlineData("SfButton.xml")]
        [InlineData("SfDateTimeEdit.xml")]
        public void CarbonDarkSkin_IsEmbeddedWithSyncfusionLogicalName(string fileName)
        {
            var assembly = typeof(CarbonTheme).Assembly;
            var expected = ResourcePrefix + fileName;

            Assert.Contains(expected, assembly.GetManifestResourceNames());

            using var stream = assembly.GetManifestResourceStream(expected);
            Assert.NotNull(stream);

            using var reader = new StreamReader(stream!);
            var content = reader.ReadToEnd();
            Assert.Contains("<Theme ThemeName=\"CarbonDark\"", content);
        }
    }
}