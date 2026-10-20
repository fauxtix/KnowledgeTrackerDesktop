using System.Diagnostics;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Tools;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Library view (UI_DESIGN_SPEC section 4): a searchable/filterable card
    /// grid on the left and a read-only preview of the selected entry on the
    /// right. Presentation only; all data access goes through
    /// <see cref="IKnowledgeEntryService"/>.
    /// </summary>
    public class ctlMain : UserControl, IAppearingView
    {
        private static readonly (string Label, EntrySortMode Mode)[] SortOptions =
        {
            ("Newest first", EntrySortMode.DateResolvedDescending),
            ("Oldest first", EntrySortMode.DateResolvedAscending),
            ("Title A\u2013Z", EntrySortMode.TitleAscending),
            ("Title Z\u2013A", EntrySortMode.TitleDescending),
            ("Project A\u2013Z", EntrySortMode.ProjectAscending),
        };

        private readonly IKnowledgeEntryService _service;
        private readonly INavigationService _navigation;
        private readonly IShellHost _shell;

        private const int SearchDebounceMs = 250;

        private readonly TextBoxExt _search = new();
        private readonly SfButton _favoritesButton = new();
        private readonly SfButton _tagsButton = new();
        private readonly ComboBoxAdv _projectFilter = new();
        private readonly ComboBoxAdv _sort = new();
        private readonly FlowLayoutPanel _tagsPanel = new();
        private readonly ToolStripDropDown _tagsDropDown = new();
        private readonly Label _countLabel = new();
        private readonly FlowLayoutPanel _cards = new();
        private readonly ctlEntryPreview _preview = new();
        private readonly SplitContainer _split = new();
        private readonly TableLayoutPanel _statePanel = new();
        private readonly Label _stateLabel = new();
        private readonly SfButton _retryButton = new();
        private readonly SfButton _clearFiltersButton = new();
        private readonly System.Windows.Forms.Timer _searchDebounce = new();

        private readonly List<KnowledgeEntry> _allEntries = new();
        private readonly HashSet<string> _selectedTags = new(StringComparer.OrdinalIgnoreCase);
        private readonly LibraryNavigationRequest? _initialRequest;

        private int? _selectedEntryId;
        private bool _favoritesOnly;
        private bool _suppressEvents;
        private bool _initialRequestApplied;
        private bool _splitLayoutApplied;
        private bool _rebuildSuspended;
        private bool _rebuildPending;
        private string? _pendingProject;
        private int _filteredCount;

        public ctlMain(
            IKnowledgeEntryService service,
            INavigationService navigation,
            IShellHost shell,
            LibraryNavigationRequest? request)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _initialRequest = request;
            AppLocalization.LanguageChanged += OnLanguageChanged;

            _searchDebounce.Interval = SearchDebounceMs;
            _searchDebounce.Tick += OnSearchDebounceTick;

            // The preview only raises intents; the view keeps owning how each
            // action opens, exactly like the card events do.
            _preview.EditRequested += OnPreviewEditRequested;
            _preview.OpenCodeRequested += OnPreviewCodeRequested;
            _preview.OpenAttachmentsRequested += OnPreviewAttachmentsRequested;
            _preview.OpenAttachmentRequested += (_, attachment) =>
                EntryOpenActions.OpenAttachment(attachment);
            _preview.OpenLinkRequested += entry => EntryOpenActions.OpenUrl(entry.YouTubeUrl);

            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;

            Controls.Add(BuildRoot());
        }

        public async Task OnAppearingAsync()
        {
            var addButton = new SfButton
            {
                AutoSize = false,
                Size = new Size(120, 32),
                Margin = new Padding(0, 0, 0, 0),
            };
            AppLocalization.Bind(addButton, "+ New Entry");
            addButton.Click += (_, _) => OpenNewEntry();
            ButtonStyler.Apply(addButton, ButtonRole.Primary);

            _shell.SetPageTitle("Library");
            _shell.SetHeaderActions(addButton);
            UpdateFavoritesVisual();

            await LoadAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppLocalization.LanguageChanged -= OnLanguageChanged;
                _searchDebounce.Stop();
                _searchDebounce.Tick -= OnSearchDebounceTick;
                _searchDebounce.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnSearchDebounceTick(object? sender, EventArgs e)
        {
            _searchDebounce.Stop();
            RebuildList();
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            AppLocalization.Apply(this);
            RebuildTagFilters();
        }

        private void OpenNewEntry() =>
            _navigation.NavigateTo<ctlAddEdit>(
                new EntryEditorRequest(new KnowledgeEntry()));

        private Control BuildRoot()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.Controls.Add(BuildFilterBar(), 0, 0);
            root.Controls.Add(BuildSplitHost(), 0, 1);
            return root;
        }

        /// <summary>
        /// Splits the working area into the card list (left) and the read-only
        /// preview (right). The splitter stays draggable and the preview keeps
        /// its width when the window is resized, so the list absorbs the extra
        /// room.
        /// </summary>
        private Control BuildSplitHost()
        {
            _split.Dock = DockStyle.Fill;
            _split.FixedPanel = FixedPanel.Panel2;
            _split.BackColor = AppTheme.CardBorder;
            _split.Panel1.BackColor = AppTheme.Background;
            _split.Panel2.BackColor = AppTheme.Surface;

            // Applied from the split's own size change so the constraints are
            // always evaluated against the width the panes really have.
            _split.SizeChanged += (_, _) => ApplySplitLayout();

            _split.Panel1.Controls.Add(BuildListHost());
            _split.Panel2.Controls.Add(_preview);

            return _split;
        }

        /// <summary>
        /// Keeps both panes usable: the minimum sizes are applied as soon as the
        /// view is wide enough for them and the initial splitter position is
        /// chosen once, so a later drag by the user is never overridden.
        /// </summary>
        private void ApplySplitLayout()
        {
            if (_splitLayoutApplied)
            {
                return;
            }

            const int leftMinimum = 340;
            const int rightMinimum = 460;
            var available = _split.Width;

            // Too narrow for both minimum sizes: leave the pane proportions
            // alone rather than forcing a position the setters would reject.
            if (available < leftMinimum + rightMinimum + _split.SplitterWidth)
            {
                return;
            }

            // Order matters: every setter validates the current splitter
            // position, so it is parked at its minimum before the minimums are
            // raised, and only then moved to the initial position.
            _splitLayoutApplied = true;
            _split.SplitterDistance = leftMinimum;
            _split.Panel1MinSize = leftMinimum;
            _split.Panel2MinSize = rightMinimum;
            _split.SplitterDistance = Math.Clamp(
                (int)(available * 0.6),
                leftMinimum,
                available - rightMinimum - _split.SplitterWidth);
        }

        private Control BuildFilterBar()
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(0, 4, 0, 4),
            };

            _search.Multiline = false;
            _search.Height = 36;
            _search.Width = 260;
            _search.Anchor = AnchorStyles.Left;
            _search.Margin = new Padding(0, 4, 8, 4);
            _search.TextChanged += (_, _) => RestartSearchDebounce();

            ConfigureToggleButton(_favoritesButton, "Favorites");
            _favoritesButton.Click += (_, _) => ToggleFavorites();

            ConfigureToggleButton(_tagsButton, "Tags");
            _tagsButton.Click += (_, _) => ShowTagsDropDown();

            _projectFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _projectFilter.Width = 210;
            _projectFilter.Height = 44;
            _projectFilter.TextBoxHeight = 40;
            _projectFilter.Font = AppTheme.Fonts.Input;
            _projectFilter.Anchor = AnchorStyles.Left;
            _projectFilter.Margin = new Padding(0, 4, 8, 4);
            _projectFilter.SelectedIndexChanged += (_, _) =>
            {
                if (!_suppressEvents)
                {
                    RebuildList();
                }
            };

            _sort.DropDownStyle = ComboBoxStyle.DropDownList;
            _sort.Width = 250;
            _sort.Height = 44;
            _sort.TextBoxHeight = 40;
            _sort.Font = AppTheme.Fonts.Input;
            _sort.Anchor = AnchorStyles.Left;
            _sort.Margin = new Padding(0, 4, 0, 4);
            _sort.Items.AddRange(SortOptions.Select(option => (object)AppLocalization.Translate(option.Label)).ToArray());
            AppLocalization.BindComboItems(
                _sort,
                SortOptions.Select((option, index) => (index, option.Label)).ToArray());
            _sort.SelectedIndex = 0;
            _sort.SelectedIndexChanged += (_, _) =>
            {
                if (!_suppressEvents)
                {
                    RebuildList();
                }
            };

            BuildTagsDropDown();
            var projectFrame = new ComboBoxFrame(_projectFilter, _projectFilter.Width + 4);
            var sortFrame = new ComboBoxFrame(_sort, _sort.Width + 4);

            bar.Controls.Add(_search);
            bar.Controls.Add(_favoritesButton);
            bar.Controls.Add(_tagsButton);
            bar.Controls.Add(projectFrame);
            bar.Controls.Add(sortFrame);
            return bar;
        }

        private void ConfigureToggleButton(SfButton button, string text)
        {
            AppLocalization.Bind(button, text);
            button.AutoSize = false;
            button.Size = new Size(110, 38);
            button.Font = AppTheme.Fonts.Body;
            button.Anchor = AnchorStyles.Left;
            button.Margin = new Padding(0, 5, 8, 5);
            ButtonStyler.Apply(button, ButtonRole.Secondary);
        }

        private void BuildTagsDropDown()
        {
            _tagsPanel.FlowDirection = FlowDirection.TopDown;
            _tagsPanel.WrapContents = false;
            _tagsPanel.AutoScroll = true;
            _tagsPanel.BackColor = AppTheme.Surface;
            _tagsPanel.Width = 240;
            _tagsPanel.Height = 260;
            _tagsPanel.Padding = new Padding(6, 4, 6, 4);

            var host = new ToolStripControlHost(_tagsPanel)
            {
                AutoSize = false,
                Size = _tagsPanel.Size,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };

            _tagsDropDown.AutoSize = false;
            _tagsDropDown.Size = _tagsPanel.Size;
            _tagsDropDown.Padding = Padding.Empty;
            _tagsDropDown.Items.Add(host);
        }

        private void RebuildTagFilters()
        {
            _tagsPanel.SuspendLayout();
            _tagsPanel.Controls.Clear();

            var tags = EntryListFilter.CollectTags(_allEntries);
            var availableTags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
            _selectedTags.RemoveWhere(tag => !availableTags.Contains(tag));

            if (tags.Count == 0)
            {
                var emptyLabel = new Label
                {
                    AutoSize = true,
                    ForeColor = AppTheme.SecondaryText,
                    Margin = new Padding(2, 4, 2, 4),
                };
                AppLocalization.Bind(emptyLabel, "No tags yet.");
                _tagsPanel.Controls.Add(emptyLabel);
            }
            else
            {
                foreach (var tag in tags)
                {
                    var captured = tag;
                    var check = new CheckBoxAdv
                    {
                        Text = tag,
                        AutoSize = true,
                        Checked = _selectedTags.Contains(tag),
                        BackColor = AppTheme.Surface,
                        ForeColor = AppTheme.PrimaryText,
                        Font = AppTheme.Fonts.Body,
                        Margin = new Padding(2, 2, 2, 2),
                    };
                    check.CheckedChanged += (_, _) =>
                    {
                        if (_suppressEvents)
                        {
                            return;
                        }

                        if (check.Checked)
                        {
                            _selectedTags.Add(captured);
                        }
                        else
                        {
                            _selectedTags.Remove(captured);
                        }

                        if (IsHandleCreated)
                        {
                            RebuildList();
                        }
                    };
                    _tagsPanel.Controls.Add(check);
                }
            }

            _tagsPanel.ResumeLayout();
        }

        private Control BuildListHost()
        {
            _cards.Dock = DockStyle.Fill;
            _cards.AutoScroll = true;
            _cards.FlowDirection = FlowDirection.LeftToRight;
            _cards.WrapContents = true;
            _cards.BackColor = AppTheme.Background;
            _cards.Padding = new Padding(4, 4, 12, 12);
            _cards.Margin = Padding.Empty;

            _statePanel.Dock = DockStyle.Fill;
            _statePanel.ColumnCount = 1;
            _statePanel.RowCount = 3;
            _statePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            _statePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _statePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var stateContent = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Anchor = AnchorStyles.None,
            };

            _stateLabel.AutoSize = true;
            _stateLabel.MaximumSize = new Size(440, 0);
            _stateLabel.TextAlign = ContentAlignment.MiddleCenter;
            _stateLabel.Font = AppTheme.Fonts.Body;
            _stateLabel.ForeColor = AppTheme.SecondaryText;
            _stateLabel.Margin = new Padding(0, 0, 0, 12);

            AppLocalization.Bind(_retryButton, "Retry");
            _retryButton.Size = new Size(120, 32);
            _retryButton.Visible = false;
            _retryButton.Click += async (_, _) => await LoadAsync();
            ButtonStyler.Apply(_retryButton, ButtonRole.Primary);

            AppLocalization.Bind(_clearFiltersButton, "Clear filters");
            _clearFiltersButton.Size = new Size(140, 32);
            _clearFiltersButton.Visible = false;
            _clearFiltersButton.Click += (_, _) => ClearFilters();
            ButtonStyler.Apply(_clearFiltersButton, ButtonRole.Secondary);

            stateContent.Controls.Add(_stateLabel);
            stateContent.Controls.Add(_retryButton);
            stateContent.Controls.Add(_clearFiltersButton);
            _statePanel.Controls.Add(stateContent, 0, 1);

            var listPane = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = Padding.Empty,
            };
            listPane.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            listPane.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _countLabel.Dock = DockStyle.Fill;
            _countLabel.TextAlign = ContentAlignment.MiddleLeft;
            _countLabel.Font = AppTheme.Fonts.Caption;
            _countLabel.ForeColor = AppTheme.SecondaryText;
            _countLabel.Margin = Padding.Empty;
            AppLocalization.Bind(_countLabel, CountText);
            listPane.Controls.Add(_countLabel, 0, 0);

            var host = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Background };
            host.Controls.Add(_statePanel);
            host.Controls.Add(_cards);
            listPane.Controls.Add(host, 0, 1);

            return listPane;
        }

        private async Task LoadAsync()
        {
            ShowStateMessage("Loading your knowledge library...", retry: false);
            try
            {
                var entries = await _service.GetAllAsync();
                _allEntries.Clear();
                _allEntries.AddRange(entries);

                // The reloaded entries are new instances, so the preview is
                // re-pointed at the selected one or cleared when it is gone.
                if (_selectedEntryId is { } selectedId)
                {
                    var selected = _allEntries.FirstOrDefault(entry => entry.Id == selectedId);
                    if (selected is null)
                    {
                        _selectedEntryId = null;
                        _preview.Clear();
                    }
                    else
                    {
                        _preview.SetEntry(selected);
                    }
                }

                ApplyInitialRequest();
                PopulateFilters();
                RebuildList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowStateMessage("Unable to load the knowledge library.", retry: true);
            }
        }

        private void PopulateFilters()
        {
            _suppressEvents = true;
            try
            {
                RebuildTagFilters();

                var projects = _allEntries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.ProjectName))
                    .Select(entry => entry.ProjectName!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var target = _pendingProject ?? _projectFilter.SelectedItem as string;
                _pendingProject = null;
                _projectFilter.Items.Clear();
                _projectFilter.Items.Add(AppLocalization.Translate("All projects"));
                _projectFilter.Items.AddRange(projects.Cast<object>().ToArray());
                AppLocalization.BindComboItems(_projectFilter, (0, "All projects"));
                var projectIndex = target is null ? 0 : _projectFilter.Items.IndexOf(target);
                _projectFilter.SelectedIndex = projectIndex >= 0 ? projectIndex : 0;
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        private void ApplyInitialRequest()
        {
            if (_initialRequest is null || _initialRequestApplied)
            {
                return;
            }

            _initialRequestApplied = true;

            if (!string.IsNullOrWhiteSpace(_initialRequest.Tag))
            {
                _selectedTags.Add(_initialRequest.Tag.Trim());
            }

            if (!string.IsNullOrWhiteSpace(_initialRequest.ProjectName))
            {
                _pendingProject = _initialRequest.ProjectName.Trim();
            }
        }

        private void ShowTagsDropDown() =>
            _tagsDropDown.Show(_tagsButton, 0, _tagsButton.Height);

        private void ToggleFavorites()
        {
            _favoritesOnly = !_favoritesOnly;
            UpdateFavoritesVisual();
            RebuildList();
        }

        private void UpdateFavoritesVisual()
        {
            AppLocalization.Bind(
                _favoritesButton,
                () => _favoritesOnly
                    ? "\u2605 " + AppLocalization.Translate("Favorites")
                    : AppLocalization.Translate("Favorites"));
            _favoritesButton.Style.BackColor = _favoritesOnly ? AppTheme.FavoriteOn : AppTheme.Surface;
            _favoritesButton.Style.ForeColor = _favoritesOnly ? AppTheme.Background : AppTheme.PrimaryText;
        }

        private void RebuildList()
        {
            if (_rebuildSuspended)
            {
                _rebuildPending = true;
                return;
            }

            _rebuildPending = false;

            // Any rebuild already reflects the latest search text, so a pending
            // debounce tick would only repeat the same work.
            _searchDebounce.Stop();

            var project = _projectFilter.SelectedIndex > 0
                ? _projectFilter.SelectedItem as string
                : null;

            var filtered = EntryListFilter.Apply(
                _allEntries,
                _search.Text,
                _selectedTags,
                _favoritesOnly,
                CurrentSortMode(),
                project);

            // A selection that fell out of the current filter has no card left
            // to represent it, so the preview must not keep showing it.
            if (_selectedEntryId is not null &&
                filtered.All(entry => entry.Id != _selectedEntryId))
            {
                _selectedEntryId = null;
                _preview.Clear();
            }

            _cards.SuspendLayout();
            ClearCards();

            foreach (var entry in filtered)
            {
                var card = new EntryCard(entry);
                card.SetSelected(entry.Id == _selectedEntryId);
                card.Selected += OnCardSelected;
                card.OpenRequested += OnCardOpenRequested;
                card.FavoriteRequested += OnCardFavoriteRequested;
                card.DeleteRequested += OnCardDeleteRequested;
                _cards.Controls.Add(card);
            }

            _cards.ResumeLayout();

            _filteredCount = filtered.Count;
            AppLocalization.Apply(_countLabel);

            ApplyListState(filtered.Count);
        }

        /// <summary>
        /// Runs <paramref name="action"/> while coalescing list rebuilds: every
        /// rebuild requested inside the action is deferred to a single rebuild
        /// afterwards, so updating several filters at once does not rebuild the
        /// card list repeatedly.
        /// </summary>
        private void SuspendRebuild(System.Action action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (_rebuildSuspended)
            {
                action();
                return;
            }

            _rebuildSuspended = true;
            try
            {
                action();
            }
            finally
            {
                _rebuildSuspended = false;
            }

            if (_rebuildPending)
            {
                _rebuildPending = false;
                RebuildList();
            }
        }

        /// <summary>Removes and disposes the current cards so no GDI or menu
        /// resources are leaked when the list is rebuilt.</summary>
        private void ClearCards()
        {
            var cards = _cards.Controls.OfType<EntryCard>().ToArray();
            _cards.Controls.Clear();

            foreach (var card in cards)
            {
                card.Dispose();
            }
        }

        private void RestartSearchDebounce()
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private bool HasActiveFilters() =>
            !string.IsNullOrWhiteSpace(_search.Text) ||
            _favoritesOnly ||
            _selectedTags.Count > 0 ||
            _projectFilter.SelectedIndex > 0;

        private string CountText()
        {
            if (HasActiveFilters())
            {
                return string.Format(
                    AppLocalization.Culture,
                    AppLocalization.Translate("{0} of {1} entries"),
                    _filteredCount,
                    _allEntries.Count);
            }

            return _allEntries.Count == 1
                ? AppLocalization.Translate("1 entry")
                : string.Format(
                    AppLocalization.Culture,
                    AppLocalization.Translate("{0} entries"),
                    _allEntries.Count);
        }

        private void ClearFilters()
        {
            SuspendRebuild(() =>
            {
                _search.Text = string.Empty;
                _selectedTags.Clear();

                _suppressEvents = true;
                try
                {
                    _projectFilter.SelectedIndex = 0;
                }
                finally
                {
                    _suppressEvents = false;
                }

                _favoritesOnly = false;
                UpdateFavoritesVisual();
                RebuildTagFilters();

                // Coalesced by SuspendRebuild into a single rebuild, which also
                // cancels the debounce tick scheduled by clearing the search box.
                RebuildList();
            });
        }

        private EntrySortMode CurrentSortMode()
        {
            var index = _sort.SelectedIndex;
            return index >= 0 && index < SortOptions.Length
                ? SortOptions[index].Mode
                : EntrySortMode.DateResolvedDescending;
        }

        private void ApplyListState(int filteredCount)
        {
            if (filteredCount > 0)
            {
                _clearFiltersButton.Visible = false;
                _statePanel.Visible = false;
                _cards.Visible = true;
                _cards.BringToFront();
                return;
            }

            if (_allEntries.Count == 0)
            {
                ShowStateMessage(
                    "No knowledge entries yet.\r\n\r\nStart documenting the technical problems you solve.",
                    retry: false);
                return;
            }

            ShowStateMessage(
                "No entries match your search.\r\n\r\nTry a different search term or clear the filters.",
                retry: false,
                clearFilters: true);
        }

        private void ShowStateMessage(string message, bool retry, bool clearFilters = false)
        {
            AppLocalization.Bind(_stateLabel, message);
            _retryButton.Visible = retry;
            _clearFiltersButton.Visible = clearFilters;
            _cards.Visible = false;
            _statePanel.Visible = true;
            _statePanel.BringToFront();
        }

        private void OnCardSelected(EntryCard card)
        {
            _selectedEntryId = card.Entry.Id;
            foreach (Control control in _cards.Controls)
            {
                if (control is EntryCard other)
                {
                    other.SetSelected(other.Entry.Id == _selectedEntryId);
                }
            }

            // Selection is a preview concern: no navigation happens here.
            _preview.SetEntry(card.Entry);
        }

        private void OnCardOpenRequested(EntryCard card)
        {
            _selectedEntryId = card.Entry.Id;
            _navigation.NavigateTo<ctlAddEdit>(new EntryEditorRequest(card.Entry));
        }

        private async void OnCardFavoriteRequested(EntryCard card)
        {
            try
            {
                await _service.ToggleFavoriteAsync(card.Entry);
                card.RefreshFavorite();
                RebuildList();

                if (_selectedEntryId == card.Entry.Id)
                {
                    _preview.SetEntry(card.Entry);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(
                    AppLocalization.Translate("The favorite could not be updated. Please try again."),
                    "KnowledgeTracker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async void OnCardDeleteRequested(EntryCard card)
        {
            var confirmation = MessageBox.Show(
                string.Format(
                    AppLocalization.Culture,
                    AppLocalization.Translate("Delete \"{0}\"?\r\n\r\nThis also removes its steps and attachments."),
                    card.Entry.Title),
                AppLocalization.Translate("Delete entry"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            try
            {
                await _service.DeleteAsync(card.Entry.Id);
                _allEntries.RemoveAll(entry => entry.Id == card.Entry.Id);
                if (_selectedEntryId == card.Entry.Id)
                {
                    _selectedEntryId = null;
                    _preview.Clear();
                }

                PopulateFilters();
                RebuildList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(
                    AppLocalization.Translate("The entry could not be deleted. Please try again."),
                    "KnowledgeTracker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnPreviewEditRequested(KnowledgeEntry entry)
        {
            _selectedEntryId = entry.Id;
            _navigation.NavigateTo<ctlAddEdit>(new EntryEditorRequest(entry));
        }

        private void OnPreviewCodeRequested(KnowledgeEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.CodeSnippet))
            {
                return;
            }

            using var form = new CodePreviewForm(entry.CodeSnippet);
            form.ShowDialog(this);
        }

        /// <summary>
        /// Keeps the editor's attachment behaviour: the gallery when the entry
        /// has images, otherwise every file is opened with its default app.
        /// </summary>
        private void OnPreviewAttachmentsRequested(KnowledgeEntry entry)
        {
            var images = entry.Attachments.Where(ImageGalleryForm.IsImage).ToList();
            if (images.Count > 0)
            {
                using var gallery = new ImageGalleryForm(images);
                gallery.ShowDialog(this);
                return;
            }

            foreach (var attachment in entry.Attachments)
            {
                EntryOpenActions.OpenAttachment(attachment);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                _search.Focus();
                _search.SelectAll();
                return true;
            }

            if (keyData == (Keys.Control | Keys.N))
            {
                OpenNewEntry();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
