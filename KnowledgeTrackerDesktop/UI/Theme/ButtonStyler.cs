using System.Runtime.CompilerServices;
using Syncfusion.WinForms.Controls;

namespace KnowledgeTrackerDesktop.UI.Theme
{
    /// <summary>
    /// Centralised, role-based styling for <see cref="SfButton"/> (and flat
    /// WinForms <see cref="Button"/>) controls. Each button is assigned a
    /// <see cref="ButtonRole"/> that maps to the CarbonDark palette for the
    /// normal, hover, pressed, focused and disabled states.
    ///
    /// Roles are remembered in a <see cref="ConditionalWeakTable{TKey,TValue}"/>
    /// so <see cref="CarbonTheme"/> can re-apply them after the Syncfusion skin
    /// has been set (assigning <c>ThemeName</c> resets <c>Style</c>).
    ///
    /// Border states use Syncfusion's <see cref="Pen"/>-based members
    /// (<c>Style.Border</c>, <c>HoverBorder</c>, ...), not the XML
    /// <c>BorderColor</c> attributes. Syncfusion takes ownership of these pens
    /// and disposes the previous value whenever one is reassigned, so a fresh
    /// <see cref="Pen"/> must be created for every assignment; sharing a single
    /// instance across buttons would leave the others holding a disposed pen
    /// (which crashes the button's paint).
    /// </summary>
    internal static class ButtonStyler
    {
        private sealed class RoleRegistration
        {
            public RoleRegistration(ButtonRole role) => Role = role;

            public ButtonRole Role { get; set; }
        }

        private static readonly ConditionalWeakTable<SfButton, RoleRegistration> Registrations = new();

        private static Pen BorderPen(Color color) => new(color, 1f);

        /// <summary>
        /// Assigns <paramref name="role"/> to <paramref name="button"/> and
        /// applies the matching colours immediately. Safe to call more than once.
        /// </summary>
        public static void Apply(SfButton button, ButtonRole role)
        {
            ArgumentNullException.ThrowIfNull(button);

            if (Registrations.TryGetValue(button, out var existing))
            {
                existing.Role = role;
            }
            else
            {
                Registrations.Add(button, new RoleRegistration(role));
            }

            ApplyRole(button, role);
        }

        /// <summary>Gets the role previously assigned to <paramref name="button"/>, if any.</summary>
        public static bool TryGetRole(SfButton button, out ButtonRole role)
        {
            ArgumentNullException.ThrowIfNull(button);

            if (Registrations.TryGetValue(button, out var registration))
            {
                role = registration.Role;
                return true;
            }

            role = default;
            return false;
        }

        /// <summary>
        /// Re-applies the registered role, if any. Used by <see cref="CarbonTheme"/>
        /// after the Syncfusion skin has reset the button style.
        /// </summary>
        public static void Reapply(SfButton button)
        {
            ArgumentNullException.ThrowIfNull(button);

            if (button.IsDisposed)
            {
                return;
            }

            if (TryGetRole(button, out var role))
            {
                ApplyRole(button, role);
            }
        }

        /// <summary>
        /// Applies a role to a flat WinForms <see cref="Button"/> (used for the
        /// language selector, which is not a Syncfusion control).
        /// </summary>
        public static void ApplyFlat(Button button, ButtonRole role)
        {
            ArgumentNullException.ThrowIfNull(button);

            var filled = role is ButtonRole.Primary or ButtonRole.Danger;
            var accent = role == ButtonRole.Danger ? CarbonPalette.Danger : CarbonPalette.Primary;
            var hover = role == ButtonRole.Danger ? CarbonPalette.DangerHover : CarbonPalette.PrimaryHover;
            var pressed = role == ButtonRole.Danger ? CarbonPalette.DangerPressed : CarbonPalette.PrimaryPressed;

            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = filled ? accent : CarbonPalette.Surface;
            button.ForeColor = CarbonPalette.TextPrimary;
            button.FlatAppearance.BorderColor = filled ? accent : CarbonPalette.Border;
            button.FlatAppearance.BorderSize = filled ? 2 : 1;
            button.FlatAppearance.MouseOverBackColor = filled ? hover : CarbonPalette.SecondaryHover;
            button.FlatAppearance.MouseDownBackColor = filled ? pressed : CarbonPalette.SecondaryPressed;
        }

