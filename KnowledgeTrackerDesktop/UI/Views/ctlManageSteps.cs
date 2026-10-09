using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Navigation;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Focused editor for the ordered resolution steps of an entry
    /// (UI_DESIGN_SPEC section 6). Works on a local copy; <b>Apply</b> transfers
    /// the staged collection to the parent entry, which persists on its own save.
    /// </summary>
    public class ctlManageSteps : UserControl, IAppearingView
    {
        private readonly INavigationService _navigation;
        private readonly IShellHost _shell;
        private readonly KnowledgeEntry _entry;
        private readonly List<Step> _working = new();

        private readonly Label _countLabel = new();
        private readonly Panel _scrollHost = new();
        private readonly TableLayoutPanel _rows = new();
        private readonly Label _emptyLabel = new();

        public ctlManageSteps(
            INavigationService navigation,
            IShellHost shell,
            ManageStepsRequest request)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            ArgumentNullException.ThrowIfNull(request);
            _entry = request.Entry;

            foreach (var step in request.Entry.Steps.OrderBy(step => step.Order))
            {
                _working.Add(new Step
                {
                    Id = step.Id,
                    Description = step.Description,
                    Order = _working.Count,
                    KnowledgeEntryId = step.KnowledgeEntryId,
                });
            }

            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            Controls.Add(BuildRoot());
            RebuildRows();
        }

        public Task OnAppearingAsync()
        {
            var applyButton = new SfButton
            {
                AutoSize = false,
                Size = new Size(130, 32),
            };
            AppLocalization.Bind(applyButton, "Apply steps");
            applyButton.Click += (_, _) => ApplyAndReturn();

            var backButton = new SfButton
            {
                AutoSize = false,
                Size = new Size(90, 32),
            };
            AppLocalization.Bind(backButton, "Back");
            backButton.Click += (_, _) => _navigation.GoBack();

            _shell.SetPageTitle("Manage Steps");
            _shell.SetHeaderActions(applyButton, backButton);
            return Task.CompletedTask;
        }

        private Control BuildRoot()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0, 4, 0, 0),
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var caption = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = AppTheme.PrimaryText,
                Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                Margin = Padding.Empty,
            };
            AppLocalization.Bind(caption, "Resolution steps");

            _countLabel.AutoSize = true;
            _countLabel.Anchor = AnchorStyles.Right;
            _countLabel.TextAlign = ContentAlignment.MiddleRight;
            _countLabel.ForeColor = AppTheme.SecondaryText;

            header.Controls.Add(caption, 0, 0);
            header.Controls.Add(_countLabel, 1, 0);

            _scrollHost.Dock = DockStyle.Fill;
            _scrollHost.AutoScroll = true;
            _scrollHost.Padding = new Padding(0, 4, 0, 4);

            _rows.Dock = DockStyle.Top;
            _rows.AutoSize = true;
            _rows.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _rows.ColumnCount = 1;
            _rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _rows.Margin = Padding.Empty;

            _emptyLabel.AutoSize = true;
            _emptyLabel.MaximumSize = new Size(420, 0);
            AppLocalization.Bind(
                _emptyLabel,
                "No resolution steps yet.\r\n\r\nBreak the solution into clear, reproducible steps.");
            _emptyLabel.ForeColor = AppTheme.SecondaryText;
            _emptyLabel.Anchor = AnchorStyles.None;
            _emptyLabel.Visible = false;
            _emptyLabel.Margin = new Padding(0, 24, 0, 0);

            _scrollHost.Controls.Add(_rows);
            _scrollHost.Controls.Add(_emptyLabel);

            var addButton = new SfButton
            {
                AutoSize = false,
                Size = new Size(120, 32),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 0, 0),
            };
            AppLocalization.Bind(addButton, "+ Add step");
            addButton.Click += (_, _) => AddStep();

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(_scrollHost, 0, 1);
            root.Controls.Add(addButton, 0, 2);
            return root;
        }

        private void AddStep()
        {
            _working.Add(new Step
            {
                Description = string.Empty,
                Order = _working.Count,
                KnowledgeEntryId = _entry.Id,
            });
            RebuildRows();
        }

        private void RebuildRows()
        {
            _rows.SuspendLayout();
            _rows.Controls.Clear();
            _rows.RowStyles.Clear();
            _rows.RowCount = 0;

            for (var index = 0; index < _working.Count; index++)
            {
                _rows.Controls.Add(BuildRow(index));
                _rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            _rows.ResumeLayout();

            AppLocalization.Bind(_countLabel, () =>
                _working.Count == 1
                    ? AppLocalization.Translate("1 step")
                    : string.Format(
                        AppLocalization.Culture,
                        AppLocalization.Translate("{0} steps"),
                        _working.Count));
            _emptyLabel.Visible = _working.Count == 0;
            _rows.Visible = _working.Count > 0;
        }

        private Control BuildRow(int index)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(0, 2, 0, 2),
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34f));

            var order = new Label
            {
                Dock = DockStyle.Fill,
                Text = (index + 1).ToString("00"),
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = AppTheme.Accent,
                Font = new Font(Font.FontFamily, 11f, FontStyle.Bold),
                Margin = new Padding(0, 4, 0, 0),
            };

            var description = new TextBoxExt
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Height = 52,
                ScrollBars = ScrollBars.Vertical,
                Text = _working[index].Description ?? string.Empty,
                Margin = new Padding(0, 0, 6, 0),
            };
            description.TextChanged += (_, _) => _working[index].Description = description.Text;

            row.Controls.Add(order, 0, 0);
            row.Controls.Add(description, 1, 0);
            row.Controls.Add(BuildRowButton("\u2191", index, MoveUp, enabled: index > 0), 2, 0);
            row.Controls.Add(BuildRowButton("\u2193", index, MoveDown, enabled: index < _working.Count - 1), 3, 0);
            row.Controls.Add(BuildRowButton("\u2715", index, DeleteAt, enabled: true), 4, 0);
            return row;
        }

        private SfButton BuildRowButton(string text, int index, Action<int> action, bool enabled)
        {
            var button = new SfButton
            {
                Text = text,
                AutoSize = false,
                Size = new Size(30, 28),
                Anchor = AnchorStyles.Top,
                Enabled = enabled,
                Margin = new Padding(2, 2, 0, 0),
            };
            button.Click += (_, _) => action(index);
            return button;
        }

        private void MoveUp(int index)
        {
            if (index <= 0)
            {
                return;
            }

            (_working[index - 1], _working[index]) = (_working[index], _working[index - 1]);
            RebuildRows();
        }

        private void MoveDown(int index)
        {
            if (index >= _working.Count - 1)
            {
                return;
            }

            (_working[index + 1], _working[index]) = (_working[index], _working[index + 1]);
            RebuildRows();
        }

        private void DeleteAt(int index)
        {
            if (index < 0 || index >= _working.Count)
            {
                return;
            }

            _working.RemoveAt(index);
            RebuildRows();
        }

        private void ApplyAndReturn()
        {
            _entry.Steps.Clear();
            for (var index = 0; index < _working.Count; index++)
            {
                _working[index].Order = index;
                _working[index].KnowledgeEntryId = _entry.Id;
                _entry.Steps.Add(_working[index]);
            }

            // Navigating away disposes this view; defer so the click handler
            // has finished before the control tree is torn down.
            BeginInvoke(new System.Action(() => _navigation.GoBack()));
        }
    }
}
