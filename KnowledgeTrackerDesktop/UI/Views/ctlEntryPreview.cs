using System.ComponentModel;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Read-only preview of the entry selected in the library list (Fase 3).
    ///
    /// The pane only renders the <see cref="KnowledgeEntry"/> the selected card
    /// already carries: it never queries the database, never mutates the entry
    /// and never creates files. Actions are raised as events so the view keeps
    /// deciding how to open the editor, the code preview, the attachments and
    /// the video link.
    ///
    /// Every field is optional: missing values are omitted instead of being
    /// rendered as empty rows, and a null entry shows the empty state.
    /// </summary>
    public class ctlEntryPreview : UserControl
    {
        /// <summary>
        /// The rendered rows live in a single-column table docked to the top of
        /// the scrolling pane. Text blocks fill the column instead of wrapping at
        /// a fixed width, so dragging the splitter re-wraps them with no extra
        /// resize handling: the layout engine measures every AutoSize row against
        /// the real column width and the vertical scrollbar absorbs the overflow.
        /// </summary>
        private readonly TableLayoutPanel _content = new();

        public ctlEntryPreview()
        {
            Dock = DockStyle.Fill;
            AutoScroll = true;
            BackColor = AppTheme.Surface;
            Margin = Padding.Empty;
            Padding = new Padding(16, 12, 16, 16);

            _content.Dock = DockStyle.Top;
            _content.AutoSize = true;
            _content.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _content.ColumnCount = 1;
            _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _content.Margin = Padding.Empty;
            _content.BackColor = AppTheme.Surface;

            Controls.Add(_content);
            Render();
        }

        /// <summary>The entry being previewed, or null when the pane is empty.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public KnowledgeEntry? CurrentEntry { get; private set; }

        /// <summary>True while no entry is selected (the empty state is shown).</summary>
        public bool IsEmpty => CurrentEntry is null;

        public event Action<KnowledgeEntry>? EditRequested;

        public event Action<KnowledgeEntry>? OpenCodeRequested;

        public event Action<KnowledgeEntry>? OpenAttachmentsRequested;

        public event Action<KnowledgeEntry>? OpenLinkRequested;

        public event Action<KnowledgeEntry, AttachmentInfo>? OpenAttachmentRequested;

        public string TitleText => CurrentEntry?.Title ?? string.Empty;

        public string ProjectText =>
            string.IsNullOrWhiteSpace(CurrentEntry?.ProjectName)
                ? string.Empty
                : CurrentEntry.ProjectName.Trim();

        public string TagsText =>
            string.Join(", ", KnowledgeEntryTags.Split(CurrentEntry?.Tags));

        public string DateText => CurrentEntry?.DateResolved is { } date
            ? date.ToString("dd MMM yyyy", AppLocalization.Culture)
            : string.Empty;

        public string DescriptionText =>
            MarkdownPreviewRenderer.ToPlainText(CurrentEntry?.Description);

        public IReadOnlyList<string> StepHeadings =>
            StepRows().Select(row => StepHeading(row.Position)).ToArray();

        public IReadOnlyList<string> StepTexts =>
            StepRows().Select(row => row.Text).ToArray();

        public IReadOnlyList<string> AttachmentNames => CurrentEntry is null
            ? Array.Empty<string>()
            : CurrentEntry.Attachments
                .Select(AttachmentName)
                .Where(name => name.Length > 0)
                .ToArray();

        public bool ShowsFavorite => CurrentEntry?.IsFavorite == true;

        public bool ShowsProject => ProjectText.Length > 0;

        public bool ShowsTags => TagsText.Length > 0;

        public bool ShowsDate => DateText.Length > 0;

        public bool ShowsDescription => DescriptionText.Length > 0;

        public bool ShowsSteps => StepRows().Count > 0;

        public bool ShowsCodeAction =>
            !string.IsNullOrWhiteSpace(CurrentEntry?.CodeSnippet);

        public bool ShowsAttachmentsAction => CurrentEntry?.Attachments.Count > 0;

        public bool ShowsLinkAction =>
            EntryOpenActions.IsHttpUrl(CurrentEntry?.YouTubeUrl);

        /// <summary>Previews <paramref name="entry"/>, or shows the empty state when null.</summary>
        public void SetEntry(KnowledgeEntry? entry)
        {
            CurrentEntry = entry;
            Render();
        }

        /// <summary>Clears the preview so no stale entry is kept around.</summary>
        public void Clear() => SetEntry(null);

        public void RequestEditEntry()
        {
            if (CurrentEntry is { } entry)
            {
                EditRequested?.Invoke(entry);
            }
        }

        public void RequestOpenCode()
        {
            if (CurrentEntry is { } entry && ShowsCodeAction)
            {
                OpenCodeRequested?.Invoke(entry);
            }
        }

        public void RequestOpenAttachments()
        {
            if (CurrentEntry is { } entry && ShowsAttachmentsAction)
            {
                OpenAttachmentsRequested?.Invoke(entry);
            }
        }

        public void RequestOpenLink()
        {
            if (CurrentEntry is { } entry && ShowsLinkAction)
            {
                OpenLinkRequested?.Invoke(entry);
            }
        }

        public void RequestOpenAttachment(AttachmentInfo attachment)
        {
            ArgumentNullException.ThrowIfNull(attachment);

            if (CurrentEntry is { } entry && entry.Attachments.Contains(attachment))
            {
                OpenAttachmentRequested?.Invoke(entry, attachment);
            }
        }

        private void Render()
        {
            ResetRows();

            if (CurrentEntry is null)
            {
                AddRow(CreateEmptyState(), new RowStyle(SizeType.Absolute, 160f));
                return;
            }

            AddHeader();
            AddMetadata();
            AddActions();
            AddDescription();
            AddSteps();
            AddAttachments();
        }

        /// <summary>Replaces the rendered rows, disposing everything it drops.</summary>
        private void ResetRows()
        {
            var previous = _content.Controls.Cast<Control>().ToArray();
            _content.Controls.Clear();
            _content.RowStyles.Clear();
            _content.RowCount = 0;

            foreach (var control in previous)
            {
                control.Dispose();
            }
        }

        private void AddRow(Control control, RowStyle? style = null)
        {
            var index = _content.RowCount;
            _content.RowStyles.Add(style ?? new RowStyle(SizeType.AutoSize));
            _content.Controls.Add(control, 0, index);
            _content.RowCount = index + 1;
        }

        private static Control CreateEmptyState()
        {
            var label = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.Fonts.Body,
                ForeColor = AppTheme.SecondaryText,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            AppLocalization.Bind(label, "Select an entry to preview it.");
            return label;
        }

        private void AddHeader()
        {
            var title = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Font = AppTheme.Fonts.CardTitle,
                ForeColor = AppTheme.PrimaryText,
                Text = TitleText,
                Margin = Padding.Empty,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var favorite = new Label
            {
                AutoSize = false,
                Size = new Size(30, 30),
                Font = AppTheme.Fonts.CardTitle,
                ForeColor = ShowsFavorite ? AppTheme.FavoriteOn : AppTheme.FavoriteOff,
                Text = ShowsFavorite ? "\u2605" : "\u2606",
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(6, 0, 0, 0),
            };

            var header = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30f));
            header.Controls.Add(title, 0, 0);
            header.Controls.Add(favorite, 1, 0);
            AddRow(header);
        }

        private void AddMetadata()
        {
            if (ShowsProject)
            {
                AddCaption("Project");
                AddValue(ProjectText);
            }

            if (ShowsTags)
            {
                AddCaption("Tags");
                AddValue(TagsText);
            }

            if (ShowsDate)
            {
                AddCaption("Date resolved");
                AddValue(
                    () => DateText,
                    font: AppTheme.Fonts.Body,
                    color: AppTheme.PrimaryText);
            }
        }

        private void AddActions()
        {
            var actions = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Surface,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 16, 0, 0),
            };

            // Each button is paired with every caption it may show, in both
            // languages, so the equalised width is measured against the longest
            // one whichever language is active when the preview is rendered.
            var buttons = new List<(SfButton Button, IReadOnlyList<string> Captions)>();

            // Primary action: opening the editor is what the preview is for, so
            // it carries the accent fill and a taller footprint.
            buttons.Add((
                CreateActionButton("Edit Entry", RequestEditEntry, ButtonRole.Primary),
                Captions("Edit Entry")));

            if (ShowsCodeAction)
            {
                buttons.Add((
                    CreateActionButton("Open code", RequestOpenCode, ButtonRole.Secondary),
                    Captions("Open code")));
            }

            if (ShowsAttachmentsAction)
            {
                var count = CurrentEntry?.Attachments.Count ?? 0;
                buttons.Add((
                    CreateActionButton(
                        () => string.Format(
                            AppLocalization.Culture,
                            AppLocalization.Translate("Open attachments ({0})"),
                            CurrentEntry?.Attachments.Count ?? 0),
                        RequestOpenAttachments,
                        ButtonRole.Secondary),
                    FormattedCaptions("Open attachments ({0})", count)));
            }

            if (ShowsLinkAction)
            {
                buttons.Add((
                    CreateActionButton("Open video", RequestOpenLink, ButtonRole.Secondary),
                    Captions("Open video")));
            }

            actions.Controls.AddRange(buttons.Select(pair => pair.Button).ToArray());

            // Give every button the same width (the widest caption's) so they line
            // up instead of each sizing to its own text.
            EqualizeButtonWidths(buttons);

            AddRow(actions);
        }

        /// <summary>
        /// Widens every button to match the widest caption, so a row of actions
        /// reads as a set instead of a ragged line. Only the width is equalised;
        /// the Primary/Secondary height difference is kept on purpose.
        ///
        /// Every caption is measured in both languages and a fixed slack is added,
        /// so a single width fits whichever language is active (and the longest
        /// translation is never clipped by a couple of pixels).
        /// </summary>
        private static void EqualizeButtonWidths(
            IReadOnlyList<(SfButton Button, IReadOnlyList<string> Captions)> buttons)
        {
            if (buttons.Count == 0)
            {
                return;
            }

            var widest = 0;
            foreach (var (button, captions) in buttons)
            {
                foreach (var text in captions)
                {
                    var width = TextRenderer.MeasureText(
                        text,
                        button.Font,
                        Size.Empty,
                        TextFormatFlags.NoPadding).Width;

                    // Account for the button's horizontal padding and border, plus
                    // a small slack so the longest translation is never clipped by
                    // a few pixels of rendering difference.
                    const int slack = 10;
                    width += button.Padding.Horizontal + slack;
                    if (width > widest)
                    {
                        widest = width;
                    }
                }
            }

            if (widest <= 0)
            {
                return;
            }

            foreach (var (button, _) in buttons)
            {
                var height = button.MinimumSize.Height;
                button.AutoSize = false;
                button.MinimumSize = new Size(widest, height);
                button.Width = widest;
            }
        }

        /// <summary>
        /// The captions a text-bound button may show: the English key and its
        /// Portuguese translation, so the equalised width fits both languages.
        /// </summary>
        private static IReadOnlyList<string> Captions(string key) => new[]
        {
            key,
            AppLocalization.Translate(key, AppLanguage.Portuguese),
        };

        /// <summary>
        /// The formatted captions of a button whose text carries a value (the
        /// attachments count), in both languages, so the width fits whichever
        /// language is active.
        /// </summary>
        private static IReadOnlyList<string> FormattedCaptions(string formatKey, int value) => new[]
        {
            string.Format(AppLocalization.Culture, formatKey, value),
            string.Format(
                AppLocalization.Culture,
                AppLocalization.Translate(formatKey, AppLanguage.Portuguese),
                value),
        };

        private void AddDescription()
        {
            if (!ShowsDescription)
            {
                return;
            }

            AddCaption("Description");
            AddValue(DescriptionText);
        }

        private void AddSteps()
        {
            var rows = StepRows();
            if (rows.Count == 0)
            {
                return;
            }

            AddCaption("Resolution steps");

            foreach (var row in rows)
            {
                var position = row.Position;
                var text = row.Text;

                AddValue(
                    () => StepHeading(position),
                    font: AppTheme.Fonts.Caption,
                    color: AppTheme.SecondaryText,
                    topMargin: 8);
                AddValue(text);
            }
        }

        private void AddAttachments()
        {
            var names = AttachmentNames;
            if (names.Count == 0)
            {
                return;
            }

            AddCaption("Attachments");

            var attachments = CurrentEntry!.Attachments
                .Where(attachment => AttachmentName(attachment).Length > 0)
                .ToArray();

            for (var index = 0; index < attachments.Length; index++)
            {
                var captured = attachments[index];
                var link = new Label
                {
                    AutoSize = true,
                    Dock = DockStyle.Fill,
                    Cursor = Cursors.Hand,
                    Font = AppTheme.Fonts.Body,
                    ForeColor = AppTheme.Accent,
                    Margin = new Padding(0, 3, 0, 0),
                    Text = names[index],
                };
                link.Click += (_, _) => RequestOpenAttachment(captured);
                AddRow(link);
            }
        }

        /// <summary>
        /// Renders a section heading: a title in the accent colour with a
        /// full-width separator underneath, so each block of the preview
        /// ("Resolution steps", "Description", "Attachments", "Project",
        /// "Tags", "Date resolved") reads as a distinct section instead of a
        /// faint caption. Uses the same <see cref="AppTheme.Fonts.SectionTitle"/>
        /// as the editor cards for consistency.
        /// </summary>
        private void AddCaption(string key)
        {
            var section = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 20, 0, 4),
                Padding = Padding.Empty,
                BackColor = AppTheme.Surface,
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var caption = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Font = AppTheme.Fonts.SectionTitle,
                ForeColor = AppTheme.Accent,
                Margin = Padding.Empty,
            };
            AppLocalization.Bind(caption, key);

            // Full-width underline separating this heading from its content.
            var separator = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = AppTheme.CardBorder,
                Margin = new Padding(0, 4, 0, 0),
            };

            section.Controls.Add(caption, 0, 0);
            section.Controls.Add(separator, 0, 1);
            AddRow(section);
        }

        private void AddValue(
            string text,
            Font? font = null,
            Color? color = null,
            int topMargin = 0)
        {
            AddValue(() => text, font, color, topMargin);
        }

        private void AddValue(
            Func<string> textFactory,
            Font? font = null,
            Color? color = null,
            int topMargin = 0)
        {
            var value = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Font = font ?? AppTheme.Fonts.Body,
                ForeColor = color ?? AppTheme.PrimaryText,
                Margin = new Padding(0, topMargin, 0, 0),
            };
            AppLocalization.Bind(value, textFactory);
            AddRow(value);
        }

        /// <summary>
        /// Creates an action button. Its width is provisional: <see cref="AddActions"/>
        /// equalises the whole row to the widest caption afterwards. The role
        /// drives the visual hierarchy (Primary is filled and taller).
        /// </summary>
        private static SfButton CreateActionButton(string key, Action onClick, ButtonRole role)
        {
            var button = CreateButton(onClick, role);
            AppLocalization.Bind(button, key);
            ButtonStyler.Apply(button, role);
            return button;
        }

        private static SfButton CreateActionButton(Func<string> textFactory, Action onClick, ButtonRole role)
        {
            var button = CreateButton(onClick, role);
            AppLocalization.Bind(button, textFactory);
            ButtonStyler.Apply(button, role);
            return button;
        }

        private static SfButton CreateButton(Action onClick, ButtonRole role)
        {
            // The primary action is slightly taller so it reads as the main
            // call to action; the secondary ones stay compact.
            var height = role == ButtonRole.Primary ? 38 : 34;
            var button = new SfButton
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, height),
                Padding = new Padding(14, 0, 14, 0),
                Margin = new Padding(0, 0, 8, 0),
            };
            button.Click += (_, _) => onClick();
            return button;
        }

        private static string AttachmentName(AttachmentInfo attachment)
        {
            if (!string.IsNullOrWhiteSpace(attachment.FileName))
            {
                return attachment.FileName.Trim();
            }

            return string.IsNullOrWhiteSpace(attachment.FilePath)
                ? string.Empty
                : Path.GetFileName(attachment.FilePath);
        }

        /// <summary>
        /// Steps in display order; the legacy free text is the fallback. Each row
        /// keeps its 1-based position so its heading can be re-rendered from the
        /// current language instead of a value frozen at render time.
        /// </summary>
        private IReadOnlyList<(int Position, string Text)> StepRows()
        {
            if (CurrentEntry is null)
            {
                return Array.Empty<(int Position, string Text)>();
            }

            var rows = new List<(int Position, string Text)>();
            var steps = CurrentEntry.Steps.OrderBy(step => step.Order).ToArray();

            for (var index = 0; index < steps.Length; index++)
            {
                var text = steps[index].Description?.Trim() ?? string.Empty;
                if (text.Length == 0)
                {
                    continue;
                }

                rows.Add((index + 1, text));
            }

            if (rows.Count == 0 &&
                !string.IsNullOrWhiteSpace(CurrentEntry.ResolutionSteps))
            {
                rows.Add((1, CurrentEntry.ResolutionSteps.Trim()));
            }

            return rows;
        }

        private static string StepHeading(int position) =>
            string.Format(
                AppLocalization.Culture,
                AppLocalization.Translate("Step {0}"),
                position);
    }
}
