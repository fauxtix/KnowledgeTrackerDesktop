using System.ComponentModel;
using System.Drawing.Drawing2D;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Theme;
using KnowledgeTrackerDesktop.UI.Views;
using Syncfusion.WinForms.Controls;

namespace KnowledgeTrackerDesktop.UI.Shell
{
    /// <summary>
    /// Application shell (UI_DESIGN_SPEC section 3). Hosts the current view in a
    /// content area together with the navigation rail and page header. The shell
    /// owns no database or business logic; view lifecycle is managed by the
    /// navigation service.
    /// </summary>
    public class frmMain : SfForm, INavigationHost, IShellHost
    {
        private readonly Panel _navigationPanel;
        private readonly FlowLayoutPanel _navItems;
        private readonly Panel _headerPanel;
        private readonly FlowLayoutPanel _actionsHost;
        private readonly TableLayoutPanel _headerOptionsHost;
        private readonly TableLayoutPanel _languageOptions;
        private readonly Button _englishLanguageButton;
        private readonly Button _portugueseLanguageButton;
        private readonly Image _englishFlag;
        private readonly Image _portugueseFlag;
        private readonly Label _pageTitle;
        private readonly PageTitlePanel _titlePanel;
        private readonly Panel _contentHost;
        private readonly Label _placeholder;

        public frmMain()
        {
            Text = "KnowledgeTracker";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1024, 700);
            Size = new Size(1280, 820);
            AutoScaleMode = AutoScaleMode.Font;

            _contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                BackColor = AppTheme.Background,
            };

