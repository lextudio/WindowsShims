using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridTextColumn = System.Windows.Controls.DataGridTextColumn;
using WpfBinding = System.Windows.Data.Binding;

namespace LeXtudio.Windows.Sample;

// Runtime probe for the linked WPF DataGrid control root (session 23).
// Each step runs in isolation and reports pass/fail; the goal is to find the
// first behavior rung that needs a real implementation, not to look pretty.
public sealed partial class MainPage : Page
{
    public static int ProbeFailures { get; private set; }

    public sealed record Person(string Name, int Age, string City);

    private readonly StackPanel _report = new() { Spacing = 4 };

    public MainPage()
    {
        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new TextBlock
        {
            Text = "WPF DataGrid runtime probe",
            FontSize = 20,
            Margin = new Thickness(12),
        };
        Grid.SetRow(header, 0);
        layout.Children.Add(header);

        var body = new Grid { Margin = new Thickness(12) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(body, 1);
        layout.Children.Add(body);

        var reportScroller = new ScrollViewer { Content = _report };
        Grid.SetColumn(reportScroller, 0);
        body.Children.Add(reportScroller);

        var gridHost = new Border
        {
            BorderBrush = new SolidColorBrush(Colors.Gray),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(12, 0, 0, 0),
        };
        Grid.SetColumn(gridHost, 1);
        body.Children.Add(gridHost);

        Content = layout;

        RunProbe(gridHost);
    }

    private void RunProbe(Border gridHost)
    {
        WpfDataGrid? grid = null;

        Step("construct DataGrid (static + instance ctor)", () =>
        {
            grid = new WpfDataGrid();
        });

        if (grid is null)
        {
            Step("remaining steps", () => throw new InvalidOperationException("skipped: no DataGrid instance"));
            return;
        }

        Step("add explicit text columns", () =>
        {
            grid.Columns.Add(new WpfDataGridTextColumn { Header = "Name", Binding = new WpfBinding("Name") });
            grid.Columns.Add(new WpfDataGridTextColumn { Header = "Age", Binding = new WpfBinding("Age") });
            grid.Columns.Add(new WpfDataGridTextColumn { Header = "City", Binding = new WpfBinding("City") });
            if (grid.Columns.Count != 3)
            {
                throw new InvalidOperationException($"expected 3 columns, found {grid.Columns.Count}");
            }
        });

        Step("populate Items directly", () =>
        {
            grid.Items.Add(new Person("Ada", 36, "London"));
            grid.Items.Add(new Person("Grace", 45, "Arlington"));
            grid.Items.Add(new Person("Anita", 39, "Seattle"));
            if (grid.Items.Count != 3)
            {
                throw new InvalidOperationException($"expected 3 items, found {grid.Items.Count}");
            }
        });

        Step("read selection/command surface", () =>
        {
            _ = grid.SelectedCells;
            _ = WpfDataGrid.DeleteCommand;
            _ = WpfDataGrid.SelectAllCommand;
        });

        Step("toggle public options", () =>
        {
            grid.CanUserAddRows = false;
            grid.CanUserDeleteRows = false;
            grid.AutoGenerateColumns = false;
            grid.FrozenColumnCount = 1;
        });

        _grid = grid;

        Step("attach to visual tree", () =>
        {
            gridHost.Child = grid;
        });

        // The render path runs in OnApplyTemplate, which fires only after the
        // control enters a live visual tree and lays out. Verify post-Loaded.
        grid.Loaded += OnGridLoaded;

        if (App.ProbeMode)
        {
            // Fallback so a headless probe can never hang if Loaded never fires.
            _ = FallbackExitAsync();
        }
    }

    private WpfDataGrid? _grid;
    private bool _verified;

    private void OnGridLoaded(object sender, RoutedEventArgs e)
    {
        if (_verified || _grid is null)
        {
            return;
        }

        _verified = true;
        _grid.UpdateLayout();

        Step("first visible artifact: PART_ShimRowsHost populated", () =>
        {
            var host = FindDescendant(_grid!, "PART_ShimRowsHost");
            if (host is null)
            {
                throw new InvalidOperationException("PART_ShimRowsHost not found — template not applied");
            }

            var rowCount = VisualTreeHelper.GetChildrenCount(host);
            Console.WriteLine($"[probe]   rows host children (header + data rows) = {rowCount}");
            // 1 header row + 3 data rows.
            if (rowCount != 4)
            {
                throw new InvalidOperationException($"expected 4 host children (header + 3 rows), found {rowCount}");
            }
        });

        Step("report: grid desired size", () =>
        {
            Console.WriteLine($"[probe]   DesiredSize={_grid!.DesiredSize}");
            if (_grid!.DesiredSize.Width <= 0 || _grid.DesiredSize.Height <= 0)
            {
                throw new InvalidOperationException($"grid did not size: {_grid.DesiredSize}");
            }
        });

        Console.WriteLine($"[probe] DONE failures={ProbeFailures}");

        if (App.ProbeMode)
        {
            Environment.Exit(ProbeFailures == 0 ? 0 : 1);
        }
    }

    private static async Task FallbackExitAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(15));
        Console.WriteLine("[probe] DONE (fallback timeout — Loaded never fired)");
        Environment.Exit(2);
    }

    // Depth-first search for a named element in the live visual tree.
    private static FrameworkElement? FindDescendant(DependencyObject root, string name)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement fe && fe.Name == name)
            {
                return fe;
            }

            var found = FindDescendant(child, name);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private void Step(string name, Action action)
    {
        string outcome;
        var ok = false;
        try
        {
            action();
            outcome = "ok";
            ok = true;
        }
        catch (Exception ex)
        {
            ProbeFailures++;
            outcome = $"{ex.GetType().Name}: {ex.Message}";
        }

        Console.WriteLine($"[probe] {name}: {outcome}");
        _report.Children.Add(new TextBlock
        {
            Text = $"{(ok ? "✓" : "✗")} {name} — {outcome}",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(ok ? Colors.Green : Colors.Red),
        });
    }
}
