using System.Diagnostics;
using System.Reflection;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.Controls.Styles;
using Syncfusion.WinForms.Input;
using Syncfusion.Windows.Forms.Tools;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Add/Edit entry editor (UI_DESIGN_SPEC section 5). Writes a technical
    /// article: the object is mutated while editing and committed on Save, which
    /// persists the whole aggregate through <see cref="IKnowledgeEntryService"/>.
    /// </summary>
    public class ctlAddEdit : UserControl, IAppearingView
    {
        private readonly IKnowledgeEntryService _service;
        private readonly INavigationService _navigation;
        private readonly IShellHost _shell;
        private readonly KnowledgeEntry _entry;
        private readonly bool _isNew;
        private readonly List<Step> _workingSteps = new();

        private readonly TextBoxExt _title = new();
        private readonly TextBoxExt _project = new();
        private readonly TextBoxExt _technologies = new();
        private readonly SfDateTimeEdit _date = new();
        private readonly TextBoxExt _tags = new();
        private readonly CheckBoxAdv _favorite = new();
        private readonly TextBoxExt _description = new();
        private readonly Label _stepsCount = new();
        private readonly Label _stepsEmpty = new();
        private readonly TableLayoutPanel _stepRows = new();
        private readonly TextBoxExt _code = new();
        private readonly TextBoxExt _youtube = new();
        private readonly SfButton _openVideo = new();
        private readonly TableLayoutPanel _attachments = new();
        private readonly SfButton _openGallery = new();
        private readonly TextBoxExt _comments = new();
        private readonly Label _errorLabel = new();
        private readonly TableLayoutPanel _generalForm = new();
        private readonly TableLayoutPanel _stepsForm = new();
        private readonly TableLayoutPanel _codeForm = new();
        private readonly TableLayoutPanel _attachmentsForm = new();
        private readonly List<SfScrollFrame> _scrollFrames = new();

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal Func<string[]?>? FilePicker { get; set; }

        /// <summary>
        /// Horizontal padding kept between a tab caption and the header
        /// borders. The strip sizes each header from its own caption
        /// (<c>SizeMode.Normal</c>), so this inset must stay small enough for
        /// the longest translated label to be drawn in full.
        /// </summary>
        private const int TabTextInset = 8;

        private TabControlAdv? _editorTabs;
        private SfButton? _saveButton;
        private bool _dirty;

        public ctlAddEdit(
            IKnowledgeEntryService service,
            INavigationService navigation,
            IShellHost shell,
            EntryEditorRequest request)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            ArgumentNullException.ThrowIfNull(request);

            _entry = request.Entry;
            _isNew = request.IsNew;
            foreach (var step in _entry.Steps.OrderBy(step => step.Order))
            {
                _workingSteps.Add(new Step
                {
                    Id = step.Id,
                    Description = step.Description,
                    Order = _workingSteps.Count,
                    KnowledgeEntryId = step.KnowledgeEntryId,
                });
            }

            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;
            DoubleBuffered = true;
            AppLocalization.LanguageChanged += OnLanguageChanged;
            Controls.Add(BuildRoot());
            LoadEntryIntoControls();
        }

        private void OnLanguageChanged(object? sender, EventArgs e) => AppLocalization.Apply(this);

        public Task OnAppearingAsync()
        {
            if (_isNew)
            {
                _title.Focus();
            }

            SetHeader();
            return Task.CompletedTask;
        }

        private void SetHeader()
        {
            _saveButton = new SfButton
            {
                Text = "Save",
                AutoSize = false,
                Size = new Size(100, 32),
            };
            _saveButton.Click += async (_, _) => await SaveAsync();

            var cancel = new SfButton
            {
                Text = "Cancel",
                AutoSize = false,
                Size = new Size(100, 32),
            };
            cancel.Click += (_, _) => Cancel();

            var back = new SfButton
            {
                AutoSize = false,
                Size = new Size(90, 32),
            };
            AppLocalization.Bind(_saveButton, "Save");
            AppLocalization.Bind(cancel, "Cancel");
            AppLocalization.Bind(back, "Back");
            back.Click += (_, _) => Cancel();
            ButtonStyler.Apply(_saveButton, ButtonRole.Primary);
            ButtonStyler.Apply(cancel, ButtonRole.Secondary);
            ButtonStyler.Apply(back, ButtonRole.Ghost);

            _shell.SetPageTitle(_isNew ? "New Entry" : "Edit Entry");
            _shell.SetHeaderActions(_saveButton, cancel, back);
        }

        private Control BuildRoot()
        {
            ConfigureForm(_generalForm);
            ConfigureForm(_stepsForm);
            ConfigureForm(_attachmentsForm);

            _errorLabel.AutoSize = true;
            _errorLabel.ForeColor = AppTheme.Error;
            _errorLabel.Visible = false;
            _errorLabel.Margin = new Padding(0, 0, 0, 8);

            _title.Dock = DockStyle.Fill;
            _title.Multiline = false;
            _title.Font = new Font(Font.FontFamily, 14f);
            _title.TextChanged += (_, _) => MarkDirty();

            BuildMetadata();

            // Tab 1: General — every piece of classification metadata (title,
            // project, technologies, date, tags, favorite) plus description
            // and comments lives on the General tab.
            AddSectionRow(_generalForm, CreateSection("Main information", BuildMainInformationRow()));
            AddSectionRow(_generalForm, CreateSection("Classification", BuildClassificationRow()));
            AddSectionRow(_generalForm, CreateSection("Description (Markdown)", BuildDescriptionSection()));
            AddSectionRow(_generalForm, CreateSection("Comments", BuildCommentsSection()));

            // Tab 2: Resolutions — structured steps.
            AddSectionRow(_stepsForm, CreateSection("Resolution steps", BuildStepsSection()));

            // Tab 3: Code — dedicated code editor that expands to fill the
            // available space, with copy and preview actions.
            BuildCodeTab();

            // Tab 4: Attachments — attachments list, gallery, and YouTube video.
            AddSectionRow(_attachmentsForm, CreateSection("Attachments and references", BuildAttachmentsSection()));

            var tabs = _editorTabs = new TabControlAdv
            {
                Dock = DockStyle.Fill,
                TabStyle = typeof(TabRenderer2D),
                // All four tabs share a fixed, uniform width (140x36) ensuring
                // balanced presentation and full label visibility without clipping.
                SizeMode = Syncfusion.Windows.Forms.Tools.TabSizeMode.Fixed,
                ItemSize = new Size(140, 36),
                BackColor = AppTheme.Background,
                TabPanelBackColor = AppTheme.Background,
                BorderVisible = true,
                BorderColor = AppTheme.CardBorder,
                SeparatorColor = AppTheme.CardBorder,
                Font = AppTheme.Fonts.Caption,
                ActiveTabFont = AppTheme.Fonts.SectionTitle,
                HotTrack = true,
            };
            EnableDoubleBuffering(tabs);
            tabs.DrawItem += (_, e) => DrawEditorTab(tabs, e);

            tabs.Controls.Add(CreateTab("General", _generalForm));
            tabs.Controls.Add(CreateTab("Resolutions", _stepsForm));
            tabs.Controls.Add(CreateTab("Code", _codeForm));
            tabs.Controls.Add(CreateTab("Attachments", _attachmentsForm));
            tabs.SelectedIndex = 0;

            // The validation message sits above the tab strip so it stays
            // visible no matter which tab is active.
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.Controls.Add(_errorLabel, 0, 0);
            root.Controls.Add(tabs, 0, 1);
            return root;
        }

        private TabPageAdv CreateTab(string titleKey, Control form)
        {
            var page = new TabPageAdv
            {
                BackColor = AppTheme.Background,
            };
            AppLocalization.Bind(page, titleKey);
            page.Controls.Add(CreateScrollableForm(form));
            return page;
        }

        /// <summary>
        /// Applies the Carbon look to the editor tab strip after the skin has
        /// been set (mirrors the behaviour the editor had before the single
        /// page redesign). Safe to call repeatedly.
        /// </summary>
        internal void ApplyEditorTabTheme()
        {
            if (_editorTabs is null)
            {
                return;
            }

            _editorTabs.BackColor = AppTheme.Background;
            _editorTabs.TabPanelBackColor = AppTheme.Background;
            _editorTabs.ActiveTabColor = AppTheme.Surface;
            _editorTabs.InactiveTabColor = AppTheme.Background;
            _editorTabs.ActiveTabForeColor = AppTheme.PrimaryText;
            _editorTabs.InActiveTabForeColor = AppTheme.SecondaryText;
            _editorTabs.BorderVisible = true;
            _editorTabs.BorderColor = AppTheme.CardBorder;
            _editorTabs.SeparatorColor = AppTheme.CardBorder;

            foreach (TabPageAdv page in _editorTabs.TabPages)
            {
                page.BackColor = AppTheme.Background;
            }

            _editorTabs.Invalidate();
        }

        private static void DrawEditorTab(TabControlAdv tabs, DrawTabEventArgs e)
        {
            var selected = e.Index == tabs.SelectedIndex;
            var bounds = e.Bounds;
            var background = selected
                ? AppTheme.Surface
                : AppTheme.Background;

            using var backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, bounds);

            var borderBounds = Rectangle.Inflate(bounds, -1, -1);
            using (var borderPen = new Pen(AppTheme.CardBorder))
            {
                e.Graphics.DrawRectangle(borderPen, borderBounds);
            }

            var textColor = selected ? AppTheme.PrimaryText : AppTheme.SecondaryText;
            var text = tabs.TabPages[e.Index].Text;
            var textBounds = Rectangle.Inflate(bounds, -TabTextInset, -2);
            var flags = TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.NoPrefix;

            // The header is sized from the caption font, so the emphasis font
            // is only used while the complete label still fits inside the
            // header: a lighter caption always beats a shortened one.
            var font = selected ? AppTheme.Fonts.SectionTitle : AppTheme.Fonts.Caption;
            if (!FitsInHeader(e.Graphics, text, font, textBounds, flags))
            {
                font = AppTheme.Fonts.Caption;
            }

            if (!FitsInHeader(e.Graphics, text, font, textBounds, flags))
            {
                // Defensive only: reachable if a future layout change shrinks
                // the headers below the width of their own caption.
                flags |= TextFormatFlags.EndEllipsis;
            }

            TextRenderer.DrawText(e.Graphics, text, font, textBounds, textColor, flags);

            if (selected)
            {
                var indicator = new Rectangle(bounds.X + 1, bounds.Bottom - 3, bounds.Width - 2, 3);
                using var accentBrush = new SolidBrush(AppTheme.Accent);
                e.Graphics.FillRectangle(accentBrush, indicator);
            }
        }

        /// <summary>
        /// Measures <paramref name="text"/> exactly as <see cref="DrawEditorTab"/>
        /// renders it, so the caption can be checked against the header
        /// bounds before drawing.
        /// </summary>
        private static bool FitsInHeader(Graphics graphics, string text, Font font, Rectangle bounds, TextFormatFlags flags)
        {
            var required = TextRenderer.MeasureText(graphics, text, font, Size.Empty, flags);
            return required.Width <= bounds.Width && required.Height <= bounds.Height;
        }

        /// <summary>
        /// Builds one visual section: a surface-coloured card with a header and
        /// the supplied rows. Cards are reused inside each editor tab so the
        /// Carbon look stays consistent across the tabbed layout.
        /// </summary>
        private Control CreateSection(string titleKey, params Control[] rows)
        {
            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = AppTheme.Surface,
                Padding = new Padding(16, 12, 16, 12),
                Margin = Padding.Empty,
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            EnableDoubleBuffering(card);

            var header = new Label
            {
                AutoSize = true,
                ForeColor = AppTheme.PrimaryText,
                Font = AppTheme.Fonts.SectionTitle,
                Margin = new Padding(0, 0, 0, 10),
            };
            AppLocalization.Bind(header, titleKey);
            card.Controls.Add(header, 0, 0);
            card.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            foreach (var row in rows)
            {
                AddFormRow(card, row);
                row.Margin = new Padding(0, 0, 0, 8);
            }

            card.Paint += (_, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, card.ClientSize.Width - 1, card.ClientSize.Height - 1);
            };

            return card;
        }

        private static void ConfigureForm(TableLayoutPanel form)
        {
            form.Dock = DockStyle.Top;
            form.AutoSize = true;
            form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            form.ColumnCount = 1;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            form.Margin = Padding.Empty;
        }

        private Control CreateScrollableForm(Control form)
        {
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(8),
            };
            EnableDoubleBuffering(body);
            body.Controls.Add(form);

            ApplyThemedScrollFrame(body);
            return body;
        }

        private static void EnableDoubleBuffering(Control control) =>
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true);

        private void ApplyThemedScrollFrame(Control control)
        {
            var scrollFrame = new SfScrollFrame
            {
            };
            var style = new ScrollerVisualStyle(control);
            ApplyScrollBarTheme(style.VerticalScrollBar);
            ApplyScrollBarTheme(style.HorizontalScrollBar);
            scrollFrame.Style = style;
            scrollFrame.Control = control;
            _scrollFrames.Add(scrollFrame);
        }

        private static void ApplyScrollBarTheme(ScrollBarStyleInfo style)
        {
            style.ScrollBarBackColor = AppTheme.Background;
            style.ThumbColor = AppTheme.CardBorder;
            style.ThumbHoverColor = AppTheme.AccentAlt;
            style.ThumbPressedColor = AppTheme.Accent;
            style.ArrowButtonBackColor = AppTheme.Surface;
            style.ArrowButtonHoverBackColor = AppTheme.CardBorder;
            style.ArrowButtonPressedBackColor = AppTheme.Accent;
            style.ArrowButtonForeColor = AppTheme.SecondaryText;
            style.ArrowButtonHoverForeColor = AppTheme.PrimaryText;
            style.ArrowButtonPressedForeColor = AppTheme.PrimaryText;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppLocalization.LanguageChanged -= OnLanguageChanged;

                foreach (var scrollFrame in _scrollFrames)
                {
                    scrollFrame.Dispose();
                }

                _scrollFrames.Clear();
            }

            base.Dispose(disposing);
        }

        private void BuildMetadata()
        {
            _project.Dock = DockStyle.Fill;
            _project.Multiline = false;
            _project.TextChanged += (_, _) => MarkDirty();

            _technologies.Dock = DockStyle.Fill;
            _technologies.Multiline = false;
            _technologies.TextChanged += (_, _) => MarkDirty();

            _date.AllowNull = true;
            _date.Watermark = "Not set";
            _date.AutoSize = false;
            _date.MinimumSize = new Size(0, 27);
            _date.MaximumSize = new Size(0, 27);
            _date.ValueChanged += (_, _) => MarkDirty();

            _tags.Dock = DockStyle.Fill;
            _tags.Multiline = false;
            _tags.TextChanged += (_, _) => MarkDirty();

            _favorite.AutoSize = true;
            AppLocalization.Bind(_favorite, "Favorite");
            _favorite.Anchor = AnchorStyles.Left;
            _favorite.Margin = new Padding(8, 6, 0, 0);
            _favorite.CheckedChanged += (_, _) => MarkDirty();
        }

        private Control BuildMainInformationRow()
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                // The editor is a single scrolling page, so the metadata inputs
                // no longer inherit a font from the old tab pages. Set it here
                // so their rendered height stays consistent (and matches the
                // fixed-height date editor).
                Font = AppTheme.Fonts.Caption,
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

            row.Controls.Add(Field("Title", CreateMetadataInput(_title), rightPad: 8), 0, 0);
            row.Controls.Add(Field("Project", CreateMetadataInput(_project)), 1, 0);
            return row;
        }

        private Control BuildClassificationRow()
        {
            var meta = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 4,
                RowCount = 1,
                Margin = Padding.Empty,
                // See BuildMainInformationRow: keep the metadata inputs on the
                // same font as before the tabs were removed.
                Font = AppTheme.Fonts.Caption,
            };
            meta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            meta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            meta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            meta.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11f));

            meta.Controls.Add(Field("Technologies", CreateMetadataInput(_technologies), rightPad: 8), 0, 0);
            meta.Controls.Add(Field("Date resolved", CreateMetadataInput(_date), rightPad: 8), 1, 0);
            meta.Controls.Add(Field("Tags", CreateMetadataInput(_tags), rightPad: 8), 2, 0);
            meta.Controls.Add(BuildFavoriteField(), 3, 0);
            return meta;
        }

        private static Control CreateMetadataInput(Control input)
        {
            var wrapper = ModernLayoutHelper.WrapInRoundedPanel(input, height: 30);
            wrapper.Margin = Padding.Empty;
            return wrapper;
        }

        private Control BuildFavoriteField()
        {
            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
            };
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var caption = new Label
            {
                AutoSize = true,
                Font = AppTheme.Fonts.SectionTitle,
                Margin = new Padding(0, 0, 0, 2),
            };

            var host = new FlowLayoutPanel
            {
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0),
                Margin = Padding.Empty,
            };
            host.Controls.Add(_favorite);

            container.Controls.Add(caption, 0, 0);
            container.Controls.Add(host, 0, 1);
            return container;
        }

        private Control BuildDescriptionSection()
        {
            _description.Dock = DockStyle.Fill;
            _description.Multiline = true;
            _description.ScrollBars = ScrollBars.Vertical;
            _description.Height = 80;
            _description.Font = new Font("Consolas", 11f);
            _description.TextChanged += (_, _) => MarkDirty();
            ApplyThemedScrollFrame(_description);

            var hint = new Label
            {
                AutoSize = true,
                ForeColor = AppTheme.SecondaryText,
                Font = AppTheme.Fonts.Caption,
                Margin = new Padding(0, 4, 0, 0),
            };
            AppLocalization.Bind(hint, "Markdown is supported here.");

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.Controls.Add(_description, 0, 0);
            stack.Controls.Add(hint, 0, 1);
            return stack;
        }

        private Control BuildStepsSection()
        {
            _stepsCount.AutoSize = true;
            _stepsCount.ForeColor = AppTheme.SecondaryText;

            _stepsEmpty.AutoSize = true;
            _stepsEmpty.MaximumSize = new Size(520, 0);
            AppLocalization.Bind(
                _stepsEmpty,
                "No resolution steps yet. Add steps to document how the issue was resolved.");
            _stepsEmpty.ForeColor = AppTheme.SecondaryText;
            _stepsEmpty.Margin = new Padding(0, 8, 0, 8);

            _stepRows.Dock = DockStyle.Top;
            _stepRows.AutoSize = true;
            _stepRows.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _stepRows.RowCount = 0;
            _stepRows.ColumnCount = 1;
            _stepRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _stepRows.Margin = Padding.Empty;

            var add = new SfButton
            {
                Text = "+ Add step",
                AutoSize = false,
                Size = new Size(120, 32),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 8, 0, 0),
                Tag = "+ Add step",
            };
            AppLocalization.Bind(add, "+ Add step");
            add.Click += (_, _) => AddStep();
            ButtonStyler.Apply(add, ButtonRole.Ghost);

            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 1,
                Margin = Padding.Empty,
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row.Controls.Add(_stepsCount, 0, 0);

            var section = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
            };
            for (var index = 0; index < section.RowCount; index++)
            {
                section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            section.Controls.Add(row, 0, 0);
            section.Controls.Add(_stepsEmpty, 0, 1);
            section.Controls.Add(_stepRows, 0, 2);
            section.Controls.Add(add, 0, 3);

            RebuildStepRows();
            return section;
        }

        internal void AddStep()
        {
            _workingSteps.Add(new Step
            {
                Description = string.Empty,
                Order = _workingSteps.Count,
                KnowledgeEntryId = _entry.Id,
            });
            MarkDirty();
            RebuildStepRows();
        }

        private void RebuildStepRows()
        {
            _stepRows.SuspendLayout();
            foreach (Control control in _stepRows.Controls.Cast<Control>().ToArray())
            {
                _stepRows.Controls.Remove(control);
                control.Dispose();
            }

            _stepRows.RowStyles.Clear();
            _stepRows.RowCount = _workingSteps.Count;
            for (var index = 0; index < _workingSteps.Count; index++)
            {
                _stepRows.Controls.Add(BuildStepRow(index), 0, index);
                _stepRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));
            }

            AppLocalization.Bind(_stepsCount, () =>
            {
                var template = _workingSteps.Count == 1 ? "1 step" : "{0} steps";
                return _workingSteps.Count == 1
                    ? AppLocalization.Translate(template)
                    : string.Format(
                        AppLocalization.Culture,
                        AppLocalization.Translate(template),
                        _workingSteps.Count);
            });
            _stepsEmpty.Visible = _workingSteps.Count == 0;
            _stepRows.ResumeLayout(true);
            _stepRows.PerformLayout();
            _stepRows.Parent?.PerformLayout();
        }

        private Control BuildStepRow(int index)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                Height = 62,
                MinimumSize = new Size(0, 62),
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
                Text = _workingSteps[index].Description ?? string.Empty,
                Margin = new Padding(0, 0, 6, 0),
            };
            description.TextChanged += (_, _) =>
            {
                _workingSteps[index].Description = description.Text;
                MarkDirty();
            };

            row.Controls.Add(order, 0, 0);
            row.Controls.Add(description, 1, 0);
            row.Controls.Add(BuildStepButton("\u2191", index, MoveStepUp, index > 0, ButtonRole.Icon), 2, 0);
            row.Controls.Add(BuildStepButton("\u2193", index, MoveStepDown, index < _workingSteps.Count - 1, ButtonRole.Icon), 3, 0);
            row.Controls.Add(BuildStepButton("\u2715", index, RemoveStep, true, ButtonRole.IconDanger), 4, 0);
            CarbonTheme.Apply(row);
            return row;
        }

        private SfButton BuildStepButton(string text, int index, Action<int> action, bool enabled, ButtonRole role)
        {
            var button = new SfButton
            {
                Text = text,
                AutoSize = false,
                Size = new Size(32, 32),
                Anchor = AnchorStyles.Top,
                Enabled = enabled,
                Margin = new Padding(2, 2, 0, 0),
            };
            button.Click += (_, _) => action(index);
            ButtonStyler.Apply(button, role);
            return button;
        }

        private void MoveStepUp(int index)
        {
            if (index <= 0 || index >= _workingSteps.Count)
            {
                return;
            }

            (_workingSteps[index - 1], _workingSteps[index]) =
                (_workingSteps[index], _workingSteps[index - 1]);
            MarkDirty();
            RebuildStepRows();
        }

        private void MoveStepDown(int index)
        {
            if (index < 0 || index >= _workingSteps.Count - 1)
            {
                return;
            }

            (_workingSteps[index + 1], _workingSteps[index]) =
                (_workingSteps[index], _workingSteps[index + 1]);
            MarkDirty();
            RebuildStepRows();
        }

        private void RemoveStep(int index)
        {
            if (index < 0 || index >= _workingSteps.Count)
            {
                return;
            }

            _workingSteps.RemoveAt(index);
            MarkDirty();
            RebuildStepRows();
        }

        private void BuildCodeTab()
        {
            _codeForm.Dock = DockStyle.Fill;
            _codeForm.ColumnCount = 2;
            _codeForm.RowCount = 2;
            _codeForm.ColumnStyles.Clear();
            _codeForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _codeForm.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _codeForm.RowStyles.Clear();
            _codeForm.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _codeForm.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _codeForm.BackColor = AppTheme.Surface;
            _codeForm.Padding = new Padding(16, 12, 16, 12);
            _codeForm.Margin = Padding.Empty;
            EnableDoubleBuffering(_codeForm);
            _codeForm.Paint += (_, e) =>
            {
                using var pen = new Pen(AppTheme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, _codeForm.ClientSize.Width - 1, _codeForm.ClientSize.Height - 1);
            };

            var header = new Label
            {
                AutoSize = true,
                ForeColor = AppTheme.PrimaryText,
                Font = AppTheme.Fonts.SectionTitle,
                Margin = new Padding(0, 0, 0, 10),
            };
            AppLocalization.Bind(header, "Code");
            _codeForm.Controls.Add(header, 0, 0);

            _code.Dock = DockStyle.Fill;
            _code.Multiline = true;
            _code.ScrollBars = ScrollBars.Both;
            _code.WordWrap = false;
            _code.AcceptsTab = true;
            _code.Font = new Font("Consolas", 11f);
            _code.TextChanged += (_, _) => MarkDirty();
            ApplyThemedScrollFrame(_code);

            var copy = new SfButton
            {
                AutoSize = false,
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Top,
                Margin = new Padding(0, 0, 8, 10),
                Enabled = !string.IsNullOrEmpty(_code.Text),
            };
            AppLocalization.Bind(copy, "Copy");
            _code.TextChanged += (_, _) => copy.Enabled = !string.IsNullOrEmpty(_code.Text);
            copy.Click += (_, _) => CopyCode();

            var preview = new SfButton
            {
                AutoSize = false,
                Size = new Size(130, 30),
                Anchor = AnchorStyles.Top,
                Margin = new Padding(0, 0, 0, 10),
                Enabled = !string.IsNullOrEmpty(_code.Text),
            };
            AppLocalization.Bind(preview, "Preview");
            _code.TextChanged += (_, _) => preview.Enabled = !string.IsNullOrEmpty(_code.Text);
            preview.Click += (_, _) => PreviewCode();

            ButtonStyler.Apply(copy, ButtonRole.Secondary);
            ButtonStyler.Apply(preview, ButtonRole.Secondary);

            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Right,
                Margin = Padding.Empty,
            };
            actions.Controls.Add(copy);
            actions.Controls.Add(preview);

            _codeForm.Controls.Add(actions, 1, 0);
            _codeForm.Controls.Add(_code, 0, 1);
            _codeForm.SetColumnSpan(_code, 2);
        }

        private Control BuildYouTubeSection()
        {
            _youtube.Dock = DockStyle.Fill;
            _youtube.Multiline = false;
            _youtube.TextChanged += (_, _) =>
            {
                _openVideo.Enabled = IsHttpUrl(_youtube.Text);
                MarkDirty();
            };

            AppLocalization.Bind(_openVideo, "Open video");
            _openVideo.AutoSize = false;
            _openVideo.Size = new Size(110, 28);
            _openVideo.Anchor = AnchorStyles.Left;
            _openVideo.Enabled = false;
            _openVideo.Margin = new Padding(8, 0, 0, 0);
            _openVideo.Click += (_, _) => OpenUrl(_youtube.Text);
            ButtonStyler.Apply(_openVideo, ButtonRole.Secondary);

            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Controls.Add(_youtube, 0, 0);
            row.Controls.Add(_openVideo, 1, 0);

            return Field("YouTube", row);
        }

        private Control BuildAttachmentsSection()
        {
            _attachments.Dock = DockStyle.Top;
            _attachments.AutoSize = true;
            _attachments.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _attachments.ColumnCount = 1;
            _attachments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _attachments.Margin = Padding.Empty;

            var add = new SfButton
            {
                AutoSize = true,
                MinimumSize = new Size(110, 28),
                Padding = new Padding(8, 0, 8, 0),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 4, 0, 0),
                Tag = "+ Add file",
            };
            AppLocalization.Bind(add, "+ Add file");
            add.Click += (_, _) => AddAttachment();
            ButtonStyler.Apply(add, ButtonRole.Ghost);

            _openGallery.AutoSize = true;
            _openGallery.MinimumSize = new Size(120, 28);
            _openGallery.Padding = new Padding(8, 0, 8, 0);
            _openGallery.Anchor = AnchorStyles.Left;
            _openGallery.Margin = new Padding(8, 4, 0, 0);
            _openGallery.Enabled = false;
            AppLocalization.Bind(_openGallery, "Open gallery");
            _openGallery.Click += (_, _) => OpenGallery();
            ButtonStyler.Apply(_openGallery, ButtonRole.Secondary);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty,
            };
            actions.Controls.Add(add);
            actions.Controls.Add(_openGallery);

            var section = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
            };
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.Controls.Add(_attachments, 0, 0);
            section.Controls.Add(actions, 0, 1);
            section.Controls.Add(BuildYouTubeSection(), 0, 2);

            return section;
        }

        private Control BuildCommentsSection()
        {
            _comments.Dock = DockStyle.Fill;
            _comments.Multiline = true;
            _comments.ScrollBars = ScrollBars.Vertical;
            _comments.Height = 56;
            _comments.TextChanged += (_, _) => MarkDirty();
            ApplyThemedScrollFrame(_comments);

            return _comments;
        }

        private static void AddFormRow(TableLayoutPanel form, Control control)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 0, 0, 12);
            form.Controls.Add(control);
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        /// <summary>
        /// Adds a section card as a top-docked, auto-sized row. Sections must not
        /// be dock-filled, otherwise the surrounding auto-size layout would
        /// stretch their inputs instead of respecting their preferred height.
        /// </summary>
        private static void AddSectionRow(TableLayoutPanel form, Control section)
        {
            section.Dock = DockStyle.Top;
            section.Margin = new Padding(0, 0, 0, 12);
            form.Controls.Add(section);
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        private Control Field(string label, Control input, int rightPad = 0)
        {
            var container = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, rightPad, 0),
            };
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            container.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var caption = CreateSectionLabel(label);
            caption.Margin = new Padding(0, 0, 0, 2);
            input.Dock = DockStyle.Fill;

            container.Controls.Add(caption, 0, 0);
            container.Controls.Add(input, 0, 1);
            return container;
        }

        private Label CreateSectionLabel(string text)
        {
            var label = new Label
            {
                AutoSize = true,
                ForeColor = AppTheme.SecondaryText,
                Font = AppTheme.Fonts.SectionTitle,
                Margin = Padding.Empty,
            };
            AppLocalization.Bind(label, text);
            return label;
        }

        private void LoadEntryIntoControls()
        {
            _title.Text = _entry.Title;
            _project.Text = _entry.ProjectName ?? string.Empty;
            _technologies.Text = _entry.Technologies ?? string.Empty;
            _date.Value = _entry.DateResolved;
            _tags.Text = _entry.Tags ?? string.Empty;
            _favorite.Checked = _entry.IsFavorite;
            _description.Text = _entry.Description ?? string.Empty;
            _code.Text = _entry.CodeSnippet ?? string.Empty;
            _youtube.Text = _entry.YouTubeUrl ?? string.Empty;
            _comments.Text = _entry.Comments ?? string.Empty;
            _openVideo.Enabled = IsHttpUrl(_youtube.Text);

            RefreshAttachments();
            _dirty = false;
        }

        private void RefreshAttachments()
        {
            _attachments.SuspendLayout();
            _attachments.Controls.Clear();
            _attachments.RowStyles.Clear();
            _attachments.RowCount = 0;

            if (_entry.Attachments.Count == 0)
            {
                var empty = new Label
                {
                    AutoSize = true,
                    ForeColor = AppTheme.SecondaryText,
                    Margin = new Padding(0, 0, 0, 4),
                };
                AppLocalization.Bind(empty, "No attachments.");
                _attachments.Controls.Add(empty);
                _attachments.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            else
            {
                foreach (var attachment in _entry.Attachments.ToArray())
                {
                    _attachments.Controls.Add(BuildAttachmentRow(attachment));
                    _attachments.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                }
            }

            _attachments.ResumeLayout();

            _openGallery.Enabled = _entry.Attachments.Any(ImageGalleryForm.IsImage);

            CarbonTheme.Apply(_attachments);
            AppLocalization.Apply(_attachments);
        }

        private Control BuildAttachmentRow(AttachmentInfo attachment)
        {
            var info = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
            };
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            info.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var name = new Label
            {
                AutoSize = true,
                Text = string.IsNullOrWhiteSpace(attachment.FileName) ? "(unnamed)" : attachment.FileName,
                Margin = Padding.Empty,
            };

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(attachment.FilePath))
            {
                parts.Add(attachment.FilePath!);
            }
            if (attachment.DateAdded.HasValue)
            {
                parts.Add(string.Format(
                    AppLocalization.Culture,
                    AppLocalization.Translate("added {0}"),
                    attachment.DateAdded.Value.ToString("dd MMM yyyy", AppLocalization.Culture)));
            }

            var detail = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(620, 0),
                Text = string.Join("  \u00b7  ", parts),
                ForeColor = AppTheme.SecondaryText,
                Font = new Font(Font.FontFamily, 8f),
                Margin = Padding.Empty,
            };

            info.Controls.Add(name, 0, 0);
            info.Controls.Add(detail, 0, 1);

            var open = new SfButton
            {
                AutoSize = false,
                Size = new Size(64, 26),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(8, 0, 0, 0),
            };
            AppLocalization.Bind(open, "Open");
            open.Click += (_, _) => OpenAttachment(attachment);
            ButtonStyler.Apply(open, ButtonRole.Secondary);

            var remove = new SfButton
            {
                AutoSize = false,
                Size = new Size(80, 26),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(6, 0, 0, 0),
            };
            AppLocalization.Bind(remove, "Remove");
            remove.Click += (_, _) => RemoveAttachment(attachment);
            ButtonStyler.Apply(remove, ButtonRole.Danger);

            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 4),
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Controls.Add(info, 0, 0);
            row.Controls.Add(open, 1, 0);
            row.Controls.Add(remove, 2, 0);
            return row;
        }

        private void OpenGallery()
        {
            var images = _entry.Attachments.Where(ImageGalleryForm.IsImage).ToList();
            if (images.Count == 0)
            {
                return;
            }

            using var gallery = new ImageGalleryForm(images);
            gallery.ShowDialog(this);
        }

        internal void AddAttachment()
        {
            string[]? selectedFiles;
            if (FilePicker is not null)
            {
                selectedFiles = FilePicker();
            }
            else
            {
                using var dialog = new OpenFileDialog
                {
                    Title = AppLocalization.Translate("Add attachment"),
                    Filter = AppLocalization.Translate("All files (*.*)|*.*"),
                    CheckFileExists = true,
                    Multiselect = true,
                };

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                selectedFiles = dialog.FileNames;
            }

            if (selectedFiles is null || selectedFiles.Length == 0)
            {
                return;
            }

            var anyAdded = false;
            foreach (var path in selectedFiles)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (_entry.Attachments.Any(existing =>
                        string.Equals(existing.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                _entry.Attachments.Add(new AttachmentInfo
                {
                    KnowledgeEntryId = _entry.Id,
                    FileName = Path.GetFileName(path),
                    FilePath = path,
                    DateAdded = DateTime.Now,
                });
                anyAdded = true;
            }

            if (anyAdded)
            {
                MarkDirty();
                RefreshAttachments();
            }
        }

        private void RemoveAttachment(AttachmentInfo attachment)
        {
            _entry.Attachments.Remove(attachment);
            MarkDirty();
            RefreshAttachments();
        }

        private void OpenAttachment(AttachmentInfo attachment) =>
            EntryOpenActions.OpenAttachment(attachment);

        private void CopyCode()
        {
            if (string.IsNullOrEmpty(_code.Text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(_code.Text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void PreviewCode()
        {
            if (string.IsNullOrEmpty(_code.Text))
            {
                return;
            }

            using var form = new CodePreviewForm(_code.Text);
            form.ShowDialog(this);
        }

        private void OpenUrl(string? url) => EntryOpenActions.OpenUrl(url);

        private static bool IsHttpUrl(string? url) => EntryOpenActions.IsHttpUrl(url);

        private void MarkDirty() => _dirty = true;

        private void ApplyToEntry()
        {
            _entry.Title = _title.Text.Trim();
            _entry.ProjectName = NullIfBlank(_project.Text);
            _entry.Technologies = NullIfBlank(_technologies.Text);
            _entry.DateResolved = _date.Value;
            _entry.Tags = NullIfBlank(_tags.Text);
            _entry.IsFavorite = _favorite.Checked;
            _entry.Steps.Clear();
            for (var index = 0; index < _workingSteps.Count; index++)
            {
                _workingSteps[index].Order = index;
                _workingSteps[index].KnowledgeEntryId = _entry.Id;
                _entry.Steps.Add(_workingSteps[index]);
            }

            // Markdown / code / comments preserve their exact source whitespace.
            _entry.Description = string.IsNullOrEmpty(_description.Text) ? null : _description.Text;
            _entry.CodeSnippet = string.IsNullOrEmpty(_code.Text) ? null : _code.Text;
            _entry.Comments = string.IsNullOrEmpty(_comments.Text) ? null : _comments.Text;
            _entry.YouTubeUrl = NullIfBlank(_youtube.Text);
        }

        private async Task SaveAsync()
        {
            var title = _title.Text.Trim();
            if (title.Length == 0)
            {
                ShowError("A title is required.");

                // The title lives on the General tab, so make sure the user is
                // looking at it when the validation message appears.
                if (_editorTabs is not null && _editorTabs.TabPages.Count > 0)
                {
                    _editorTabs.SelectedIndex = 0;
                }

                _title.Focus();
                return;
            }

            HideError();
            ApplyToEntry();

            try
            {
                if (_saveButton is not null)
                {
                    _saveButton.Enabled = false;
                }

                await _service.SaveAsync(_entry);
                _dirty = false;
                BeginInvoke(new System.Action(() => _navigation.GoBack()));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowError("The knowledge entry could not be saved. Please try again.");
            }
            finally
            {
                if (_saveButton is not null)
                {
                    _saveButton.Enabled = true;
                }
            }
        }

        private void Cancel()
        {
            if (_dirty)
            {
                var confirmation = MessageBox.Show(
                    AppLocalization.Translate("Discard unsaved changes?"),
                    AppLocalization.Translate("Discard changes"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmation != DialogResult.Yes)
                {
                    return;
                }
            }

            BeginInvoke(new System.Action(() => _navigation.GoBack()));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                _ = SaveAsync();
                return true;
            }

            if (keyData == Keys.Escape)
            {
                Cancel();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowError(string message)
        {
            AppLocalization.Bind(_errorLabel, message);
            _errorLabel.Visible = true;
        }

        private void HideError()
        {
            _errorLabel.Text = string.Empty;
            _errorLabel.Visible = false;
        }

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
