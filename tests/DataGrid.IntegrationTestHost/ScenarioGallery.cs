#if DEBUG
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using DataGrid.TestScenarios;
using WpfDataGrid = System.Windows.Controls.DataGrid;

namespace DataGrid.IntegrationTestHost;

// Left-nav scenario gallery for manual visual inspection of the DataGrid shim.
// Each scenario reuses the exact same grid-construction factory the headless
// HTTP probes in MainPage.cs call (BuildMetadataGrid, BuildFilterGrid, etc.) —
// there is exactly one place that builds each grid, so what a developer sees
// running this app is guaranteed to be what DataGrid.IntegrationTests actually
// exercises, not a separate hand-maintained demo that could drift out of sync.
//
// Selection is driven directly on the UI thread (not through MainPage's
// RunOnUi/DispatcherQueue.TryEnqueue + blocking Wait() helper used by the HTTP
// probes) — calling that from a NavigationView.SelectionChanged handler, which
// already runs on the UI thread, would deadlock: TryEnqueue only runs the
// queued callback on a later turn of the dispatcher loop, which can't happen
// while this handler is itself blocked on done.Wait().
public sealed partial class MainPage
{
    private sealed record Scenario(string Id, string Title, string Description, Func<WpfDataGrid> Build);

    private static readonly Scenario[] Scenarios =
    [
        new("basic", "Basic Grid",
            "A standard bound DataGrid: 7 text columns, 20 sample rows, real WPF cell editing.",
            DataGridScenarios.BuildMetadataGrid),
        new("filter", "Auto Filter",
            "Auto-filter buttons enabled on every column (DataGridExtensions-style text filtering).",
            DataGridScenarios.BuildFilterGrid),
        new("hex-filter", "Hex Filter",
            "Hex-formatted filter templates on the RID/Token/Offset columns.",
            DataGridScenarios.BuildHexFilterGrid),
        new("row-details", "Row Details (Nested Grid)",
            "Expandable row details hosting a nested DataGrid per master row.",
            DataGridScenarios.BuildRowDetailsGrid),
        new("variable-height", "Variable-Height Rows",
            "Virtualized rows where one row has a tall 150px details panel and the rest have none.",
            () => DataGridScenarios.BuildVariableHeightGrid(40, 5)),
        new("grouped", "Grouped + GroupStyle",
            "Rows grouped by Country, with GroupStyle.HeaderStringFormat producing a readable group header.",
            () => DataGridScenarios.BuildGroupedStyleGrid("format")),
        new("frozen-edit", "Frozen Columns (Editable)",
            "40-row editable grid with 2 frozen leading columns, real cell editing, and column resize.",
            DataGridScenarios.BuildFrozenEditGrid),
    ];

    private TextBlock? _descriptionText;

    private UIElement BuildGalleryUi()
    {
        var nav = new NavigationView
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            OpenPaneLength = 260,
            IsSettingsVisible = false,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsPaneToggleButtonVisible = false,
            Header = "DataGrid Scenario Gallery",
        };

        foreach (var scenario in Scenarios)
        {
            // Name gives DevFlow's UI-automation tree a stable, non-empty element
            // id for this item (NavigationViewItemPresenter, its template child,
            // has no IsSelected property DevFlow's tap fallback can drive —
            // this item itself does).
            nav.MenuItems.Add(new NavigationViewItem { Content = scenario.Title, Tag = scenario.Id, Name = $"nav-{scenario.Id}" });
        }

        _descriptionText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8,
            Margin = new Thickness(0, 0, 0, 12),
        };

        var card = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            // Fall back to a plain neutral gray if the Fluent theme dictionary
            // doesn't expose these keys on this Uno target — a visible card
            // outline either way, rather than a silently invisible border.
            BorderBrush = TryGetBrush("CardStrokeColorDefaultBrush")
                ?? new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xD0, 0xD0, 0xD0)),
            Background = TryGetBrush("CardBackgroundFillColorDefaultBrush")
                ?? new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA)),
            Child = _root,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };

        var content = new Grid { Margin = new Thickness(24) };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_descriptionText, 0);
        Grid.SetRow(card, 1);
        content.Children.Add(_descriptionText);
        content.Children.Add(card);

        nav.Content = content;
        nav.SelectionChanged += (_, args) =>
        {
            var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
            if (tag is not null)
            {
                SelectScenario(tag);
            }
        };

        // Select the first scenario once the pane's items are actually realized —
        // setting SelectedItem before NavigationView has materialized its menu
        // doesn't reliably fire SelectionChanged on every platform.
        nav.Loaded += (_, _) =>
        {
            if (nav.SelectedItem is null && nav.MenuItems.Count > 0)
            {
                nav.SelectedItem = nav.MenuItems[0];
            }
        };

        return nav;
    }

    private static Brush? TryGetBrush(string resourceKey)
        => Application.Current.Resources.TryGetValue(resourceKey, out var value) ? value as Brush : null;

    private void SelectScenario(string id)
    {
        var scenario = Array.Find(Scenarios, s => s.Id == id);
        if (scenario is null)
        {
            return;
        }

        if (_descriptionText is not null)
        {
            _descriptionText.Text = scenario.Description;
        }

        _root.Children.Clear();
        var grid = scenario.Build();
        _grid = grid;
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.VerticalAlignment = VerticalAlignment.Stretch;
        _root.Children.Add(grid);
    }
}
#endif
