namespace KnowledgeTrackerDesktop.UI.Theme
{
    /// <summary>
    /// Single source of truth for the CarbonDark palette. Every colour used by
    /// the custom theme (Syncfusion skin XML and code-level overrides) comes
    /// from here so the whole UI stays consistent.
    /// </summary>
    internal static class CarbonPalette
    {
        /// <summary>Name of the custom Syncfusion theme (embedded XML resources).</summary>
        public const string ThemeName = "CarbonDark";

        /// <summary>
        /// Built-in dark visual style used as the base. Syncfusion legacy
        /// controls only accept <c>VisualTheme</c> enum names, so the custom
        /// <see cref="ThemeName"/> cannot be applied to them; the base style
        /// keeps them dark and the code-level overrides refine their colours.
        /// </summary>
        public const string BaseVisualStyle = "Office2016DarkGray";

        /// <summary>Forms and general canvas background. <c>#121824</c></summary>
        public static readonly Color DeepBackground = ColorTranslator.FromHtml("#121824");

        /// <summary>Containers, cards, sidebars and headers. <c>#1E2640</c></summary>
        public static readonly Color Surface = ColorTranslator.FromHtml("#1E2640");

        /// <summary>Primary accent (purple). <c>#7C3AED</c></summary>
        public static readonly Color Accent = ColorTranslator.FromHtml("#7C3AED");

        /// <summary>Secondary accent (cyan). <c>#06B6D4</c></summary>
        public static readonly Color AccentAlt = ColorTranslator.FromHtml("#06B6D4");

        /// <summary>Primary text. <c>#F8FAFC</c></summary>
        public static readonly Color TextPrimary = ColorTranslator.FromHtml("#F8FAFC");

        /// <summary>Secondary / muted text. <c>#94A3B8</c></summary>
        public static readonly Color TextSecondary = ColorTranslator.FromHtml("#94A3B8");

        /// <summary>Borders and separators. <c>#2E375E</c></summary>
        public static readonly Color Border = ColorTranslator.FromHtml("#2E375E");

        /// <summary>Standard corner radius used where a control exposes one.</summary>
        public const int CornerRadius = 8;
    }
}
