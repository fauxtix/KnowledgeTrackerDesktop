using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Read-only preview of a code snippet. It never modifies the source
    /// (UI_DESIGN_SPEC section 5.6) and is opened from the Code section of the
    /// Add/Edit editor.
    /// </summary>
    public sealed class CodePreviewForm : SfForm
    {
        public CodePreviewForm(string text)
        {
            Text = AppLocalization.Translate("Code preview");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(950, 800);
            MinimumSize = new Size(400, 300);
            BackColor = AppTheme.Background;
            MinimizeBox = false;
            MaximizeBox = false;

            var preview = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,               
                Text = text,
                Font = new Font("Consolas", 12f),
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.PrimaryText,
                BorderStyle = BorderStyle.FixedSingle,
            };

            var close = new SfButton
            {
                AutoSize = false,
                Size = new Size(100, 32),
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
            };
            AppLocalization.Bind(close, "Close");
            ButtonStyler.Apply(close, ButtonRole.Secondary);

            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10),
                BackColor = AppTheme.Surface,
            };
            panel.Controls.Add(close);
            panel.Resize += (_, _) => close.Location = new Point(
                panel.ClientSize.Width - close.Width - 10,
                10);

            Controls.Add(preview);
            Controls.Add(panel);

            AcceptButton = close;
            CancelButton = close;

            CarbonTheme.Apply(this);
        }
    }
}