        private static void ApplyRole(SfButton button, ButtonRole role)
        {
            button.Font = AppTheme.Fonts.Body;

            switch (role)
            {
                case ButtonRole.Primary:
                    Fill(button, CarbonPalette.Primary, CarbonPalette.PrimaryHover, CarbonPalette.PrimaryPressed);
                    break;
                case ButtonRole.Danger:
                    Fill(button, CarbonPalette.Danger, CarbonPalette.DangerHover, CarbonPalette.DangerPressed);
                    break;
                case ButtonRole.Secondary:
                    Outlined(button);
                    break;
                case ButtonRole.Ghost:
                    Ghost(button);
                    break;
                case ButtonRole.Nav:
                    Nav(button);
                    break;
                case ButtonRole.NavActive:
                    NavActive(button);
                    break;
                case ButtonRole.Icon:
                    Icon(button);
                    break;
                case ButtonRole.IconDanger:
                    IconDanger(button);
                    break;
            }
        }

        private static void Fill(SfButton button, Color back, Color hover, Color pressed)
        {
            var style = button.Style;

            style.BackColor = back;
            style.ForeColor = CarbonPalette.TextPrimary;
            style.HoverBackColor = hover;
            style.HoverForeColor = CarbonPalette.TextPrimary;
            style.PressedBackColor = pressed;
            style.PressedForeColor = CarbonPalette.TextPrimary;
            style.FocusedBackColor = back;
            style.FocusedForeColor = CarbonPalette.TextPrimary;
            style.DisabledBackColor = CarbonPalette.DisabledBack;
            style.DisabledForeColor = CarbonPalette.DisabledText;

            style.Border = null;
            style.HoverBorder = null;
            style.PressedBorder = null;
            style.FocusedBorder = null;
            style.DisabledBorder = null;
        }

        private static void Outlined(SfButton button)
        {
            var style = button.Style;

            style.BackColor = CarbonPalette.Surface;
            style.ForeColor = CarbonPalette.TextPrimary;
            style.HoverBackColor = CarbonPalette.SecondaryHover;
            style.HoverForeColor = CarbonPalette.TextPrimary;
            style.PressedBackColor = CarbonPalette.SecondaryPressed;
            style.PressedForeColor = CarbonPalette.TextPrimary;
            style.FocusedBackColor = CarbonPalette.Surface;
            style.FocusedForeColor = CarbonPalette.TextPrimary;
            style.DisabledBackColor = CarbonPalette.DisabledBack;
            style.DisabledForeColor = CarbonPalette.DisabledText;

            style.Border = BorderPen(CarbonPalette.Border);
            style.HoverBorder = BorderPen(CarbonPalette.SecondaryBorderHover);
            style.PressedBorder = BorderPen(CarbonPalette.Border);
            style.FocusedBorder = BorderPen(CarbonPalette.AccentAlt);
            style.DisabledBorder = BorderPen(CarbonPalette.Border);
        }

        private static void Ghost(SfButton button)
        {
            // Transparent is not honoured by SfButton, so match the parent surface.
            var baseColor = button.Parent?.BackColor ?? AppTheme.Background;
            var style = button.Style;

            style.BackColor = baseColor;
            style.ForeColor = CarbonPalette.TextPrimary;
            style.HoverBackColor = CarbonPalette.GhostHover;
            style.HoverForeColor = CarbonPalette.TextPrimary;
            style.PressedBackColor = CarbonPalette.GhostPressed;
            style.PressedForeColor = CarbonPalette.TextPrimary;
            style.FocusedBackColor = baseColor;
            style.FocusedForeColor = CarbonPalette.TextPrimary;
            style.DisabledBackColor = baseColor;
            style.DisabledForeColor = CarbonPalette.DisabledText;

            style.Border = BorderPen(CarbonPalette.GhostBorder);
            style.HoverBorder = BorderPen(CarbonPalette.SecondaryBorderHover);
            style.PressedBorder = BorderPen(CarbonPalette.Border);
            style.FocusedBorder = BorderPen(CarbonPalette.AccentAlt);
            style.DisabledBorder = BorderPen(CarbonPalette.GhostBorder);
        }

