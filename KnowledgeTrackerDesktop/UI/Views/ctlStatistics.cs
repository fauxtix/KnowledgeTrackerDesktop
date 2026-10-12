using System.Diagnostics;
using KnowledgeTrackerDesktop.Core;
using KnowledgeTrackerDesktop.Services;
using KnowledgeTrackerDesktop.UI.Navigation;
using KnowledgeTrackerDesktop.UI.Theme;
using Syncfusion.Drawing;
using Syncfusion.WinForms.Controls;
using Syncfusion.Windows.Forms.Chart;

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
        private readonly Label _totalValue = new();
        private readonly Label _favoriteValue = new();
        private readonly Label _projectValue = new();
        private readonly Label _tagValue = new();
        private readonly ChartControl _activityChart = new();
        private readonly Panel _activityChartHost = new();
        private readonly Label _activityEmptyLabel = new();
        private readonly TableLayoutPanel _projectsList = new();
        private readonly TableLayoutPanel _tagsList = new();

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
            _contentHost.BackColor = AppTheme.Surface;

            _content.Dock = DockStyle.Top;
            _content.AutoSize = true;
            _content.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _content.ColumnCount = 1;
            _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _content.Margin = Padding.Empty;
            _content.Padding = Padding.Empty;

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
            _kpiRow.Margin = new Padding(0, 0, 0, 8);
            for (var index = 0; index < 4; index++)
            {
                _kpiRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            }

            _kpiRow.Controls.Add(BuildKpiCard("Total entries", _totalValue), 0, 0);
            _kpiRow.Controls.Add(BuildKpiCard("Favorites", _favoriteValue), 1, 0);
            _kpiRow.Controls.Add(BuildKpiCard("Projects", _projectValue), 2, 0);
            _kpiRow.Controls.Add(BuildKpiCard("Tags", _tagValue), 3, 0);
            return _kpiRow;
        }

        private Control BuildKpiCard(string caption, Label valueLabel)
        {
            valueLabel.AutoSize = false;
            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Text = "0";
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.ForeColor = AppTheme.Accent;
            valueLabel.Font = new Font(Font.FontFamily, 22f, FontStyle.Bold);
            valueLabel.Margin = Padding.Empty;

            var captionLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = caption,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = AppTheme.SecondaryText,
                Font = new Font(Font.FontFamily, 9f, FontStyle.Regular),
                Margin = Padding.Empty,
            };
            AppLocalization.Bind(captionLabel, caption);

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Height = 92,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 6, 12, 6),
                BackColor = AppTheme.Surface,
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 62f));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 38f));
            card.Controls.Add(valueLabel, 0, 0);
            card.Controls.Add(captionLabel, 0, 1);
            return card;
        }

        private static Label BuildSectionHeader(string text)
        {
            var label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = AppTheme.PrimaryText,
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
            _activityChartHost.Margin = new Padding(0, 0, 0, 8);

            _activityChart.Dock = DockStyle.Fill;
            _activityChart.BackColor = AppTheme.Surface;
            _activityChart.ShowLegend = false;
            _activityChart.ShowToolTips = true;
            _activityChart.PrimaryXAxis.ValueType = ChartValueType.Category;
            _activityChart.PrimaryXAxis.ForeColor = AppTheme.SecondaryText;
            _activityChart.PrimaryXAxis.Font = AppTheme.Fonts.Caption;
            _activityChart.PrimaryXAxis.TickColor = AppTheme.CardBorder;
            _activityChart.PrimaryYAxis.ForeColor = AppTheme.SecondaryText;
            _activityChart.PrimaryYAxis.Font = AppTheme.Fonts.Caption;
            _activityChart.PrimaryYAxis.TickColor = AppTheme.CardBorder;
            _activityChart.PrimaryYAxis.DrawGrid = true;
            _activityChart.ChartArea.BackInterior = new BrushInfo(AppTheme.Surface);

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
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            row.Controls.Add(BuildListSection("Top projects", _projectsList), 0, 0);
            row.Controls.Add(BuildListSection("Top tags", _tagsList), 1, 0);
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

            _totalValue.Text = summary.TotalEntries.ToString();
            _favoriteValue.Text = summary.FavoriteEntries.ToString();
            _projectValue.Text = summary.TotalProjects.ToString();
            _tagValue.Text = summary.TotalTags.ToString();

            PopulateActivityChart(summary.EntriesByMonth);
            PopulateList(_projectsList, summary.TopProjects, "No projects yet.", OpenLibraryProject);
            PopulateList(_tagsList, summary.TopTags, "No tags yet.", OpenLibraryTag);

            _statePanel.Visible = false;
            _contentHost.Visible = true;
        }

        private void PopulateActivityChart(IReadOnlyList<MonthCount> months)
        {
            _activityChart.Series.Clear();

            var series = new ChartSeries(AppLocalization.Translate("Entries"), ChartSeriesType.Column);
            series.Style.Interior = new BrushInfo(AppTheme.Accent);
            var index = 0;
            foreach (var month in months)
            {
                series.Points.Add(new ChartPoint(index, month.Count, MonthLabel(month)));
                index++;
            }

            _activityChart.Series.Add(series);
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
            list.RowCount = 0;

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
                list.Controls.Add(emptyLabel);
                list.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            else
            {
                foreach (var item in items)
                {
                    var name = item.Name;
                    var button = new SfButton
                    {
                        Text = $"{item.Name}    {item.Count}",
                        AutoSize = false,
                        Dock = DockStyle.Fill,
                        Height = 30,
                        Margin = new Padding(0, 0, 0, 4),
                    };
                    button.Click += (_, _) => onSelect(name);
                    list.Controls.Add(button);
                    ButtonStyler.Apply(button, ButtonRole.Ghost);
                    list.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
                }
            }

            list.ResumeLayout();
        }

        private static string MonthLabel(MonthCount month) =>
            new DateTime(month.Year, month.Month, 1).ToString("MMM yyyy", AppLocalization.Culture);

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
