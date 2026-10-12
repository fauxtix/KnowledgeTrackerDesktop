using System.Drawing.Drawing2D;
using KnowledgeTrackerDesktop.UI.Theme;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// A dashboard list row (UI_DESIGN_SPEC section 7) used for the top-project
    /// and top-tag lists: the name sits flush left and the count is shown as a
    /// right-aligned pill. The whole row is clickable and highlights on hover.
    /// Presentation only; the caller supplies the click action.
    /// </summary>
    internal sealed class StatListRow : Control
    {
        private const int CornerRadius = 6;
        private const int BadgeHorizontalPadding = 9;
        private const int BadgeGap = 10;
        private const int NameLeft = 4;

        private readonly string _name;
        private readonly int _count;
        private bool _hover;

        public StatListRow(string name, int count, Action onClick)
        {
            ArgumentNullException.ThrowIfNull(onClick);

            _name = name ?? string.Empty;
            _count = count;

            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);

            Height = 30;
            Margin = new Padding(0, 0, 0, 4);
            Cursor = Cursors.Hand;
            BackColor = AppTheme.Background;
            Click += (_, _) => onClick();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(Point.Empty, ClientSize);
            if (_hover)
            {
                using GraphicsPath path = ModernLayoutHelper.RoundedPath(bounds, CornerRadius);
                using SolidBrush hover = new(CarbonPalette.GhostHover);
                g.FillPath(hover, path);
            }

            string countText = _count.ToString(AppLocalization.Culture);
            Size countSize = TextRenderer.MeasureText(countText, AppTheme.Fonts.Small);
            int badgeWidth = countSize.Width + (BadgeHorizontalPadding * 2);
            int badgeHeight = countSize.Height + 2;
            var badge = new Rectangle(
                bounds.Right - badgeWidth - 2,
                (bounds.Height - badgeHeight) / 2,
                badgeWidth,
                badgeHeight);

            using (GraphicsPath badgePath = ModernLayoutHelper.RoundedPath(badge, badgeHeight))
            using (SolidBrush badgeFill = new(AppTheme.ChipBackground))
            {
                g.FillPath(badgeFill, badgePath);
            }

            TextRenderer.DrawText(
                g,
                countText,
                AppTheme.Fonts.Small,
                badge,
                AppTheme.PrimaryText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            var nameBounds = new Rectangle(
                NameLeft,
                0,
                Math.Max(0, badge.Left - BadgeGap - NameLeft),
                bounds.Height);
            TextRenderer.DrawText(
                g,
                _name,
                AppTheme.Fonts.Body,
                nameBounds,
                AppTheme.PrimaryText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }
    }
}
