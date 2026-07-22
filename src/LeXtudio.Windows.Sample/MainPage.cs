using DataGrid.TestScenarios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WpfDataGrid = System.Windows.Controls.DataGrid;

namespace LeXtudio.Windows.Sample;

public sealed partial class MainPage : Page
{
    private sealed record Scenario(string Id, string Title, string Description, Func<WpfDataGrid> Build);

    private static readonly Scenario[] Scenarios =
    [
        new("basic", "Basic Grid",
            "A standard bound DataGrid with explicit text columns and sample metadata rows.",
            DataGridScenarios.BuildMetadataGrid),
        new("filter", "Auto Filter",
            "Auto-filter buttons enabled on every metadata column.",
            DataGridScenarios.BuildFilterGrid),
        new("hex-filter", "Hex Filter",
            "Hex-formatted filter templates on RID, Token, and Offset.",
            DataGridScenarios.BuildHexFilterGrid),
        new("row-details", "Row Details",
            "Customer orders with their product lines presented as nested row details.",
            DataGridScenarios.BuildRowDetailsGrid),
        new("variable-height", "Variable Height",
            "Service health checks where an active incident carries expanded operational context.",
            () => DataGridScenarios.BuildVariableHeightGrid(40, 5)),
        new("grouped", "Grouped",
            "A 32-person team directory grouped by country, role, and work location.",
            () => DataGridScenarios.BuildGroupedStyleGrid("format")),
        new("frozen-edit", "Frozen Edit",
            "Inventory planning with pinned SKU and Product columns and editable stock counts.",
            DataGridScenarios.BuildFrozenEditGrid),
    ];

    private readonly Grid _gridHost = new();
    private readonly TextBlock _description = new()
    {
        TextWrapping = TextWrapping.Wrap,
        Opacity = 0.8,
        Margin = new Thickness(0, 0, 0, 12),
    };

    public MainPage()
    {
        Content = BuildUi();
    }

    private UIElement BuildUi()
    {
        var nav = new NavigationView
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            OpenPaneLength = 260,
            IsSettingsVisible = false,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsPaneToggleButtonVisible = false,
            Header = "LeXtudio.Windows DataGrid",
        };

        foreach (var scenario in Scenarios)
        {
            nav.MenuItems.Add(new NavigationViewItem
            {
                Content = scenario.Title,
                Tag = scenario.Id,
                Name = $"nav-{scenario.Id}",
            });
        }

        var content = new Grid { Margin = new Thickness(24) };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var frame = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            BorderBrush = TryGetBrush("CardStrokeColorDefaultBrush")
                ?? new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xD0, 0xD0, 0xD0)),
            Background = TryGetBrush("CardBackgroundFillColorDefaultBrush")
                ?? new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA)),
            Child = _gridHost,
        };

        Grid.SetRow(_description, 0);
        Grid.SetRow(frame, 1);
        content.Children.Add(_description);
        content.Children.Add(frame);

        nav.Content = content;
        nav.SelectionChanged += (_, args) =>
        {
            if ((args.SelectedItem as NavigationViewItem)?.Tag is string id)
            {
                SelectScenario(id);
            }
        };
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

        _description.Text = scenario.Description;
        _gridHost.Children.Clear();

        var grid = scenario.Build();
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.VerticalAlignment = VerticalAlignment.Stretch;
        _gridHost.Children.Add(grid);
    }
}
