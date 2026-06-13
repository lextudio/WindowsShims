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

    public sealed class Person
    {
        public Person(string name, int age, string city)
        {
            Name = name;
            Age = age;
            City = city;
        }

        public string Name { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
    }

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

        Step("header click sorts rows (ascending then descending)", () =>
        {
            // Age column (index 1). Items: 36, 45, 39, 41 (Katherine added).
            var ageColumn = _grid!.Columns[1];

            _grid.HandleShimHeaderClicked(ageColumn);
            var asc = RowAges();
            Console.WriteLine($"[probe]   ages ascending = [{string.Join(",", asc)}]");
            if (!IsSorted(asc, ascending: true))
            {
                throw new InvalidOperationException($"ages not ascending: [{string.Join(",", asc)}]");
            }

            _grid.HandleShimHeaderClicked(ageColumn);
            var desc = RowAges();
            Console.WriteLine($"[probe]   ages descending = [{string.Join(",", desc)}]");
            if (!IsSorted(desc, ascending: false))
            {
                throw new InvalidOperationException($"ages not descending: [{string.Join(",", desc)}]");
            }
        });

        Step("selection survives a sort rebuild (by item identity)", () =>
        {
            var gen = _grid!.ItemContainerGenerator;
            var row = gen.ContainerFromIndex(0) as WpfDataGridRow;
            if (row is null)
            {
                throw new InvalidOperationException("no row to select");
            }

            _grid.HandleShimRowClicked(row);
            var selectedItem = row.Item;

            // Re-sort (ascending) — rows are rebuilt; the selected item should
            // keep its highlight on whatever row now holds it.
            _grid.HandleShimHeaderClicked(_grid.Columns[1]);

            var stillSelected = false;
            for (var i = 0; ; i++)
            {
                if (gen.ContainerFromIndex(i) is not WpfDataGridRow r)
                {
                    break;
                }

                if (ReferenceEquals(r.Item, selectedItem))
                {
                    stillSelected = r.IsSelected;
                    break;
                }
            }

            Console.WriteLine($"[probe]   selected item still highlighted after sort = {stillSelected}");
            if (!stillSelected)
            {
                throw new InvalidOperationException("selection was lost across the sort rebuild");
            }
        });

        Step("removing the selected item clears the selection", () =>
        {
            var gen = _grid!.ItemContainerGenerator;
            var row = gen.ContainerFromIndex(0) as WpfDataGridRow;
            if (row?.Item is not { } item)
            {
                throw new InvalidOperationException("no row/item to select");
            }

            _grid.HandleShimRowClicked(row);
            if (!ReferenceEquals(_grid.SelectedItem, item))
            {
                throw new InvalidOperationException("precondition failed: item not selected");
            }

            _grid.Items.Remove(item);

            Console.WriteLine($"[probe]   SelectedItem after remove = {(_grid.SelectedItem is null ? "null" : "set")}");
            if (_grid.SelectedItem is not null)
            {
                throw new InvalidOperationException("selection was not cleared after removing the selected item");
            }

            // And no remaining row should be highlighted.
            for (var i = 0; ; i++)
            {
                if (gen.ContainerFromIndex(i) is not WpfDataGridRow r)
                {
                    break;
                }

                if (r.IsSelected)
                {
                    throw new InvalidOperationException("a row is still highlighted after the selected item was removed");
                }
            }
        });

        Step("keyboard navigation moves selection (Down/Up)", () =>
        {
            var gen = _grid!.ItemContainerGenerator;
            var row0 = gen.ContainerFromIndex(0) as WpfDataGridRow;
            _grid.HandleShimRowClicked(row0!);

            _grid.MoveSelectionByOffset(1);
            var down = gen.ContainerFromIndex(1) as WpfDataGridRow;
            if (down is null || !down.IsSelected || (gen.ContainerFromIndex(0) as WpfDataGridRow)!.IsSelected)
            {
                throw new InvalidOperationException("Down did not move selection to row 1");
            }

            _grid.MoveSelectionByOffset(-1);
            if (!(gen.ContainerFromIndex(0) as WpfDataGridRow)!.IsSelected || down.IsSelected)
            {
                throw new InvalidOperationException("Up did not move selection back to row 0");
            }
        });

        Step("Home/End move selection to first/last row", () =>
        {
            var gen = _grid!.ItemContainerGenerator;
            var rowCount = 0;
            while (gen.ContainerFromIndex(rowCount) is WpfDataGridRow)
            {
                rowCount++;
            }

            _grid.MoveSelectionToIndex(int.MaxValue);
            var last = gen.ContainerFromIndex(rowCount - 1) as WpfDataGridRow;
            if (last is null || !last.IsSelected)
            {
                throw new InvalidOperationException("End did not select the last row");
            }

            _grid.MoveSelectionToIndex(0);
            var first = gen.ContainerFromIndex(0) as WpfDataGridRow;
            if (first is null || !first.IsSelected || last.IsSelected)
            {
                throw new InvalidOperationException("Home did not select the first row");
            }
        });

        Step("cell-level selection honors SelectionUnit.Cell", () =>
        {
            _grid!.SelectionUnit = System.Windows.Controls.DataGridSelectionUnit.Cell;
            _grid.UpdateLayout(); // realize rows' cells after prior rebuilds

            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var row1 = _grid.ItemContainerGenerator.ContainerFromIndex(1) as WpfDataGridRow;
            var cellA = row0?.TryGetCell(0);
            var cellB = row1?.TryGetCell(2);
            if (cellA is null || cellB is null)
            {
                throw new InvalidOperationException("could not resolve cells for cell-selection test");
            }

            Console.WriteLine($"[probe]   SelectionUnit = {_grid.SelectionUnit}");
            _grid.HandleShimCellClicked(cellA);
            if (!cellA.IsSelected || row0!.IsSelected)
            {
                throw new InvalidOperationException(
                    $"cell click did not select only the cell (cellA.IsSelected={cellA.IsSelected}, row0.IsSelected={row0!.IsSelected})");
            }

            _grid.HandleShimCellClicked(cellB);
            if (cellA.IsSelected || !cellB.IsSelected)
            {
                throw new InvalidOperationException("cell selection did not move to the new cell");
            }

            // CurrentCell / SelectedCells reflect the shim cell selection.
            var current = _grid.CurrentCell;
            Console.WriteLine($"[probe]   CurrentCell column={current.Column?.Header}, SelectedCells={_grid.SelectedCells.Count}");
            if (!ReferenceEquals(current.Column, cellB.Column) || _grid.SelectedCells.Count != 1
                || !ReferenceEquals(_grid.SelectedCells[0].Column, cellB.Column))
            {
                throw new InvalidOperationException("CurrentCell/SelectedCells not updated from cell selection");
            }

            // Retain across a rebuild: sort, then the same (item, column) cell
            // should be re-selected on the rebuilt row.
            var selItem = cellB.RowOwner!.Item;
            var selColumn = cellB.Column;
            _grid.HandleShimHeaderClicked(_grid.Columns[0]); // sort by Name → rebuild
            _grid.UpdateLayout();

            var reselected = false;
            for (var i = 0; _grid.ItemContainerGenerator.ContainerFromIndex(i) is WpfDataGridRow r; i++)
            {
                if (!ReferenceEquals(r.Item, selItem))
                {
                    continue;
                }

                for (var c = 0; c < _grid.Columns.Count; c++)
                {
                    var cc = r.TryGetCell(c);
                    if (cc is not null && ReferenceEquals(cc.Column, selColumn))
                    {
                        reselected = cc.IsSelected;
                    }
                }
            }

            Console.WriteLine($"[probe]   cell reselected after sort = {reselected}");
            if (!reselected)
            {
                throw new InvalidOperationException("cell selection was lost across the rebuild");
            }

            // Removing the cell-selected item clears the cell selection surface.
            _grid.Items.Remove(selItem);
            _grid.UpdateLayout();
            Console.WriteLine($"[probe]   after remove: SelectedCells={_grid.SelectedCells.Count}, CurrentCell.IsValid={_grid.CurrentCell.IsValid}");
            if (_grid.SelectedCells.Count != 0 || _grid.CurrentCell.IsValid)
            {
                throw new InvalidOperationException("cell selection not cleared after removing its item");
            }

            _grid.SelectionUnit = System.Windows.Controls.DataGridSelectionUnit.FullRow;
        });

        Step("cell editing writes back to the item (Age int)", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var ageCell = row0?.TryGetCell(1); // Age column
            if (ageCell is null)
            {
                throw new InvalidOperationException("could not resolve Age cell");
            }

            if (!ageCell.BeginEdit(null))
            {
                throw new InvalidOperationException("BeginEdit failed");
            }

            if (ageCell.Content is not TextBox box)
            {
                throw new InvalidOperationException($"editing content is {ageCell.Content?.GetType().Name ?? "null"}, expected TextBox");
            }

            box.Text = "99";
            if (!ageCell.CommitEdit())
            {
                throw new InvalidOperationException("CommitEdit failed");
            }

            var age = ((Person)row0!.Item!).Age;
            Console.WriteLine($"[probe]   after edit: item.Age={age}, IsEditing={ageCell.IsEditing}");
            if (age != 99 || ageCell.IsEditing || ageCell.Content is TextBox)
            {
                throw new InvalidOperationException($"edit not committed (Age={age}, IsEditing={ageCell.IsEditing})");
            }
        });

        Step("multi-select: Ctrl adds, Shift ranges, plain click resets", () =>
        {
            _grid!.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Extended;
            // Ensure at least 3 rows (earlier steps removed some items).
            while (_grid.Items.Count < 3)
            {
                _grid.Items.Add(new Person($"P{_grid.Items.Count}", 20 + _grid.Items.Count, "Town"));
            }

            _grid.UpdateLayout();
            var gen = _grid.ItemContainerGenerator;
            var r0 = gen.ContainerFromIndex(0) as WpfDataGridRow;
            var r1 = gen.ContainerFromIndex(1) as WpfDataGridRow;
            var r2 = gen.ContainerFromIndex(2) as WpfDataGridRow;
            if (r0 is null || r1 is null || r2 is null)
            {
                throw new InvalidOperationException("need 3 rows for multi-select test");
            }

            const global::Windows.System.VirtualKeyModifiers none = global::Windows.System.VirtualKeyModifiers.None;
            const global::Windows.System.VirtualKeyModifiers ctrl = global::Windows.System.VirtualKeyModifiers.Control;
            const global::Windows.System.VirtualKeyModifiers shift = global::Windows.System.VirtualKeyModifiers.Shift;

            _grid.HandleShimRowClicked(r0, none);
            _grid.HandleShimRowClicked(r1, ctrl);
            Console.WriteLine($"[probe]   after Ctrl: count={_grid.ShimSelectedItems.Count}, r0={r0.IsSelected}, r1={r1.IsSelected}");
            if (_grid.ShimSelectedItems.Count != 2 || !r0.IsSelected || !r1.IsSelected)
            {
                throw new InvalidOperationException("Ctrl did not add to the selection");
            }

            _grid.HandleShimRowClicked(r2, shift); // anchor r1 → range r1..r2
            Console.WriteLine($"[probe]   after Shift: count={_grid.ShimSelectedItems.Count}, r0={r0.IsSelected}, r1={r1.IsSelected}, r2={r2.IsSelected}");
            if (_grid.ShimSelectedItems.Count != 2 || r0.IsSelected || !r1.IsSelected || !r2.IsSelected)
            {
                throw new InvalidOperationException("Shift range incorrect");
            }

            _grid.HandleShimRowClicked(r0, none); // plain click resets
            if (_grid.ShimSelectedItems.Count != 1 || !r0.IsSelected || r1.IsSelected || r2.IsSelected)
            {
                throw new InvalidOperationException("plain click did not reset to single selection");
            }

            _grid.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Extended;
        });

        Step("Auto column width sizes to content and aligns header+cells", () =>
        {
            // Force a rebuild then lay out so the Auto-width pass runs.
            _grid!.HandleShimHeaderClicked(_grid.Columns[2]); // sort City → rebuild
            _grid.UpdateLayout();
            _grid.UpdateLayout();

            var host = FindDescendant(_grid, "PART_ShimRowsHost") as Microsoft.UI.Xaml.Controls.Panel;
            var headerPanel = host is null ? null : VisualTreeHelper.GetChild(host, 0);
            var nameHeader = headerPanel is null ? null : VisualTreeHelper.GetChild(headerPanel, 0) as FrameworkElement;
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var nameCell = row0?.TryGetCell(0);

            if (nameHeader is null || nameCell is null)
            {
                throw new InvalidOperationException("could not resolve Name header/cell");
            }

            Console.WriteLine($"[probe]   Name header width={nameHeader.Width}, cell width={nameCell.Width}");
            if (double.IsNaN(nameHeader.Width) || nameHeader.Width <= 0
                || Math.Abs(nameHeader.Width - nameCell.Width) > 0.5)
            {
                throw new InvalidOperationException(
                    $"Auto width not applied/aligned (header={nameHeader.Width}, cell={nameCell.Width})");
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

    // Ages of the items in current display order (via the generator).
    private List<int> RowAges()
    {
        var gen = _grid!.ItemContainerGenerator;
        var ages = new List<int>();
        for (var i = 0; ; i++)
        {
            if (gen.ContainerFromIndex(i) is not WpfDataGridRow row || row.Item is not Person p)
            {
                break;
            }

            ages.Add(p.Age);
        }

        return ages;
    }

    private static bool IsSorted(List<int> values, bool ascending)
    {
        for (var i = 1; i < values.Count; i++)
        {
            if (ascending && values[i] < values[i - 1])
            {
                return false;
            }

            if (!ascending && values[i] > values[i - 1])
            {
                return false;
            }
        }

        return values.Count > 1;
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
