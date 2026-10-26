using System.ComponentModel;
using System.Drawing.Drawing2D;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Theme;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// One KnowledgeEntry rendered as a card (UI_DESIGN_SPEC section 4.3).
    /// The card raises presentation events only; it never touches persistence.
    /// A single left click selects the entry so the library can show it in the
    /// preview pane; a double click, the "Open/Edit" context menu item or the
    /// preview's edit action opens the entry for editing. A right click also
    /// selects it and shows the context menu.
    /// </summary>
    public class EntryCard : Panel
    {
        public const int CardWidth = 340;
        private const int CardHeight = 216;
        private const int CornerRadius = 10;
        private const double OpenDebounceMs = 500;

        private readonly Label _favorite;
        private readonly Label _delete;
        private readonly Label _duplicate;
        private readonly Font _deleteFont = new("Segoe MDL2 Assets", 12f);
        private readonly ContextMenuStrip _contextMenu;
        private Label? _originLink;
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

            _delete = new Label
            {
                AutoSize = false,
                Size = new Size(28, 28),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = _deleteFont,
                Text = "\uE74D",
                ForeColor = AppTheme.SecondaryText,
                Cursor = Cursors.Hand,
                AccessibleName = AppLocalization.Translate("Delete"),
            };
            _delete.Click += (_, _) => DeleteRequested?.Invoke(this);
            _delete.MouseEnter += (_, _) => _delete.ForeColor = AppTheme.Error;
            _delete.MouseLeave += (_, _) => _delete.ForeColor = AppTheme.SecondaryText;

            // Duplicate copies the entry into the editor without touching the
            // original, so it is a neutral action, not a destructive one.
            _duplicate = new Label
            {
                AutoSize = false,
                Size = new Size(28, 28),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = _deleteFont,
                Text = "\uE8C8",
                ForeColor = AppTheme.SecondaryText,
                Cursor = Cursors.Hand,
                AccessibleName = AppLocalization.Translate("Duplicate"),
            };
            _duplicate.Click += (_, _) => DuplicateRequested?.Invoke(this);
            _duplicate.MouseEnter += (_, _) => _duplicate.ForeColor = AppTheme.PrimaryText;
            _duplicate.MouseLeave += (_, _) => _duplicate.ForeColor = AppTheme.SecondaryText;

            _contextMenu = BuildContextMenu();

            var layout = BuildLayout();
            Controls.Add(layout);
            WireMouseEvents(layout);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public KnowledgeEntry Entry { get; }

        /// <summary>
        /// Releases the resources the card owns itself (the context menu and the
        /// MDL2 icon font). Shared theme fonts are never disposed here.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _contextMenu.Dispose();
                _deleteFont.Dispose();
            }

            base.Dispose(disposing);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected => _isSelected;

        public event Action<EntryCard>? Selected;

        public event Action<EntryCard>? OpenRequested;

        public event Action<EntryCard>? FavoriteRequested;

        public event Action<EntryCard>? DeleteRequested;

        public event Action<EntryCard>? DuplicateRequested;

        /// <summary>
        /// Raised when the user clicks the origin shown on the card, so the
        /// source of the knowledge can be opened without opening the entry.
        /// </summary>
        public event Action<EntryCard>? OriginOpenRequested;

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
            HandleGesture(e.Button, e.Clicks);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            HandleDoubleClick(e.Button);
        }

        /// <summary>
        /// Applies a mouse gesture coming from the card surface or from any of the
        /// child controls that cover it. It is kept as an explicit method so the
        /// gesture rules (select on left click, open on the second click of a
        /// double click, select on right click) can be asserted directly instead
        /// of only through simulated Windows mouse messages.
        /// </summary>
        internal void HandleGesture(MouseButtons button, int clicks)
        {
            if (button == MouseButtons.Left)
            {
                RaiseSelected();

                // A second click of a double click arrives as a mouse-down with
                // Clicks == 2 (WM_LBUTTONDBLCLK); it is the "open" gesture.
                if (clicks >= 2)
                {
                    RaiseOpenRequested();
                }
            }
            else if (button == MouseButtons.Right)
            {
                RaiseSelected();
            }
        }

        /// <summary>
        /// Applies the double-click part of a gesture from any surface. The open
        /// is debounced, so the second mouse-down of the same double click and
        /// this event collapse into a single open.
        /// </summary>
        internal void HandleDoubleClick(MouseButtons button)
        {
            if (button == MouseButtons.Left)
            {
                RaiseOpenRequested();
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

            var duplicate = new ToolStripMenuItem("Duplicate");
            AppLocalization.Bind(duplicate, "Duplicate");
            duplicate.Click += (_, _) => DuplicateRequested?.Invoke(this);

            var delete = new ToolStripMenuItem("Delete");
            AppLocalization.Bind(delete, "Delete");
            delete.Click += (_, _) => DeleteRequested?.Invoke(this);

            menu.Items.AddRange(new ToolStripItem[] { open, favorite, duplicate, delete });
            ContextMenuStrip = menu;
            return menu;
        }

        /// <summary>
        /// Forwards mouse interaction from the child controls that cover the
        /// card surface so selection, double-click open and the context menu
        /// work wherever the user clicks. The favorite affordance is excluded
        /// so it keeps toggling without selecting or opening the entry.
        /// </summary>
        private void WireMouseEvents(Control control)
        {
            if (control == _favorite || control == _delete || control == _duplicate ||
                control == _originLink)
            {
                return;
            }

            control.MouseDown += OnChildMouseDown;
            control.MouseDoubleClick += OnChildMouseDoubleClick;
            control.ContextMenuStrip = _contextMenu;

            foreach (Control child in control.Controls)
            {
                WireMouseEvents(child);
            }
        }

        private void OnChildMouseDown(object? sender, MouseEventArgs e) =>
            HandleGesture(e.Button, e.Clicks);

        private void OnChildMouseDoubleClick(object? sender, MouseEventArgs e) =>
            HandleDoubleClick(e.Button);

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

            _favorite.Margin = Padding.Empty;
            _delete.Margin = Padding.Empty;
            _duplicate.Margin = Padding.Empty;

            var actions = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
                Margin = new Padding(6, 0, 0, 0),
            };
            actions.Controls.Add(_favorite);
            actions.Controls.Add(_duplicate);
            actions.Controls.Add(_delete);

            outer.Controls.Add(content, 0, 0);
            outer.Controls.Add(actions, 1, 0);
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

        /// <summary>
        /// The origin is a shortcut to the source of the knowledge: clicking it
        /// opens the folder or the link, without selecting or opening the
        /// entry itself.
        /// </summary>
        private Control CreateOriginLink()
        {
            var kind = Entry.ProjectOriginKind!.Value;
            var location = Entry.ProjectOriginLocation!.Trim();

            var link = CreateLabel(
                ProjectOriginRules.DescribeLocation(kind, location),
                AppTheme.Fonts.Small,
                AppTheme.Accent);
            link.Cursor = Cursors.Hand;
            link.MinimumSize = new Size(0, 14);
            link.AccessibleName = AppLocalization.Translate("Open origin");
            link.Click += (_, _) => OriginOpenRequested?.Invoke(this);
            link.MouseEnter += (_, _) => link.ForeColor = AppTheme.AccentAlt;
            link.MouseLeave += (_, _) => link.ForeColor = AppTheme.Accent;

            _originLink = link;
            return link;
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