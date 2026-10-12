using System.ComponentModel;
using System.Drawing.Drawing2D;
using KnowledgeTrackerDesktop.UI.Theme;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// A modern dashboard KPI tile (UI_DESIGN_SPEC section 7): a rounded surface
    /// with a small coloured vertical bar on the left, a large value and a
    /// caption. Presentation only; callers set <see cref="Value"/>.
    /// </summary>
    internal sealed class KpiCard : Control
    {
        private const int CornerRadius = 8;
        private const int BarWidth = 4;
        private const int BarTopInset = 12;
        private const int BarBottomInset = 12;

        private readonly Label _value;
        private readonly Label _caption;

        public KpiCard(string captionKey, Color accent)
        {
            Accent = accent;

            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);

            BackColor = AppTheme.Surface;
            MinimumSize = new Size(140, 92);

            _value = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = "0",
                TextAlign = ContentAlignment.BottomLeft,
                ForeColor = AppTheme.PrimaryText,
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
            };

            _caption = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                ForeColor = AppTheme.SecondaryText,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
            };
            AppLocalization.Bind(_caption, captionKey);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = new Padding(28, 8, 16, 8),
                BackColor = Color.Transparent,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
            layout.Controls.Add(_value, 0, 0);
            layout.Controls.Add(_caption, 0, 1);
            Controls.Add(layout);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Accent { get; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Value
        {
            get => _value.Text;
            set => _value.Text = value;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var bounds = new Rectangle(Point.Empty, ClientSize);
            using GraphicsPath path = ModernLayoutHelper.RoundedPath(bounds, CornerRadius);
            using SolidBrush fill = new(AppTheme.Surface);
            using Pen border = new(AppTheme.CardBorder, 1f);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);

            int barHeight = Math.Max(BarWidth, Height - BarTopInset - BarBottomInset);
            var bar = new Rectangle(0, BarTopInset, BarWidth, barHeight);
            using GraphicsPath barPath = ModernLayoutHelper.RoundedPath(bar, BarWidth);
            using SolidBrush barBrush = new(Accent);
            e.Graphics.FillPath(barBrush, barPath);
        }
    }
}
