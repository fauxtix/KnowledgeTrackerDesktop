using System.Diagnostics;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.WinForms.Controls;

namespace KnowledgeTrackerDesktop.UI.Views
{
    /// <summary>
    /// Statistics view (UI_DESIGN_SPEC section 7): a KPI summary, an activity
    /// chart and actionable top-project/top-tag lists that navigate to the
    /// Library with the matching filter. Presentation only; data comes from
    /// <see cref="IStatisticsService"/>.
    /// </summary>
    public class ctlStatistics : UserControl, IAppearingView
    {
        private enum StateKind
        {
            Loading,
            Error,
            Empty,
        }

        private readonly IStatisticsService _service;
        private readonly INavigationService _navigation;
        private readonly IShellHost _shell;

        private readonly Panel _contentHost = new();
        private readonly TableLayoutPanel _content = new();
        private readonly TableLayoutPanel _kpiRow = new();
        private readonly KpiCard _totalCard = new("Total entries", AppTheme.Accent);
        private readonly KpiCard _favoriteCard = new("Favorites", AppTheme.FavoriteOn);
        private readonly KpiCard _projectCard = new("Projects", AppTheme.AccentAlt);
        private readonly KpiCard _tagCard = new("Tags", AppTheme.Success);
        private readonly ActivityChart _activityChart = new();
        private readonly Panel _activityChartHost = new();
        private readonly Label _activityEmptyLabel = new();
        private readonly TableLayoutPanel _projectsList = new();
        private readonly TableLayoutPanel _tagsList = new();
        private readonly TableLayoutPanel _originsList = new();

        private readonly TableLayoutPanel _statePanel = new();
        private readonly Label _stateLabel = new();
        private readonly FlowLayoutPanel _stateActions = new();
        private readonly SfButton _retryButton = new();
        private readonly SfButton _createButton = new();
        private readonly SfButton _libraryButton = new();

