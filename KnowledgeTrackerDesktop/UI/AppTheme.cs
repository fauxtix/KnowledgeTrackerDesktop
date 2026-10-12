using KnowledgeTrackerDesktop.UI.Theme;

namespace KnowledgeTrackerDesktop.UI
{
    /// <summary>
    /// Centralised, theme-aware colours (UI_DESIGN_SPEC section 10: no isolated
    /// hard-coded colours scattered across controls). Values come from the
    /// <see cref="CarbonPalette"/>. Syncfusion styling is applied separately by
    /// <see cref="CarbonTheme"/>, for Syncfusion-owned controls; this keeps the
    /// custom-drawn surfaces consistent.
    /// </summary>
    internal static class AppTheme
    {
        /// <summary>General canvas / form background.</summary>
        public static Color Background => CarbonPalette.DeepBackground;

        /// <summary>Panels, cards, sidebars and header surfaces.</summary>
        public static Color Surface => CarbonPalette.Surface;

        public static Color CardBorder => CarbonPalette.Border;

        public static Color CardSelectedBorder => CarbonPalette.Accent;

        public static Color CardSelectedSurface => Color.FromArgb(42, 35, 80);

        public static Color PrimaryText => CarbonPalette.TextPrimary;

        public static Color SecondaryText => CarbonPalette.TextSecondary;

        public static Color Accent => CarbonPalette.Accent;

        public static Color AccentAlt => CarbonPalette.AccentAlt;

        public static Color Success => CarbonPalette.Success;

        public static Color ChipBackground => CarbonPalette.Border;

        public static Color FavoriteOn => Color.FromArgb(240, 173, 0);

        public static Color FavoriteOff => CarbonPalette.TextSecondary;

        public static Color Error => Color.FromArgb(248, 113, 113);

        /// <summary>
        /// Shared typography. The application body font is installed before
        /// controls are created; views opt into larger role fonts as needed.
        /// </summary>
        public static class Fonts
        {
            public static Font Body { get; } = new("Segoe UI", 12f);

            public static Font Input { get; } = new("Segoe UI", 13f);

            public static Font CardTitle { get; } = new("Segoe UI", 14f, FontStyle.Bold);

            public static Font CardHeader { get; } = new("Segoe UI", 11f, FontStyle.Bold);

            public static Font SectionTitle { get; } = new("Segoe UI", 12f, FontStyle.Bold);

            public static Font Caption { get; } = new("Segoe UI", 11f);

            public static Font Small { get; } = new("Segoe UI", 10f);

            public static void RegisterAsDefault()
            {
                Application.SetDefaultFont(Body);
            }
        }
    }
}