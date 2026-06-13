using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WpfDataGrid = System.Windows.Controls.DataGrid;
using WpfDataGridRow = System.Windows.Controls.DataGridRow;
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
            grid.Columns.Add(new WpfDataGridTextColumn
            {
                Header = "Age",
                Binding = new WpfBinding("Age"),
                Width = new System.Windows.Controls.DataGridLength(60),
            });
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

        Microsoft.UI.Xaml.Controls.Panel? host = null;

        Step("first visible artifact: PART_ShimRowsHost populated", () =>
        {
            host = FindDescendant(_grid!, "PART_ShimRowsHost") as Microsoft.UI.Xaml.Controls.Panel;
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

        Step("rows are DataGridRow containers hosting their own cells", () =>
        {
            // host children 1..3 are the data rows (child 0 is the header).
            var firstRow = host is null ? null : VisualTreeHelper.GetChild(host, 1) as WpfDataGridRow;
            if (firstRow is null)
            {
                throw new InvalidOperationException("first data row is not a DataGridRow");
            }

            var cellsHost = FindDescendant(firstRow, "PART_CellsHost");
            if (cellsHost is null)
            {
                throw new InvalidOperationException("PART_CellsHost not found in DataGridRow");
            }

            var cellCount = VisualTreeHelper.GetChildrenCount(cellsHost);
            Console.WriteLine($"[probe]   first row cell count = {cellCount}");
            if (cellCount != 3)
            {
                throw new InvalidOperationException($"expected 3 cells (one per column), found {cellCount}");
            }
        });

        Step("container generation: rows resolve via ItemContainerGenerator", () =>
        {
            var gen = _grid!.ItemContainerGenerator;
            Console.WriteLine($"[probe]   generator status = {gen.Status}");
            if (gen.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                throw new InvalidOperationException($"generator status = {gen.Status}, expected ContainersGenerated");
            }

            var item0 = _grid.Items[0];
            var byIndex = gen.ContainerFromIndex(0) as WpfDataGridRow;
            var byItem = gen.ContainerFromItem(item0) as WpfDataGridRow;
            if (byIndex is null)
            {
                throw new InvalidOperationException("ContainerFromIndex(0) is not a DataGridRow");
            }

            if (!ReferenceEquals(byIndex, byItem))
            {
                throw new InvalidOperationException("ContainerFromItem and ContainerFromIndex disagree");
            }

            var index = gen.IndexFromContainer(byIndex);
            Console.WriteLine($"[probe]   IndexFromContainer(row0) = {index}, ItemFromContainer matches item0 = {ReferenceEquals(gen.ItemFromContainer(byIndex), item0)}");
            if (index != 0 || !ReferenceEquals(gen.ItemFromContainer(byIndex), item0))
            {
                throw new InvalidOperationException("round-trip index/item resolution failed");
            }
        });

        Step("reactivity: adding an item adds a row", () =>
        {
            _grid!.Items.Add(new Person("Katherine", 41, "Hampton"));
            _grid.UpdateLayout();

            var rebuilt = FindDescendant(_grid, "PART_ShimRowsHost") as Microsoft.UI.Xaml.Controls.Panel;
            var rowCount = rebuilt is null ? 0 : VisualTreeHelper.GetChildrenCount(rebuilt);
            Console.WriteLine($"[probe]   host children after add = {rowCount}");
            // header + 4 data rows.
            if (rowCount != 5)
            {
                throw new InvalidOperationException($"expected 5 host children after add, found {rowCount}");
            }
        });

        Step("header uses DataGridColumnHeader and honors explicit width", () =>
        {
            var header = host is null ? null : VisualTreeHelper.GetChild(host, 0);
            var firstHeader = header is null ? null : VisualTreeHelper.GetChild(header, 0);
            if (firstHeader is not System.Windows.Controls.Primitives.DataGridColumnHeader)
            {
                throw new InvalidOperationException(
                    $"header cell is {firstHeader?.GetType().Name ?? "null"}, expected DataGridColumnHeader");
            }

            // Age column (index 1) has explicit Width=60.
            var ageHeader = VisualTreeHelper.GetChild(header!, 1) as FrameworkElement;
            Console.WriteLine($"[probe]   Age header width = {ageHeader?.Width}");
            if (ageHeader is null || Math.Abs(ageHeader.Width - 60) > 0.5)
            {
                throw new InvalidOperationException($"Age header width = {ageHeader?.Width}, expected 60");
            }
        });

        Step("selection: single-select clears the previous row", () =>
        {
            var gen = _grid!.ItemContainerGenerator;
            var row0 = gen.ContainerFromIndex(0) as WpfDataGridRow;
            var row1 = gen.ContainerFromIndex(1) as WpfDataGridRow;
            if (row0 is null || row1 is null)
            {
                throw new InvalidOperationException("expected at least two rows for selection test");
            }

            _grid.HandleShimRowClicked(row0);
            if (!row0.IsSelected || row1.IsSelected || !ReferenceEquals(_grid.SelectedItem, _grid.Items[0]))
            {
                throw new InvalidOperationException(
                    $"after click row0: row0.IsSelected={row0.IsSelected}, row1.IsSelected={row1.IsSelected}, SelectedItem match={ReferenceEquals(_grid.SelectedItem, _grid.Items[0])}");
            }

            _grid.HandleShimRowClicked(row1);
            if (row0.IsSelected || !row1.IsSelected || !ReferenceEquals(_grid.SelectedItem, _grid.Items[1]))
            {
                throw new InvalidOperationException(
                    $"after click row1: row0.IsSelected={row0.IsSelected}, row1.IsSelected={row1.IsSelected}");
            }

            var selectedBg = row1.Background is not null;
            var clearedBg = row0.Background is null;
            Console.WriteLine($"[probe]   row1 highlighted={selectedBg}, row0 cleared={clearedBg}");
            if (!selectedBg || !clearedBg)
            {
                throw new InvalidOperationException("selection visual (Background) not reflected");
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