        public ctlStatistics(
            IStatisticsService service,
            INavigationService navigation,
            IShellHost shell)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));

            Dock = DockStyle.Fill;
            BackColor = AppTheme.Background;

            Controls.Add(BuildRoot());
            _contentHost.Visible = false;
            AppLocalization.LanguageChanged += OnLanguageChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppLocalization.LanguageChanged -= OnLanguageChanged;
            }

            base.Dispose(disposing);
        }

        public async Task OnAppearingAsync()
        {
            _shell.SetPageTitle("Statistics");
            _shell.ClearHeaderActions();
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            ShowStateMessage("Loading your statistics...", StateKind.Loading);
            try
            {
                var summary = await _service.GetStatisticsAsync();
                Render(summary);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowStateMessage("Unable to load statistics.", StateKind.Error);
            }
        }

        private Control BuildRoot()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 1,
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.Controls.Add(BuildContentHost(), 0, 0);
            root.Controls.Add(BuildStatePanel(), 0, 0);
            return root;
        }

        private Control BuildContentHost()
        {
            _contentHost.Dock = DockStyle.Fill;
            _contentHost.AutoScroll = true;
            _contentHost.BackColor = AppTheme.Background;

            _content.Dock = DockStyle.Top;
            _content.AutoSize = true;
            _content.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _content.ColumnCount = 1;
            _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _content.Margin = Padding.Empty;
            _content.Padding = Padding.Empty;
            _content.BackColor = AppTheme.Background;

            _content.Controls.Add(BuildKpiRow());
            _content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _content.Controls.Add(BuildSectionHeader("Entries over time"));
            _content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            BuildActivityChart();
            _content.Controls.Add(_activityChartHost);
            _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 210f));

            _content.Controls.Add(BuildExplorerRow());
            _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 250f));

            _contentHost.Controls.Add(_content);
            return _contentHost;
        }

        private Control BuildKpiRow()
        {
            _kpiRow.Dock = DockStyle.Fill;
            _kpiRow.AutoSize = true;
            _kpiRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _kpiRow.ColumnCount = 4;
            _kpiRow.RowCount = 1;
            _kpiRow.Margin = new Padding(0, 0, 0, 10);
            _kpiRow.BackColor = AppTheme.Background;
            for (var index = 0; index < 4; index++)
            {
                _kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            }

            _kpiRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 92f));

            _totalCard.Dock = DockStyle.Fill;
            _favoriteCard.Dock = DockStyle.Fill;
            _projectCard.Dock = DockStyle.Fill;
            _tagCard.Dock = DockStyle.Fill;
            _totalCard.Margin = new Padding(0, 0, 10, 0);
            _favoriteCard.Margin = new Padding(0, 0, 10, 0);
            _projectCard.Margin = new Padding(0, 0, 10, 0);
            _tagCard.Margin = Padding.Empty;

            _kpiRow.Controls.Add(_totalCard, 0, 0);
            _kpiRow.Controls.Add(_favoriteCard, 1, 0);
            _kpiRow.Controls.Add(_projectCard, 2, 0);
            _kpiRow.Controls.Add(_tagCard, 3, 0);
            return _kpiRow;
        }

        private static Label BuildSectionHeader(string text)
        {
            var label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = AppTheme.Accent,
                Font = new Font(SystemFonts.DefaultFont.FontFamily, 10f, FontStyle.Bold),
                Margin = new Padding(0, 4, 0, 4),
            };
            AppLocalization.Bind(label, text);
            return label;
        }

        private void BuildActivityChart()
        {
            _activityChartHost.Dock = DockStyle.Fill;
            _activityChartHost.BackColor = AppTheme.Surface;
            _activityChartHost.Padding = new Padding(10, 6, 10, 6);
            _activityChartHost.Margin = new Padding(0, 0, 0, 8);

            _activityChart.Dock = DockStyle.Fill;
            _activityChart.BackColor = AppTheme.Surface;

            _activityEmptyLabel.Dock = DockStyle.Fill;
            _activityEmptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            _activityEmptyLabel.ForeColor = AppTheme.SecondaryText;
            _activityEmptyLabel.BackColor = AppTheme.Surface;
            _activityEmptyLabel.Font = AppTheme.Fonts.Caption;
            _activityEmptyLabel.Padding = new Padding(16);
            _activityEmptyLabel.Visible = false;

            _activityChartHost.Controls.Add(_activityChart);
            _activityChartHost.Controls.Add(_activityEmptyLabel);
            _activityEmptyLabel.BringToFront();
        }

        private Control BuildExplorerRow()
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            row.Controls.Add(BuildListSection("Top projects", _projectsList), 0, 0);
            row.Controls.Add(BuildListSection("Top tags", _tagsList), 1, 0);
            row.Controls.Add(BuildListSection("Origins", _originsList), 2, 0);
            return row;
        }

        private Control BuildListSection(string title, TableLayoutPanel list)
        {
            list.Dock = DockStyle.Fill;
            list.ColumnCount = 1;
            list.AutoScroll = true;
            list.Margin = new Padding(0, 0, 8, 0);
            list.Padding = new Padding(0, 4, 0, 0);

            var section = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            section.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            section.Controls.Add(BuildSectionHeader(title), 0, 0);
            section.Controls.Add(list, 0, 1);
            return section;
        }

        private Control BuildStatePanel()
        {
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
            _stateLabel.MaximumSize = new Size(460, 0);
            _stateLabel.TextAlign = ContentAlignment.MiddleCenter;
            _stateLabel.ForeColor = AppTheme.SecondaryText;
            _stateLabel.Margin = new Padding(0, 0, 0, 12);

            AppLocalization.Bind(_retryButton, "Retry");
            _retryButton.Size = new Size(120, 32);
            _retryButton.Margin = new Padding(0, 0, 8, 0);
            _retryButton.Click += async (_, _) => await LoadAsync();
            ButtonStyler.Apply(_retryButton, ButtonRole.Primary);

            AppLocalization.Bind(_createButton, "+ Create entry");
            _createButton.Size = new Size(130, 32);
            _createButton.Margin = new Padding(0, 0, 8, 0);
            _createButton.Click += (_, _) =>
                _navigation.NavigateTo<ctlAddEdit>(
                    new EntryEditorRequest(new KnowledgeEntry()));
            ButtonStyler.Apply(_createButton, ButtonRole.Primary);

            AppLocalization.Bind(_libraryButton, "Open Library");
            _libraryButton.Size = new Size(120, 32);
            _libraryButton.Margin = Padding.Empty;
            _libraryButton.Click += (_, _) =>
                _navigation.NavigateToPrimary<ctlMain>(new LibraryNavigationRequest());
            ButtonStyler.Apply(_libraryButton, ButtonRole.Secondary);

            _stateActions.FlowDirection = FlowDirection.LeftToRight;
            _stateActions.AutoSize = true;
            _stateActions.WrapContents = false;
            _stateActions.Margin = Padding.Empty;
            _stateActions.Controls.Add(_createButton);
            _stateActions.Controls.Add(_libraryButton);
            _stateActions.Controls.Add(_retryButton);

            stateContent.Controls.Add(_stateLabel);
            stateContent.Controls.Add(_stateActions);
            _statePanel.Controls.Add(stateContent, 0, 1);
            return _statePanel;
        }

        private void Render(StatisticsSummary summary)
        {
            if (summary.TotalEntries == 0)
            {
                ShowStateMessage(
                    "Your knowledge statistics will appear here once you create some entries.",
                    StateKind.Empty);
                return;
            }

            _totalCard.Value = summary.TotalEntries.ToString();
            _favoriteCard.Value = summary.FavoriteEntries.ToString();
            _projectCard.Value = summary.TotalProjects.ToString();
            _tagCard.Value = summary.TotalTags.ToString();

            PopulateActivityChart(summary.EntriesByMonth);
            PopulateList(_projectsList, summary.TopProjects, "No projects yet.", OpenLibraryProject);
            PopulateList(_tagsList, summary.TopTags, "No tags yet.", OpenLibraryTag);
            PopulateOriginList(summary.EntriesByOrigin, summary.EntriesWithoutOrigin);

            _statePanel.Visible = false;
            _contentHost.Visible = true;
        }

        private void PopulateActivityChart(IReadOnlyList<MonthCount> months)
        {
            _activityChart.SetMonths(months);
            _activityEmptyLabel.Visible = months.All(month => month.Count == 0);
            if (_activityEmptyLabel.Visible)
            {
                AppLocalization.Bind(
                    _activityEmptyLabel,
                    "No resolved entries in the last 12 months.\r\nSet a “Date resolved” value on an entry to see activity here.");
            }
        }

        private void PopulateList(
            TableLayoutPanel list,
            IReadOnlyList<NameCount> items,
            string emptyText,
            Action<string> onSelect)
        {
            list.SuspendLayout();
            list.Controls.Clear();
            list.RowStyles.Clear();
            list.ColumnCount = 1;

            if (items.Count == 0)
            {
                var emptyLabel = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Height = 28,
                    ForeColor = AppTheme.SecondaryText,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(0, 4, 0, 4),
                };
                AppLocalization.Bind(emptyLabel, emptyText);
                list.RowCount = 2;
                list.Controls.Add(emptyLabel, 0, 0);
                list.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
                list.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            }
            else
            {
                list.RowCount = items.Count + 1;
                for (var index = 0; index < items.Count; index++)
                {
                    var item = items[index];
                    var name = item.Name;
                    var row = new StatListRow(item.Name, item.Count, () => onSelect(name))
                    {
                        Dock = DockStyle.Fill,
                    };
                    list.Controls.Add(row, 0, index);
                    list.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
                }

                list.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            }

            list.ResumeLayout(true);
        }

        /// <summary>
        /// Fills the origin breakdown: one row per kind of source, plus the
        /// entries whose origin was never recorded. Every row opens the
        /// Library filtered by that kind.
        /// </summary>
        private void PopulateOriginList(
            IReadOnlyList<OriginCount> origins,
            int withoutOrigin)
        {
            _originsList.SuspendLayout();
            _originsList.Controls.Clear();
            _originsList.RowStyles.Clear();
            _originsList.ColumnCount = 1;

            var rows = new List<(string Label, int Count, OriginFilterOption Option)>();
            foreach (var origin in origins)
            {
                rows.Add((
                    AppLocalization.Translate(ProjectOriginRules.DescribeKind(origin.Kind)),
                    origin.Count,
                    OriginFilterOptionFor(origin.Kind)));
            }

            if (withoutOrigin > 0)
            {
                rows.Add((
                    AppLocalization.Translate("Not recorded"),
                    withoutOrigin,
                    OriginFilterOption.NotRecorded));
            }

            if (rows.Count == 0)
            {
                var emptyLabel = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    Height = 28,
                    ForeColor = AppTheme.SecondaryText,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(0, 4, 0, 4),
                };
                AppLocalization.Bind(emptyLabel, "No origins yet.");
                _originsList.RowCount = 2;
                _originsList.Controls.Add(emptyLabel, 0, 0);
                _originsList.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
                _originsList.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            }
            else
            {
                _originsList.RowCount = rows.Count + 1;
                for (var index = 0; index < rows.Count; index++)
                {
                    var item = rows[index];
                    var option = item.Option;
                    var row = new StatListRow(item.Label, item.Count, () => OpenLibraryOrigin(option))
                    {
                        Dock = DockStyle.Fill,
                    };
                    _originsList.Controls.Add(row, 0, index);
                    _originsList.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
                }

                _originsList.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            }

            _originsList.ResumeLayout(true);
        }

        private static OriginFilterOption OriginFilterOptionFor(ProjectOriginKind kind) => kind switch
        {
            ProjectOriginKind.LocalFolder => OriginFilterOption.LocalFolder,
            ProjectOriginKind.GitRepository => OriginFilterOption.GitRepository,
            ProjectOriginKind.WebPage => OriginFilterOption.WebPage,
            _ => OriginFilterOption.All,
        };

        private void OpenLibraryOrigin(OriginFilterOption option) =>
            _navigation.NavigateTo<ctlMain>(new LibraryNavigationRequest(OriginFilter: option));

        private void OpenLibraryProject(string project) =>
            _navigation.NavigateTo<ctlMain>(new LibraryNavigationRequest(ProjectName: project));

        private void OpenLibraryTag(string tag) =>
            _navigation.NavigateTo<ctlMain>(new LibraryNavigationRequest(Tag: tag));

        private void ShowStateMessage(string message, StateKind kind)
        {
            AppLocalization.Bind(_stateLabel, message);
            _retryButton.Visible = kind == StateKind.Error;
            _createButton.Visible = kind == StateKind.Empty;
            _libraryButton.Visible = kind == StateKind.Empty;
            _contentHost.Visible = false;
            _statePanel.Visible = true;
        }

        private void OnLanguageChanged(object? sender, EventArgs e) =>
            _ = LoadAsync();
    }
}
