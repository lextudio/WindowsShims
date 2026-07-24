using DataGrid.TestScenarios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WpfDataGrid = System.Windows.Controls.DataGrid;

namespace LeXtudio.Windows.Sample;

public sealed partial class MainPage : Page
{
    private sealed record Scenario(
        string Id,
        string Title,
        string Description,
        Func<WpfDataGrid> Build,
        Action<WpfDataGrid, Panel>? Optionize = null);

    // DataGrid's "primary shortcut" modifier matches each OS's own convention exactly: Cmd (⌘) on
    // macOS, Ctrl elsewhere — see DataGrid.IsPrimaryShortcutModifierDown (copy/select-all) and
    // ToSelectionModifiers (click-to-toggle-select). The instructional text below must say
    // whichever one this OS actually responds to.
    private static readonly bool IsMacOS =
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
    private static readonly string PrimaryModifierLabel = IsMacOS ? "Cmd" : "Ctrl";
    private static readonly string CopyShortcutLabel = $"{PrimaryModifierLabel}+C";

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
        new("sorting", "Sorting",
            "Toggle column sorting on and off. Click column headers to sort.",
            DataGridScenarios.BuildSortingGrid,
            (g, p) => p.AddToggle("CanUserSortColumns", g, nameof(WpfDataGrid.CanUserSortColumns))),
        new("selection", "Selection",
            $"Change selection mode and unit. {PrimaryModifierLabel}+click to select multiple in Extended mode.",
            DataGridScenarios.BuildSelectionGrid,
            (g, p) =>
            {
                p.AddCombo("SelectionMode", g, nameof(WpfDataGrid.SelectionMode),
                    ["Single", "Extended"]);
                p.AddCombo("SelectionUnit", g, nameof(WpfDataGrid.SelectionUnit),
                    ["Cell", "FullRow", "CellOrRowHeader"]);
            }),
        new("reorder", "Column Reorder & Resize",
            "Drag column headers to reorder. Drag column dividers to resize.",
            DataGridScenarios.BuildReorderGrid,
            (g, p) =>
            {
                p.AddToggle("CanUserReorderColumns", g, nameof(WpfDataGrid.CanUserReorderColumns));
                p.AddToggle("CanUserResizeColumns", g, nameof(WpfDataGrid.CanUserResizeColumns));
            }),
        new("clipboard", "Clipboard",
            $"Select cells and press {CopyShortcutLabel} to copy. Change clipboard mode below.",
            DataGridScenarios.BuildClipboardGrid,
            (g, p) => p.AddCombo("ClipboardCopyMode", g, nameof(WpfDataGrid.ClipboardCopyMode),
                ["Disabled", "EnableWithoutHeader", "IncludeHeader"])),
        new("column-types", "Column Types",
            "Different column types: CheckBox, ComboBox, Hyperlink, and Text columns.",
            DataGridScenarios.BuildColumnTypesGrid),
        new("column-sizing", "Column Sizing",
            "Columns sized with fixed, SizeToHeader, Star (*), and 2* width modes.",
            DataGridScenarios.BuildColumnSizingGrid),
        new("alternating", "Alternating Row",
            "AlternatingRowBackground highlights even rows for improved readability.",
            DataGridScenarios.BuildAlternatingRowGrid,
            (g, p) =>
            {
                var altBg = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0xFF, 0xF0, 0xF5, 0xFA));
                p.AddToggle("AlternatingRowBackground", g, nameof(WpfDataGrid.AlternatingRowBackground),
                    onValue: altBg, offValue: null);
            }),
        new("grid-lines", "Grid Lines",
            "GridLinesVisibility controls which lines appear. Customize the brush color below.",
            DataGridScenarios.BuildGridLinesGrid,
            (g, p) => p.AddCombo("GridLinesVisibility", g, nameof(WpfDataGrid.GridLinesVisibility),
                ["None", "Horizontal", "Vertical", "All"])),
        new("headers", "Headers",
            "HeadersVisibility controls whether column and row headers are displayed.",
            DataGridScenarios.BuildHeadersGrid,
            (g, p) => p.AddCombo("HeadersVisibility", g, nameof(WpfDataGrid.HeadersVisibility),
                ["All", "Column", "Row", "None"])),
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
            DataGridScenarios.BuildFrozenEditGrid,
            (g, p) =>
            {
                var slider = new Slider
                {
                    Minimum = 0, Maximum = 4, StepFrequency = 1,
                    Value = g.FrozenColumnCount, Header = "FrozenColumnCount",
                };
                slider.ValueChanged += (_, args) =>
                {
                    g.FrozenColumnCount = (int)args.NewValue;
                    g.InvalidateMeasure();
                    g.UpdateLayout();
                };
                p.Children.Add(slider);
            }),
        new("large-data", "Large Data",
            "10,000 rows demonstrating virtualization. Sort columns to test performance.",
            DataGridScenarios.BuildLargeDataGrid),
    ];

    private ElementTheme _sampleTheme = ElementTheme.Default;
    public MainPage()
    {
        InitializeComponent();
        BuildNavCategories();
        ApplyShellTheme();
    }

    private void BuildNavCategories()
    {
        AddCategory("Getting Started",
            Scenarios[0], Scenarios[14]);
        AddCategory("Interaction",
            Scenarios[3], Scenarios[4], Scenarios[5], Scenarios[6]);
        AddCategory("Appearance",
            Scenarios[10], Scenarios[11], Scenarios[9], Scenarios[8]);
        AddCategory("Columns",
            Scenarios[7]);
        AddCategory("Data",
            Scenarios[1], Scenarios[2], Scenarios[15], Scenarios[12], Scenarios[13], Scenarios[16]);
    }

    private void AddCategory(string _, params Scenario[] items)
    {
        if (NavView.MenuItems.Count > 0)
            NavView.MenuItems.Add(new NavigationViewItemSeparator());

        foreach (var s in items)
        {
            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = s.Title,
                Tag = s.Id,
                Name = $"nav-{s.Id}",
            });
        }
    }

    private void OnNavLoaded(object sender, RoutedEventArgs e)
    {
        if (NavView.MenuItems.Count > 0)
            NavView.SelectedItem = NavView.MenuItems[0];
    }

    private async void OnScenarioSelected(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if ((args.SelectedItem as NavigationViewItem)?.Tag is not string id)
            return;

        LoadingOverlay.Visibility = Visibility.Visible;
        await Task.Delay(50); // let the UI thread render the spinner

        var scenario = Array.Find(Scenarios, s => s.Id == id);
        if (scenario is null)
            return;

        var grid = scenario.Build();
        grid.HorizontalAlignment = HorizontalAlignment.Stretch;
        grid.VerticalAlignment = VerticalAlignment.Stretch;

        Panel? optionsPanel = null;
        if (scenario.Optionize is not null)
        {
            optionsPanel = new StackPanel { Spacing = 12, Width = 260 };
            scenario.Optionize(grid, optionsPanel);
        }

        var host = new ScenarioHost();
        host.Show(scenario.Title, scenario.Description, grid, optionsPanel);

        ScenarioContent.Content = host;
        LoadingOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        _sampleTheme = _sampleTheme == ElementTheme.Dark
            ? ElementTheme.Light
            : ElementTheme.Dark;
        RequestedTheme = _sampleTheme;
        Root.RequestedTheme = _sampleTheme;
        ApplyShellTheme();
        if (ScenarioContent.Content is Microsoft.UI.Xaml.UIElement content)
        {
            content.InvalidateMeasure();
            content.UpdateLayout();
        }
    }

    private void ApplyShellTheme()
    {
        var dark = _sampleTheme == ElementTheme.Dark;
        Root.Background = Brush(dark ? 0xFF202020 : 0xFFF3F3F3);
    }

    internal static SolidColorBrush Brush(uint argb) =>
        new(global::Windows.UI.Color.FromArgb(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb));
}

