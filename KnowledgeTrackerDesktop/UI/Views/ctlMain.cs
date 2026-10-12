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
    /// grid. Presentation only; all data access goes through
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

        private readonly TextBoxExt _search = new();
        private readonly SfButton _favoritesButton = new();
        private readonly SfButton _tagsButton = new();
        private readonly ComboBoxAdv _projectFilter = new();
        private readonly ComboBoxAdv _sort = new();
        private readonly FlowLayoutPanel _tagsPanel = new();
        private readonly ToolStripDropDown _tagsDropDown = new();
        private readonly Label _countLabel = new();
        private readonly FlowLayoutPanel _cards = new();
        private readonly TableLayoutPanel _statePanel = new();
        private readonly Label _stateLabel = new();
        private readonly SfButton _retryButton = new();

        private readonly List<KnowledgeEntry> _allEntries = new();
        private readonly HashSet<string> _selectedTags = new(StringComparer.OrdinalIgnoreCase);
        private readonly LibraryNavigationRequest? _initialRequest;

        private int? _selectedEntryId;
        private bool _favoritesOnly;
        private bool _suppressEvents;
        private bool _initialRequestApplied;
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
            }

            base.Dispose(disposing);
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
            root.Controls.Add(BuildListHost(), 0, 1);
            return root;
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
            _search.TextChanged += (_, _) => RebuildList();

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

            stateContent.Controls.Add(_stateLabel);
            stateContent.Controls.Add(_retryButton);
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
            AppLocalization.Bind(_countLabel, () =>
                _filteredCount == 1
                    ? AppLocalization.Translate("1 entry")
                    : string.Format(
                        AppLocalization.Culture,
                        AppLocalization.Translate("{0} entries"),
                        _filteredCount));
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

                if (_selectedEntryId is not null &&
                    _allEntries.All(entry => entry.Id != _selectedEntryId))
                {
                    _selectedEntryId = null;
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

            _cards.SuspendLayout();
            _cards.Controls.Clear();

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
                retry: false);
        }

        private void ShowStateMessage(string message, bool retry)
        {
            AppLocalization.Bind(_stateLabel, message);
            _retryButton.Visible = retry;
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