        private static void Icon(SfButton button) =>
            TransparentIcon(button, CarbonPalette.TextPrimary, CarbonPalette.IconHover);

        private static void IconDanger(SfButton button) =>
            TransparentIcon(button, CarbonPalette.Danger, CarbonPalette.IconDangerHover);

        private static void TransparentIcon(SfButton button, Color iconColor, Color hoverSurface)
        {
            // SfButton does not honour Color.Transparent (it falls back to the
            // control's light surface), so match the parent surface to fake the
            // transparent look. The hover/pressed states surface a subtle tint so
            // the affordance is still visible.
            var baseColor = ResolveIconBackground(button);
            var style = button.Style;

            style.GradientBrush = null;
            style.HoverGradientBrush = null;
            style.PressedGradientBrush = null;
            style.FocusedGradientBrush = null;
            style.DisabledGradientBrush = null;

            style.BackColor = baseColor;
            style.ForeColor = iconColor;
            style.HoverBackColor = hoverSurface;
            style.HoverForeColor = CarbonPalette.TextPrimary;
            style.PressedBackColor = hoverSurface;
            style.PressedForeColor = CarbonPalette.TextPrimary;
            style.FocusedBackColor = baseColor;
            style.FocusedForeColor = CarbonPalette.TextPrimary;
            style.DisabledBackColor = baseColor;
            style.DisabledForeColor = CarbonPalette.TextSecondary;

            style.Border = null;
            style.HoverBorder = null;
            style.PressedBorder = null;
            style.FocusedBorder = null;
            style.DisabledBorder = null;
        }

        private static Color ResolveIconBackground(SfButton button)
        {
            var parent = button.Parent?.BackColor;
            return parent is { } color && color.A == 255 && IsDark(color)
                ? color
                : AppTheme.Background;
        }

        private static bool IsDark(Color color) =>
            ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) < 128;

        private static void Nav(SfButton button)
        {
            // Resting navigation item: blends with the sidebar, muted label.
            var baseColor = button.Parent?.BackColor ?? AppTheme.Surface;
            var style = button.Style;

            style.BackColor = baseColor;
            style.ForeColor = CarbonPalette.TextSecondary;
            style.HoverBackColor = CarbonPalette.GhostHover;
            style.HoverForeColor = CarbonPalette.TextPrimary;
            style.PressedBackColor = CarbonPalette.GhostPressed;
            style.PressedForeColor = CarbonPalette.TextPrimary;
            style.FocusedBackColor = baseColor;
            style.FocusedForeColor = CarbonPalette.TextPrimary;
            style.DisabledBackColor = baseColor;
            style.DisabledForeColor = CarbonPalette.DisabledText;

            style.Border = null;
            style.HoverBorder = null;
            style.PressedBorder = null;
            style.FocusedBorder = BorderPen(CarbonPalette.AccentAlt);
            style.DisabledBorder = null;
        }

        private static void NavActive(SfButton button)
        {
            // Selected navigation item: highlighted surface with an accent outline.
            var style = button.Style;

            style.BackColor = CarbonPalette.NavActiveBackground;
            style.ForeColor = CarbonPalette.TextPrimary;
            style.HoverBackColor = CarbonPalette.NavActiveHover;
            style.HoverForeColor = CarbonPalette.TextPrimary;
            style.PressedBackColor = CarbonPalette.NavActivePressed;
            style.PressedForeColor = CarbonPalette.TextPrimary;
            style.FocusedBackColor = CarbonPalette.NavActiveBackground;
            style.FocusedForeColor = CarbonPalette.TextPrimary;
            style.DisabledBackColor = CarbonPalette.DisabledBack;
            style.DisabledForeColor = CarbonPalette.DisabledText;

            style.Border = BorderPen(CarbonPalette.Primary);
            style.HoverBorder = BorderPen(CarbonPalette.Primary);
            style.PressedBorder = BorderPen(CarbonPalette.Primary);
            style.FocusedBorder = BorderPen(CarbonPalette.AccentAlt);
            style.DisabledBorder = BorderPen(CarbonPalette.Border);
        }
    }
}