internal static class OptionsPanelExtensions
{
    static void Refresh(Microsoft.UI.Xaml.UIElement target)
    {
        if (target is WpfDataGrid grid)
        {
            grid.InvalidateMeasure();
            grid.UpdateLayout();
            return;
        }

        target.InvalidateMeasure();
        target.UpdateLayout();
    }

    public static void AddToggle(this Panel panel, string header, object target, string propertyName)
    {
        var toggle = new ToggleSwitch { Header = header, IsOn = true };
        var prop = target.GetType().GetProperty(propertyName);
        if (prop is not null)
        {
            toggle.IsOn = (bool)(prop.GetValue(target) ?? true);
            toggle.Toggled += (_, _) =>
            {
                prop.SetValue(target, toggle.IsOn);
                if (target is Microsoft.UI.Xaml.UIElement ui) Refresh(ui);
            };
        }
        panel.Children.Add(toggle);
    }

    public static void AddToggle(this Panel panel, string header, object target, string propertyName,
        object? onValue, object? offValue)
    {
        var toggle = new ToggleSwitch { Header = header, IsOn = onValue is not null };
        var prop = target.GetType().GetProperty(propertyName);
        if (prop is not null)
            toggle.Toggled += (_, _) =>
            {
                prop.SetValue(target, toggle.IsOn ? onValue : offValue);
                if (target is Microsoft.UI.Xaml.UIElement ui) Refresh(ui);
            };
        panel.Children.Add(toggle);
    }

    public static void AddCombo(this Panel panel, string header, object target, string propertyName,
        string[] values)
    {
        var prop = target.GetType().GetProperty(propertyName);
        var combo = new ComboBox
        {
            Header = header,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = values,
        };

        if (prop is not null)
        {
            var current = prop.GetValue(target)?.ToString();
            combo.SelectedItem = values.FirstOrDefault(v =>
                v.Equals(current, StringComparison.OrdinalIgnoreCase)) ?? values[0];
            combo.SelectionChanged += (_, args) =>
            {
                if (args.AddedItems.FirstOrDefault() is string selected)
                {
                    var enumType = prop.PropertyType;
                    if (enumType.IsEnum)
                    {
                        prop.SetValue(target, Enum.Parse(enumType, selected));
                        if (target is Microsoft.UI.Xaml.UIElement ui) Refresh(ui);
                    }
                }
            };
        }

        panel.Children.Add(combo);
    }
}
