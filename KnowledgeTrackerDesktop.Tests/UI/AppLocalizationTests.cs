using KnowledgeTrackerDesktop.UI;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI;

public sealed class AppLocalizationTests
{
    [Fact]
    public void PortugueseAndEnglishStringsTranslateInBothDirections()
    {
        Assert.Equal("Biblioteca", AppLocalization.Translate("Library", AppLanguage.Portuguese));
        Assert.Equal("Library", AppLocalization.Translate("Biblioteca", AppLanguage.English));
        Assert.Equal(
            "Eliminar \"{0}\"?\r\n\r\nEsta ação também remove os passos e anexos.",
            AppLocalization.Translate(
                "Delete \"{0}\"?\r\n\r\nThis also removes its steps and attachments.",
                AppLanguage.Portuguese));
        Assert.Equal(
            "Delete \"{0}\"?\r\n\r\nThis also removes its steps and attachments.",
            AppLocalization.Translate(
                "Eliminar \"{0}\"?\r\n\r\nEsta ação também remove os passos e anexos.",
                AppLanguage.English));
    }

    [Fact]
    public void LanguageOptionsUseRequestedCultures()
    {
        Assert.Equal("en-US", AppLocalization.GetCulture(AppLanguage.English).Name);
        Assert.Equal("pt-PT", AppLocalization.GetCulture(AppLanguage.Portuguese).Name);
    }
}