            _placeholder = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Your knowledge library will appear here.",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = AppTheme.SecondaryText,
            };
            AppLocalization.Bind(_placeholder, "Your knowledge library will appear here.");
            _contentHost.Controls.Add(_placeholder);

            _pageTitle = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "KnowledgeTracker",
                ForeColor = AppTheme.PrimaryText,
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            };
            AppLocalization.Bind(_pageTitle, "KnowledgeTracker");

            _titlePanel = new PageTitlePanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 0, 8, 0),
            };
            _titlePanel.Controls.Add(_pageTitle);

            _actionsHost = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                WrapContents = false,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 4, 0, 4),
            };

            _englishFlag = LoadEmbeddedImage("KnowledgeTrackerDesktop.Assets.Images.Inglaterra.png");
            _portugueseFlag = LoadEmbeddedImage("KnowledgeTrackerDesktop.Assets.Images.portugal.png");
            _englishLanguageButton = BuildLanguageButton(
                AppLanguage.English,
                "EN",
                "English (United States)",
                _englishFlag);
            _portugueseLanguageButton = BuildLanguageButton(
                AppLanguage.Portuguese,
                "PT",
                "Português (Portugal)",
                _portugueseFlag);
            _englishLanguageButton.Click += OnLanguageOptionClick;
            _portugueseLanguageButton.Click += OnLanguageOptionClick;

            _languageOptions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };
            _languageOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _languageOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            _languageOptions.Controls.Add(_englishLanguageButton, 0, 0);
            _languageOptions.Controls.Add(_portugueseLanguageButton, 1, 0);
            ApplyLanguageOptionStyles();

            _headerOptionsHost = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
                Margin = Padding.Empty,
            };
            _headerOptionsHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _headerOptionsHost.Controls.Add(_actionsHost, 0, 0);

            _headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Background,
                Padding = new Padding(16, 0, 16, 0),
            };
            _headerPanel.Controls.Add(BuildHeaderLayout());

            var brand = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "KnowledgeTracker",
                Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            };

            _navItems = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
            };

            _navigationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                Padding = new Padding(16),
            };

            _navigationPanel.Controls.Add(BuildNavigationLayout(brand));

            Controls.Add(BuildRootLayout());
            CarbonTheme.Apply(this);
            AppLocalization.LanguageChanged += OnLanguageChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppLocalization.LanguageChanged -= OnLanguageChanged;
            }

            base.Dispose(disposing);
            if (disposing)
            {
                _englishFlag.Dispose();
                _portugueseFlag.Dispose();
            }
        }

        /// <summary>
        /// Set by the composition root after construction, because the navigation
        /// service itself depends on this form as its <see cref="INavigationHost"/>.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public INavigationService? Navigation { get; set; }

        /// <summary>
        /// Displays <paramref name="view"/> in the content area. The previous view
        /// is detached but not disposed: the navigation service owns its lifecycle.
        /// </summary>
        public void DisplayView(Control view)
        {
            ArgumentNullException.ThrowIfNull(view);

            _contentHost.SuspendLayout();
            _contentHost.Controls.Clear();

            if (!_placeholder.IsDisposed)
            {
                _placeholder.Dispose();
            }

            view.Dock = DockStyle.Fill;
            _contentHost.Controls.Add(view);
            AppLocalization.Apply(view);
            _contentHost.ResumeLayout();

            CarbonTheme.Apply(view);

            if (view is IAppearingView appearing)
            {
                // Fire-and-forget on the UI thread: the view owns its loading and
                // error handling, so a failure there must not crash the shell.
                _ = appearing.OnAppearingAsync();
            }
        }

        /// <summary>Updates the page header title.</summary>
        public void SetPageTitle(string title)
        {
            AppLocalization.Bind(
                _pageTitle,
                string.IsNullOrWhiteSpace(title) ? "KnowledgeTracker" : title);
        }

        /// <summary>Replaces the header actions, disposing the previous ones.</summary>
        public void SetHeaderActions(params Control[] actions)
        {
            ArgumentNullException.ThrowIfNull(actions);

            ClearHeaderActions();
            _actionsHost.Controls.AddRange(actions);
            foreach (var action in actions)
            {
                CarbonTheme.Apply(action);
            }
        }

        public void ClearHeaderActions()
        {
            var previous = _actionsHost.Controls.Cast<Control>().ToArray();
            _actionsHost.Controls.Clear();
            foreach (var control in previous)
            {
                control.Dispose();
            }
        }

        private Control BuildRootLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 116f));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            body.Controls.Add(_headerPanel, 0, 0);
            body.Controls.Add(_contentHost, 0, 1);

            root.Controls.Add(_navigationPanel, 0, 0);
            root.Controls.Add(body, 1, 0);
            return root;
        }

        private Control BuildHeaderLayout()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));

            header.Controls.Add(_titlePanel, 0, 0);
            header.Controls.Add(_headerOptionsHost, 0, 1);
            return header;
        }

        private Control BuildNavigationLayout(Label brand)
        {
            var navigation = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
            };
            navigation.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            navigation.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            navigation.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));

            navigation.Controls.Add(brand, 0, 0);
            navigation.Controls.Add(_navItems, 0, 1);
            navigation.Controls.Add(_languageOptions, 0, 2);

            _navItems.Controls.Add(BuildNavButton("Library", () =>
                Navigation?.NavigateToPrimary<ctlMain>(new LibraryNavigationRequest())));
            _navItems.Controls.Add(BuildNavButton("Statistics", () =>
                Navigation?.NavigateToPrimary<ctlStatistics>()));

            return navigation;
        }

        private SfButton BuildNavButton(string text, Action onClick)
        {
            var button = new SfButton
            {
                AutoSize = false,
                Size = new Size(188, 40),
                Margin = new Padding(0, 0, 0, 4),
            };
            AppLocalization.Bind(button, text);
            button.Click += (_, _) => onClick();
            return button;
        }

        private static Button BuildLanguageButton(
            AppLanguage language,
            string abbreviation,
            string accessibleName,
            Image flag)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 4, 2, 4),
                Padding = new Padding(4, 0, 4, 0),
                Image = flag,
                ImageAlign = ContentAlignment.MiddleLeft,
                Text = abbreviation,
                TextAlign = ContentAlignment.MiddleRight,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                FlatStyle = FlatStyle.Flat,
                BackColor = CarbonPalette.Surface,
                ForeColor = CarbonPalette.TextPrimary,
                Font = AppTheme.Fonts.Input,
                AccessibleName = accessibleName,
                AccessibleDescription = $"Select {accessibleName}",
                Tag = language,
                UseVisualStyleBackColor = false,
            };
            button.FlatAppearance.BorderColor = CarbonPalette.Border;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = CarbonPalette.Border;
            return button;
        }

        private static Image LoadEmbeddedImage(string resourceName)
        {
            using var stream = typeof(frmMain).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded image resource '{resourceName}' was not found.");
            using var image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        private void OnLanguageOptionClick(object? sender, EventArgs e)
        {
            if (sender is not Button { Tag: AppLanguage language })
            {
                return;
            }

            try
            {
                AppLocalization.SetLanguage(language);
                ApplyLanguageOptionStyles();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                MessageBox.Show(
                    AppLocalization.Translate("The language preference could not be saved."),
                    "KnowledgeTracker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ApplyLanguageOptionStyles();
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            AppLocalization.Apply(this);
            ApplyLanguageOptionStyles();
        }

        private void ApplyLanguageOptionStyles()
        {
            var activeButton = AppLocalization.Language == AppLanguage.Portuguese
                ? _portugueseLanguageButton
                : _englishLanguageButton;
            foreach (var button in new[] { _englishLanguageButton, _portugueseLanguageButton })
            {
                button.BackColor = ReferenceEquals(button, activeButton)
                    ? CarbonPalette.Accent
                    : CarbonPalette.Surface;
                button.FlatAppearance.BorderColor = ReferenceEquals(button, activeButton)
                    ? CarbonPalette.AccentAlt
                    : CarbonPalette.Border;
                button.FlatAppearance.BorderSize = ReferenceEquals(button, activeButton) ? 2 : 1;
            }
        }

        private sealed class PageTitlePanel : Panel
        {
            public PageTitlePanel()
            {
                SetStyle(
                    ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer,
                    true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                {
                    return;
                }

                using var background = new LinearGradientBrush(
                    ClientRectangle,
                    AppTheme.Surface,
                    AppTheme.Background,
                    LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(background, ClientRectangle);

                using var accent = new LinearGradientBrush(
                    new Rectangle(0, 0, 4, ClientSize.Height),
                    AppTheme.Accent,
                    AppTheme.AccentAlt,
                    LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(accent, 0, 0, 4, ClientSize.Height);
            }
        }
    }
}
