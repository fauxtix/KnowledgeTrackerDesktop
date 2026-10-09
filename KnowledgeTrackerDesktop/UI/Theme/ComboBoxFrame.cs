using System.Drawing.Drawing2D;
using Syncfusion.Windows.Forms.Tools;

namespace KnowledgeTrackerDesktop.UI.Theme
{
    internal sealed class ComboBoxFrame : Panel
    {
        private readonly ComboBoxAdv _comboBox;
        private readonly Label _displayText;
        private readonly Label _dropDownIndicator;
        private readonly ToolStripDropDown _dropDown = new();

        public ComboBoxFrame(ComboBoxAdv comboBox, int width)
        {
            _comboBox = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
            Width = width;
            Height = 48;
            BackColor = CarbonPalette.DeepBackground;
            Padding = new Padding(2);
            Margin = new Padding(0, 4, 8, 4);

            _comboBox.Dock = DockStyle.Fill;
            _comboBox.Margin = Padding.Empty;
            _comboBox.Visible = false;

            _displayText = new Label
            {
                AutoSize = false,
                Location = new Point(4, 3),
                Size = new Size(width - 44, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = CarbonPalette.DeepBackground,
                ForeColor = CarbonPalette.TextPrimary,
                Font = AppTheme.Fonts.Input,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand,
                TabStop = false,
                AccessibleName = "Selected option",
            };
            _displayText.Click += (_, _) => OpenDropDown();
            _comboBox.SelectedIndexChanged += (_, _) =>
            {
                RefreshDisplayText();
                RebuildDropDownItems();
            };

            _dropDownIndicator = new Label
            {
                AutoSize = false,
                Width = 34,
                Height = 42,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(width - 36, 3),
                BackColor = CarbonPalette.Surface,
                ForeColor = CarbonPalette.TextPrimary,
                Font = AppTheme.Fonts.CardTitle,
                Text = "\u25be",
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                TabStop = false,
            };
            _dropDownIndicator.Click += (_, _) => OpenDropDown();

            Controls.Add(_comboBox);
            Controls.Add(_displayText);
            Controls.Add(_dropDownIndicator);
            _dropDownIndicator.BringToFront();
            _displayText.BringToFront();
            RefreshDisplayText();
            RebuildDropDownItems();
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void ApplyCarbonStyle()
        {
            BackColor = CarbonPalette.DeepBackground;
            _displayText.BackColor = CarbonPalette.DeepBackground;
            _displayText.ForeColor = CarbonPalette.TextPrimary;
            _dropDownIndicator.BackColor = CarbonPalette.Surface;
            _dropDownIndicator.ForeColor = CarbonPalette.TextPrimary;
        }

        internal void ApplyLocalization()
        {
            RefreshDisplayText();
            RebuildDropDownItems();
        }

        private void RefreshDisplayText() =>
            _displayText.Text = _comboBox.SelectedItem?.ToString() ?? string.Empty;

        private void OpenDropDown()
        {
            RebuildDropDownItems();
            _dropDown.Show(this, 0, Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(CarbonPalette.Border, 1.5f);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        private void RebuildDropDownItems()
        {
            _dropDown.Items.Clear();
            for (var index = 0; index < _comboBox.Items.Count; index++)
            {
                var selectedIndex = index;
                var item = _comboBox.Items[index];
                var menuItem = new ToolStripMenuItem(item?.ToString() ?? string.Empty)
                {
                    Checked = selectedIndex == _comboBox.SelectedIndex,
                    CheckOnClick = false,
                    BackColor = CarbonPalette.Surface,
                    ForeColor = CarbonPalette.TextPrimary,
                    Font = AppTheme.Fonts.Input,
                };
                menuItem.Click += (_, _) => _comboBox.SelectedIndex = selectedIndex;
                _dropDown.Items.Add(menuItem);
            }

            _dropDown.BackColor = CarbonPalette.Surface;
            _dropDown.ForeColor = CarbonPalette.TextPrimary;
        }
    }
}
