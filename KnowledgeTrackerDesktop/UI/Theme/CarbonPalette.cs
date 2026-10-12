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

        /// <summary>Success / positive accent (green), used for dashboard data accents. <c>#22C55E</c></summary>
        public static readonly Color Success = ColorTranslator.FromHtml("#22C55E");

        /// <summary>Primary text. <c>#F8FAFC</c></summary>
        public static readonly Color TextPrimary = ColorTranslator.FromHtml("#F8FAFC");

        /// <summary>Secondary / muted text. <c>#94A3B8</c></summary>
        public static readonly Color TextSecondary = ColorTranslator.FromHtml("#94A3B8");

        /// <summary>Borders and separators. <c>#2E375E</c></summary>
        public static readonly Color Border = ColorTranslator.FromHtml("#2E375E");

        // --- Semantic button roles (dark-theme button hierarchy) ---

        /// <summary>Primary action base colour (muted steel blue). <c>#427792</c></summary>
        public static readonly Color Primary = ColorTranslator.FromHtml("#427792");

        /// <summary>Primary action hover. <c>#4E86A0</c></summary>
        public static readonly Color PrimaryHover = ColorTranslator.FromHtml("#4E86A0");

        /// <summary>Primary action pressed. <c>#345F76</c></summary>
        public static readonly Color PrimaryPressed = ColorTranslator.FromHtml("#345F76");

        /// <summary>Destructive action base colour (desaturated carmine). <c>#9E5B57</c></summary>
        public static readonly Color Danger = ColorTranslator.FromHtml("#9E5B57");

        /// <summary>Destructive action hover. <c>#B06D69</c></summary>
        public static readonly Color DangerHover = ColorTranslator.FromHtml("#B06D69");

        /// <summary>Destructive action pressed. <c>#7C4642</c></summary>
        public static readonly Color DangerPressed = ColorTranslator.FromHtml("#7C4642");

        /// <summary>Secondary (outlined) button hover surface. <c>#2E375E</c></summary>
        public static readonly Color SecondaryHover = ColorTranslator.FromHtml("#2E375E");

        /// <summary>Secondary (outlined) button pressed surface. <c>#232B49</c></summary>
        public static readonly Color SecondaryPressed = ColorTranslator.FromHtml("#232B49");

        /// <summary>Secondary (outlined) button hover border. <c>#3E4A78</c></summary>
        public static readonly Color SecondaryBorderHover = ColorTranslator.FromHtml("#3E4A78");

        /// <summary>Ghost (tertiary) button hover surface. <c>#232B45</c></summary>
        public static readonly Color GhostHover = ColorTranslator.FromHtml("#232B45");

        /// <summary>Ghost (tertiary) button pressed surface. <c>#1B2233</c></summary>
        public static readonly Color GhostPressed = ColorTranslator.FromHtml("#1B2233");

        /// <summary>Ghost (tertiary) button resting outline. Dimmer than <see cref="Border"/> so the faint button reads lighter than an outlined Secondary. <c>#263056</c></summary>
        public static readonly Color GhostBorder = ColorTranslator.FromHtml("#263056");

        /// <summary>Transparent icon button hover surface (subtle dark grey). <c>#232B45</c></summary>
        public static readonly Color IconHover = ColorTranslator.FromHtml("#232B45");

        /// <summary>Transparent destructive icon button hover surface (soft carmine). <c>#6E403C</c></summary>
        public static readonly Color IconDangerHover = ColorTranslator.FromHtml("#6E403C");

        // --- Navigation rail ---

        /// <summary>Selected navigation item surface. <c>#26315A</c></summary>
        public static readonly Color NavActiveBackground = ColorTranslator.FromHtml("#26315A");

        /// <summary>Selected navigation item hover surface. <c>#2E3C6E</c></summary>
        public static readonly Color NavActiveHover = ColorTranslator.FromHtml("#2E3C6E");

        /// <summary>Selected navigation item pressed surface. <c>#1F2A4D</c></summary>
        public static readonly Color NavActivePressed = ColorTranslator.FromHtml("#1F2A4D");

        /// <summary>Disabled button surface. <c>#2E375E</c></summary>
        public static readonly Color DisabledBack = ColorTranslator.FromHtml("#2E375E");

        /// <summary>Disabled button text. <c>#94A3B8</c></summary>
        public static readonly Color DisabledText = ColorTranslator.FromHtml("#94A3B8");

        /// <summary>Standard corner radius used where a control exposes one.</summary>
        public const int CornerRadius = 8;
    }
}
