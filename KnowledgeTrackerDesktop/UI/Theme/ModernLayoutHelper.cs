using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using Syncfusion.Windows.Forms.Tools;

namespace KnowledgeTrackerDesktop.UI.Theme
{
    /// <summary>
    /// Applies rounded-corner presentation to widgets that either have no native
    /// corner support or whose rounding would clip inner content.
    ///
    /// Generic layout controls and inputs are intentionally not reshaped: their
    /// regions can clip content or cause redraw flicker. Round only explicit
    /// surfaces (such as cards) or native GradientPanelExt wrappers.
    /// </summary>
    internal static class ModernLayoutHelper
    {
        private sealed class RegionBinding
        {
            public RegionBinding(Control control, int radius)
            {
                Radius = radius;
                control.Resize += OnResize;
            }

            public int Radius { get; set; }

            private void OnResize(object? sender, EventArgs e)
            {
                if (sender is Control control)
                {
                    ApplyRegion(control, Radius);
                }
            }
        }

        private static readonly ConditionalWeakTable<Control, RegionBinding> RegionBindings = new();

        /// <summary>
        /// Walks <paramref name="root"/> and rounds every control it recognises.
        /// Safe to call repeatedly (re-applying the same values is a no-op).
        /// </summary>
        public static void ApplyRoundedCorners(Control root, int radius = CarbonPalette.CornerRadius)
        {
            ArgumentNullException.ThrowIfNull(root);

            ApplyToControl(root, radius);
            foreach (Control control in root.Controls)
            {
                ApplyRoundedCorners(control, radius);
            }
        }

        private static void ApplyToControl(Control control, int radius)
        {
            if (control is GradientPanelExt panel)
            {
                panel.CornerRadius = radius;
            }
        }

        /// <summary>
        /// Rounds a widget by clipping its window to a rounded rectangle.
        /// Re-applies the region whenever the control is resized.
        /// </summary>
        public static void ApplyRoundedRegion(Control control, int radius = CarbonPalette.CornerRadius)
        {
            ArgumentNullException.ThrowIfNull(control);
            if (radius <= 0)
            {
                return;
            }

            if (!control.IsDisposed)
            {
                if (RegionBindings.TryGetValue(control, out var binding))
                {
                    binding.Radius = radius;
                }
                else
                {
                    RegionBindings.Add(control, new RegionBinding(control, radius));
                }
            }

            ApplyRegion(control, radius);
        }

        private static void ApplyRegion(Control control, int radius)
        {
            if (control.IsDisposed)
            {
                return;
            }

            Size size = control.ClientSize;
            if (size.Width < 1 || size.Height < 1)
            {
                return;
            }

            int r = Math.Max(1, Math.Min(radius, Math.Min(size.Width, size.Height) / 2));
            using GraphicsPath path = RoundedPath(new Rectangle(Point.Empty, size), r);
            control.Region = new Region(path);
        }

        /// <summary>
        /// Builds a graphics path describing a rounded rectangle. The returned
        /// path is the caller's responsibility to dispose.
        /// </summary>
        public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int r = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
            int diameter = r * 2;
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270f, 90f);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Wraps a compound input (a ComboBoxAdv, SfDateTimeEdit, ...) inside a
        /// rounded, bordered panel so the whole control looks like a rounded
        /// editor without clipping its inner text area. The wrapper is sized by
        /// the caller; the input fills it.
        /// </summary>
        public static GradientPanelExt WrapInRoundedPanel(
            Control input,
            int radius = CarbonPalette.CornerRadius,
            int height = 32,
            Color? borderColor = null)
        {
            ArgumentNullException.ThrowIfNull(input);

            Color border = borderColor ?? CarbonPalette.Border;
            var wrapper = new GradientPanelExt
            {
                AutoSize = false,
                Height = height,
                CornerRadius = Math.Max(2, radius),
                BorderGap = 0,
                InnerBorderColor = border,
                InnerBorderThickness = 1,
                BackColor = CarbonPalette.DeepBackground,
                Padding = new Padding(radius, 1, radius, 1),
                Margin = new Padding(0, 4, 8, 4),
            };

            input.Dock = DockStyle.Fill;
            input.Margin = Padding.Empty;
            wrapper.Controls.Add(input);
            return wrapper;
        }
    }
}