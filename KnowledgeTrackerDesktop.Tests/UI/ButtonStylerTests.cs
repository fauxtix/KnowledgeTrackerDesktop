using System.Drawing;
using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;
using Xunit;

namespace KnowledgeTrackerDesktop.Tests.UI
{
    /// <summary>
    /// Locks the role-based button hierarchy: every <see cref="ButtonRole"/> must
    /// resolve to a coherent set of colours, and the interactive states (hover,
    /// pressed) must differ from the normal state.
    /// </summary>
    public class ButtonStylerTests
    {
        [Fact]
        public void Primary_UsesMutedBlueWithDistinctStates()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.Primary);

            Assert.Equal(CarbonPalette.Primary, button.Style.BackColor);
            Assert.Equal(CarbonPalette.TextPrimary, button.Style.ForeColor);
            Assert.Equal(CarbonPalette.PrimaryHover, button.Style.HoverBackColor);
            Assert.Equal(CarbonPalette.PrimaryPressed, button.Style.PressedBackColor);
            Assert.NotEqual(button.Style.BackColor, button.Style.HoverBackColor);
            Assert.NotEqual(button.Style.HoverBackColor, button.Style.PressedBackColor);
        }

        [Fact]
        public void Secondary_IsOutlinedWithBorder()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.Secondary);

            Assert.Equal(CarbonPalette.Surface, button.Style.BackColor);
            Assert.NotNull(button.Style.Border);
            Assert.Equal(CarbonPalette.SecondaryHover, button.Style.HoverBackColor);
            Assert.Equal(CarbonPalette.SecondaryPressed, button.Style.PressedBackColor);
            Assert.NotEqual(button.Style.BackColor, button.Style.HoverBackColor);
        }

        [Fact]
        public void Ghost_FallsBackToBackgroundAndHasSubtleOutline()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.Ghost);

            Assert.Equal(AppTheme.Background, button.Style.BackColor);
            Assert.NotNull(button.Style.Border);
            Assert.Equal(CarbonPalette.TextPrimary, button.Style.ForeColor);
            Assert.Equal(CarbonPalette.GhostHover, button.Style.HoverBackColor);
            Assert.NotEqual(button.Style.BackColor, button.Style.HoverBackColor);
        }

        [Fact]
        public void Icon_IsBorderlessAndBlendsWithParent()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.Icon);

            Assert.Equal(AppTheme.Background, button.Style.BackColor);
            Assert.Null(button.Style.Border);
            Assert.Equal(CarbonPalette.TextPrimary, button.Style.ForeColor);
            Assert.Equal(CarbonPalette.IconHover, button.Style.HoverBackColor);
            Assert.Equal(CarbonPalette.TextPrimary, button.Style.HoverForeColor);
            Assert.NotEqual(button.Style.ForeColor, button.Style.DisabledForeColor);
        }

        [Fact]
        public void IconDanger_IsBorderlessWithCarmineIcon()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.IconDanger);

            Assert.Equal(AppTheme.Background, button.Style.BackColor);
            Assert.Null(button.Style.Border);
            Assert.Equal(CarbonPalette.Danger, button.Style.ForeColor);
            Assert.Equal(CarbonPalette.IconDangerHover, button.Style.HoverBackColor);
            Assert.Equal(CarbonPalette.TextPrimary, button.Style.HoverForeColor);
        }

        [Fact]
        public void Icon_MatchesDarkParentBackground()
        {
            using var host = new Panel { BackColor = AppTheme.Surface };
            var button = new SfButton();
            host.Controls.Add(button);

            ButtonStyler.Apply(button, ButtonRole.Icon);

            Assert.Equal(AppTheme.Surface, button.Style.BackColor);
        }

        [Fact]
        public void Icon_FallsBackToDarkWhenParentIsLight()
        {
            using var host = new Panel { BackColor = Color.White };
            var button = new SfButton();
            host.Controls.Add(button);

            ButtonStyler.Apply(button, ButtonRole.Icon);

            Assert.Equal(AppTheme.Background, button.Style.BackColor);
        }

        [Fact]
        public void Danger_UsesMutedRedWithDistinctStates()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.Danger);

            Assert.Equal(CarbonPalette.Danger, button.Style.BackColor);
            Assert.Equal(CarbonPalette.DangerHover, button.Style.HoverBackColor);
            Assert.Equal(CarbonPalette.DangerPressed, button.Style.PressedBackColor);
            Assert.NotEqual(button.Style.BackColor, button.Style.HoverBackColor);
        }

        [Fact]
        public void Nav_IsRestingItemBlendedWithSidebar()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.Nav);

            Assert.Equal(AppTheme.Surface, button.Style.BackColor);
            Assert.Equal(CarbonPalette.TextSecondary, button.Style.ForeColor);
            Assert.Null(button.Style.Border);
        }

        [Fact]
        public void NavActive_HasHighlightAndAccentBorder()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.NavActive);

            Assert.Equal(CarbonPalette.NavActiveBackground, button.Style.BackColor);
            Assert.Equal(CarbonPalette.TextPrimary, button.Style.ForeColor);
            Assert.NotNull(button.Style.Border);
            Assert.NotEqual(button.Style.BackColor, CarbonPalette.Surface);
        }

        [Fact]
        public void RestylingOneButton_DoesNotInvalidateAnotherButtonsBorder()
        {
            using var first = new SfButton();
            using var second = new SfButton();

            ButtonStyler.Apply(first, ButtonRole.Secondary);
            ButtonStyler.Apply(second, ButtonRole.Secondary);

            var secondPen = second.Style.Border;
            Assert.NotNull(secondPen);

            // Changing the first button's role must not dispose the pen that the
            // second button's style still owns (this used to crash OnPaint).
            ButtonStyler.Apply(first, ButtonRole.Primary);

            using var bitmap = new Bitmap(16, 16);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.DrawRectangle(secondPen!, 0, 0, 10, 10);
            Assert.True(secondPen!.Width > 0);
        }

        [Fact]
        public void TryGetRole_ReturnsAssignedRole()
        {
            using var button = new SfButton();
            ButtonStyler.Apply(button, ButtonRole.Primary);

            Assert.True(ButtonStyler.TryGetRole(button, out var role));
            Assert.Equal(ButtonRole.Primary, role);
        }
        [Fact]
        public void ApplyFlat_ProducesFilledAndOutlinedVariants()
        {
            using var primary = new Button();
            using var secondary = new Button();
            ButtonStyler.ApplyFlat(primary, ButtonRole.Primary);
            ButtonStyler.ApplyFlat(secondary, ButtonRole.Secondary);

            Assert.Equal(FlatStyle.Flat, primary.FlatStyle);
            Assert.Equal(CarbonPalette.Primary, primary.BackColor);
            Assert.Equal(CarbonPalette.Surface, secondary.BackColor);
            Assert.Equal(CarbonPalette.SecondaryHover, secondary.FlatAppearance.MouseOverBackColor);
        }
    }
}
