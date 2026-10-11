using System.ComponentModel;
using System.Drawing.Drawing2D;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Theme;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// One KnowledgeEntry rendered as a card (UI_DESIGN_SPEC section 4.3).
    /// The card raises presentation events only; it never touches persistence.
    /// A single left click opens the entry for editing; a right click selects
    /// it and shows the context menu.
    /// </summary>
    public class EntryCard : Panel
    {
        public const int CardWidth = 340;
        private const int CardHeight = 216;
        private const int CornerRadius = 10;
        private const double OpenDebounceMs = 500;

        private readonly Label _favorite;
        private readonly ContextMenuStrip _contextMenu;
        private bool _isSelected;
        private DateTime _lastOpenAt = DateTime.MinValue;

        public EntryCard(KnowledgeEntry entry)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));

            Width = CardWidth;
            Height = CardHeight;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(CardWidth, CardHeight);
            Margin = new Padding(0, 0, 12, 12);
            Padding = new Padding(14, 10, 14, 10);
            BackColor = AppTheme.Background;
            Cursor = Cursors.Hand;

            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
                true);

            _favorite = new Label
            {
                AutoSize = false,
                Size = new Size(28, 28),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = AppTheme.Fonts.CardTitle,
                Cursor = Cursors.Hand,
            };
            _favorite.Click += (_, _) => FavoriteRequested?.Invoke(this);
            RefreshFavorite();

            _contextMenu = BuildContextMenu();

            var layout = BuildLayout();
            Controls.Add(layout);
            WireMouseEvents(layout);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public KnowledgeEntry Entry { get; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected => _isSelected;

        public event Action<EntryCard>? Selected;

        public event Action<EntryCard>? OpenRequested;

        public event Action<EntryCard>? FavoriteRequested;

        public event Action<EntryCard>? DeleteRequested;

        /// <summary>Applies the selection visuals without raising events.</summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            Invalidate();
        }

        /// <summary>Refreshes the favorite star after the entry was toggled.</summary>
        public void RefreshFavorite()
        {
            _favorite.AccessibleName = AppLocalization.Translate(
                Entry.IsFavorite ? "Remove favorite" : "Add favorite");
            _favorite.Text = Entry.IsFavorite ? "\u2605" : "\u2606";
            _favorite.ForeColor = Entry.IsFavorite ? AppTheme.FavoriteOn : AppTheme.FavoriteOff;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var bounds = new Rectangle(Point.Empty, ClientSize);
            using GraphicsPath path = ModernLayoutHelper.RoundedPath(bounds, CornerRadius);
            using SolidBrush fill = new(_isSelected ? AppTheme.CardSelectedSurface : AppTheme.Surface);
            using Pen border = new(
                _isSelected ? AppTheme.CardSelectedBorder : AppTheme.CardBorder,
                _isSelected ? 2f : 1f);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                RaiseSelected();
                RaiseOpenRequested();
            }
            else if (e.Button == MouseButtons.Right)
            {
                RaiseSelected();
            }
        }

        private void RaiseSelected() => Selected?.Invoke(this);

        private void RaiseOpenRequested()
        {
            if ((DateTime.UtcNow - _lastOpenAt).TotalMilliseconds < OpenDebounceMs)
            {
                return;
            }

            _lastOpenAt = DateTime.UtcNow;
            OpenRequested?.Invoke(this);
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var open = new ToolStripMenuItem("Open/Edit");
            AppLocalization.Bind(open, "Open/Edit");
            open.Click += (_, _) => RaiseOpenRequested();

            var favorite = new ToolStripMenuItem("Toggle favorite");
            AppLocalization.Bind(favorite, "Toggle favorite");
            favorite.Click += (_, _) => FavoriteRequested?.Invoke(this);

            var delete = new ToolStripMenuItem("Delete");
            AppLocalization.Bind(delete, "Delete");
            delete.Click += (_, _) => DeleteRequested?.Invoke(this);

            menu.Items.AddRange(new ToolStripItem[] { open, favorite, delete });
            ContextMenuStrip = menu;
            return menu;
        }

        /// <summary>
        /// Forwards mouse interaction from the child controls that cover the
        /// card surface so selection, single-click open and the context menu
        /// work wherever the user clicks. The favorite affordance is excluded
        /// so it keeps toggling without selecting or opening the entry.
        /// </summary>
        private void WireMouseEvents(Control control)
        {
            if (control == _favorite)
            {
                return;
            }

            control.MouseDown += OnChildMouseDown;
            control.ContextMenuStrip = _contextMenu;

            foreach (Control child in control.Controls)
            {
                WireMouseEvents(child);
            }
        }

        private void OnChildMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                RaiseSelected();
                RaiseOpenRequested();
            }
            else if (e.Button == MouseButtons.Right)
            {
                RaiseSelected();
            }
        }

        private Control BuildLayout()
        {
            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
            };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28f));

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = AppTheme.Surface,
                Margin = Padding.Empty,
            };

            var title = CreateLabel(Entry.Title, AppTheme.Fonts.CardHeader, AppTheme.PrimaryText);
            title.MinimumSize = new Size(0, 34);
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(title, 0, content.RowCount);
            content.RowCount++;

            if (!string.IsNullOrWhiteSpace(Entry.ProjectName))
            {
                AddCardCaption(content, "Project");
                AddCardValue(content, Entry.ProjectName.Trim());
            }

            var tags = KnowledgeEntryTags.Split(Entry.Tags);
            if (tags.Count > 0)
            {
                AddCardCaption(content, "Tags");
                content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                content.Controls.Add(CreateTags(), 0, content.RowCount);
                content.RowCount++;
            }

            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var counts = CreateLabel(Counts(), AppTheme.Fonts.Small, AppTheme.SecondaryText);
            AppLocalization.Bind(counts, Counts);
            counts.Margin = new Padding(0, 4, 0, 0);
            content.Controls.Add(counts, 0, content.RowCount);
            content.RowCount++;

            _favorite.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _favorite.Margin = new Padding(6, 0, 0, 0);

            outer.Controls.Add(content, 0, 0);
            outer.Controls.Add(_favorite, 1, 0);
            return outer;
        }

        private static void AddCardCaption(TableLayoutPanel content, string key)
        {
            var label = CreateLabel(key, AppTheme.Fonts.Small, AppTheme.SecondaryText);
            AppLocalization.Bind(label, key);
            label.Margin = new Padding(0, 2, 0, 0);
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(label, 0, content.RowCount);
            content.RowCount++;
        }

        private static void AddCardValue(TableLayoutPanel content, string value)
        {
            var label = CreateLabel(value, AppTheme.Fonts.Small, AppTheme.PrimaryText);
            label.MinimumSize = new Size(0, 14);
            content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            content.Controls.Add(label, 0, content.RowCount);
            content.RowCount++;
        }

        private Control CreateTags()
        {
            var tags = KnowledgeEntryTags.Split(Entry.Tags);
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = Padding.Empty,
            };

            foreach (var tag in tags)
            {
                flow.Controls.Add(new Label
                {
                    AutoSize = true,
                    Text = $"#{tag}",
                    BackColor = AppTheme.ChipBackground,
                    ForeColor = AppTheme.SecondaryText,
                    Font = AppTheme.Fonts.Small,
                    Padding = new Padding(6, 2, 6, 2),
                    Margin = new Padding(0, 0, 6, 4),
                });
            }

            return flow;
        }

        private string Counts()
        {
            var stepText = Entry.Steps.Count == 1
                ? AppLocalization.Translate("{0} step")
                : AppLocalization.Translate("{0} steps");
            var fileText = Entry.Attachments.Count == 1
                ? AppLocalization.Translate("{0} file")
                : AppLocalization.Translate("{0} files");
            var parts = new List<string>
            {
                string.Format(AppLocalization.Culture, stepText, Entry.Steps.Count),
                string.Format(AppLocalization.Culture, fileText, Entry.Attachments.Count),
            };

            if (Entry.DateResolved.HasValue)
            {
                parts.Add(Entry.DateResolved.Value.ToString("dd MMM yyyy", AppLocalization.Culture));
            }

            return string.Join("  \u00b7  ", parts);
        }

        private static Label CreateLabel(string text, Font font, Color color) => new()
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            Text = text,
            ForeColor = color,
            Font = font,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty,
        };
    }
}