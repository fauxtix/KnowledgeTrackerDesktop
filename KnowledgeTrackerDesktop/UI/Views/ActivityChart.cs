using System.Drawing.Drawing2D;
using KnowledgeTrackerDesktop.Core;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Minimal, theme-aware activity column chart drawn with GDI+ (UI_DESIGN_SPEC
    /// section 7). Replaces a third-party chart so rendering is fully controlled
    /// and always consistent with the CarbonDark palette.
    /// </summary>
    internal sealed class ActivityChart : Control
    {
        private const int LeftGutter = 34;
        private const int RightGutter = 12;
        private const int TopGutter = 12;
        private const int BottomGutter = 26;
        private const int GridSteps = 4;

        private IReadOnlyList<MonthCount> _months = Array.Empty<MonthCount>();
        private int _hoverIndex = -1;
        private Rectangle _lastPlot;

        public ActivityChart()
        {
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
            BackColor = AppTheme.Surface;
        }

        public void SetMonths(IReadOnlyList<MonthCount> months)
        {
            _months = months ?? Array.Empty<MonthCount>();
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var index = HitTest(e.X);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var plot = PlotArea();
            _lastPlot = plot;
            if (_months.Count == 0 || plot.Width <= 0 || plot.Height <= 0)
            {
                return;
            }

            int max = _months.Max(month => month.Count);
            if (max <= 0)
            {
                return;
            }

            int axisMax = NiceMax(max);
            DrawGrid(g, plot, axisMax);

            float slot = (float)plot.Width / _months.Count;
            float barWidth = Math.Min(slot * 0.62f, 44f);
            var valueFont = AppTheme.Fonts.Small;
            using var valueBrush = new SolidBrush(AppTheme.SecondaryText);

            for (int i = 0; i < _months.Count; i++)
            {
                int count = _months[i].Count;
                if (count <= 0)
                {
                    continue;
                }

                float height = (float)count / axisMax * plot.Height;
                float x = plot.Left + (slot * i) + ((slot - barWidth) / 2f);
                float y = plot.Bottom - height;
                var rect = new RectangleF(x, y, barWidth, height);

                bool hovered = i == _hoverIndex;
                using var brush = new LinearGradientBrush(
                    rect,
                    hovered ? AppTheme.AccentAlt : AppTheme.Accent,
                    hovered ? AppTheme.Accent : AppTheme.AccentAlt,
                    LinearGradientMode.Vertical);
                using GraphicsPath barPath = RoundedTop(rect, Math.Min(5f, barWidth / 2f));
                g.FillPath(brush, barPath);

                string label = count.ToString();
                var size = g.MeasureString(label, valueFont);
                if (barWidth >= size.Width + 2f)
                {
                    g.DrawString(
                        label,
                        valueFont,
                        valueBrush,
                        x + ((barWidth - size.Width) / 2f),
                        y - size.Height - 1f);
                }
            }

            DrawMonthLabels(g, plot, slot);
        }

        private Rectangle PlotArea() => new(
            LeftGutter,
            TopGutter,
            Math.Max(0, Width - LeftGutter - RightGutter),
            Math.Max(0, Height - TopGutter - BottomGutter));

        private void DrawGrid(Graphics g, Rectangle plot, int axisMax)
        {
            using var pen = new Pen(AppTheme.CardBorder, 1f);
            var font = AppTheme.Fonts.Small;
            using var brush = new SolidBrush(AppTheme.SecondaryText);

            var labelled = new HashSet<int>();
            for (int step = 0; step <= GridSteps; step++)
            {
                float ratio = (float)step / GridSteps;
                float y = plot.Bottom - (plot.Height * ratio);
                g.DrawLine(pen, plot.Left, y, plot.Right, y);

                int value = (int)Math.Round(axisMax * ratio);
                if (!labelled.Add(value))
                {
                    continue;
                }

                string text = value.ToString();
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, brush, plot.Left - 6f - size.Width, y - (size.Height / 2f));
            }
        }

        private void DrawMonthLabels(Graphics g, Rectangle plot, float slot)
        {
            var font = AppTheme.Fonts.Small;
            using var brush = new SolidBrush(AppTheme.SecondaryText);

            int stride = slot < 30f ? 2 : 1;
            for (int i = 0; i < _months.Count; i++)
            {
                if (i % stride != 0)
                {
                    continue;
                }

                string text = ShortMonth(_months[i]);
                var size = g.MeasureString(text, font);
                float center = plot.Left + (slot * i) + (slot / 2f);
                g.DrawString(text, font, brush, center - (size.Width / 2f), plot.Bottom + 6f);
            }
        }

        private int HitTest(int x)
        {
            if (_months.Count == 0 || _lastPlot.Width <= 0
                || x < _lastPlot.Left || x > _lastPlot.Right)
            {
                return -1;
            }

            float slot = (float)_lastPlot.Width / _months.Count;
            int index = (int)((x - _lastPlot.Left) / slot);
            return index >= 0 && index < _months.Count ? index : -1;
        }

        private static int NiceMax(int max)
        {
            if (max <= GridSteps)
            {
                return max;
            }

            double power = Math.Pow(10, Math.Floor(Math.Log10(max)));
            foreach (double multiple in new[] { 1d, 2d, 2.5d, 5d, 10d })
            {
                double candidate = multiple * power;
                if (candidate >= max)
                {
                    return (int)Math.Ceiling(candidate);
                }
            }

            return max;
        }

        private static GraphicsPath RoundedTop(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            if (rect.Width <= 0f || rect.Height <= 0f)
            {
                return path;
            }

            float r = Math.Max(0f, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f));
            if (r <= 0f)
            {
                path.AddRectangle(rect);
                return path;
            }

            float diameter = r * 2f;
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180f, 90f);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270f, 90f);
            path.AddLine(rect.Right, rect.Top + r, rect.Right, rect.Bottom);
            path.AddLine(rect.Right, rect.Bottom, rect.Left, rect.Bottom);
            path.CloseFigure();
            return path;
        }

        private static string ShortMonth(MonthCount month) =>
            new DateTime(month.Year, month.Month, 1).ToString("MMM", AppLocalization.Culture);
    }
}
