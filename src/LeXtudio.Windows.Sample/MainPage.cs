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

    // Row-level rule: a person must be at least 18 (distinct from the
    // cell-level 0..150 IDataErrorInfo check).
    private sealed class MinAgeRule : System.Windows.Controls.ValidationRule
    {
        public override System.Windows.Controls.ValidationResult Validate(
            object value, System.Globalization.CultureInfo cultureInfo)
            => value is Person { Age: >= 18 }
                ? System.Windows.Controls.ValidationResult.ValidResult
                : new System.Windows.Controls.ValidationResult(false, "Age must be at least 18");
    }

    public sealed class Person : System.ComponentModel.IDataErrorInfo, System.ComponentModel.IEditableObject
    {
        private int _ageBackup;

        public Person(string name, int age, string city, bool isActive = false)
        {
            Name = name;
            Age = age;
            City = city;
            IsActive = isActive;
        }

        public string Name { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public bool IsActive { get; set; }

        // IEditableObject transaction tracking (for the probe).
        public bool InEdit { get; private set; }
        public int EndEditCount { get; private set; }
        public int CancelEditCount { get; private set; }

        public void BeginEdit()
        {
            if (!InEdit)
            {
                InEdit = true;
                _ageBackup = Age;
            }
        }

        public void EndEdit()
        {
            if (InEdit)
            {
                InEdit = false;
                EndEditCount++;
            }
        }

        public void CancelEdit()
        {
            if (InEdit)
            {
                Age = _ageBackup;
                InEdit = false;
                CancelEditCount++;
            }
        }

        public string Error => string.Empty;

        public string this[string columnName]
            => columnName == nameof(Age) && (Age < 0 || Age > 150)
                ? "Age must be between 0 and 150"
                : string.Empty;
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
            grid.Columns.Add(new System.Windows.Controls.DataGridCheckBoxColumn
            {
                Header = "Active",
                Binding = new WpfBinding("IsActive"),
            });
            grid.Columns.Add(new System.Windows.Controls.DataGridComboBoxColumn
            {
                Header = "CityPick",
                SelectedValueBinding = new WpfBinding("City"),
                ItemsSource = new[] { "London", "Arlington", "Seattle", "Hampton", "Town", "Paris" },
            });
            if (grid.Columns.Count != 5)
            {
                throw new InvalidOperationException($"expected 5 columns, found {grid.Columns.Count}");
            }

            grid.RowValidationRules.Add(new MinAgeRule());
            // Column headers only by default; the row-header step toggles to All.
            grid.HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.Column;
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
            if (cellCount != 5)
            {
                throw new InvalidOperationException($"expected 5 cells (one per column), found {cellCount}");
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

        Step("display-index changes reorder headers and cells", () =>
        {
            var nameColumn = _grid!.Columns[0];
            var ageColumn = _grid.Columns[1];
            var cityColumn = _grid.Columns[2];

            ageColumn.DisplayIndex = 0;
            nameColumn.DisplayIndex = 1;
            cityColumn.DisplayIndex = 2;
            _grid.UpdateLayout();

            var rebuiltHost = FindDescendant(_grid, "PART_ShimRowsHost") as Microsoft.UI.Xaml.Controls.Panel;
            var header = rebuiltHost is null ? null : VisualTreeHelper.GetChild(rebuiltHost, 0);
            var firstHeader = header is null ? null : VisualTreeHelper.GetChild(header, 0)
                as System.Windows.Controls.Primitives.DataGridColumnHeader;
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var firstCellColumn = row0?.TryGetCell(0)?.Column;

            Console.WriteLine($"[probe]   display[0]={_grid.ColumnFromDisplayIndex(0).Header}, header0={firstHeader?.Column?.Header}, cell0={firstCellColumn?.Header}");
            if (!ReferenceEquals(_grid.ColumnFromDisplayIndex(0), ageColumn) ||
                !ReferenceEquals(firstHeader?.Column, ageColumn) ||
                !ReferenceEquals(firstCellColumn, ageColumn))
            {
                throw new InvalidOperationException("DisplayIndex=0 did not move Age to the first realized column");
            }

            nameColumn.DisplayIndex = 0;
            ageColumn.DisplayIndex = 1;
            cityColumn.DisplayIndex = 2;
            _grid.UpdateLayout();
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

            // Reuse evidence: the real WPF PerformSort/DefaultSort path ran —
            // Items.SortDescriptions populated and the column direction set by it.
            Console.WriteLine($"[probe]   SortDescriptions={_grid.Items.SortDescriptions.Count}, dir={ageColumn.SortDirection}");
            if (_grid.Items.SortDescriptions.Count != 1
                || _grid.Items.SortDescriptions[0].PropertyName != "Age"
                || ageColumn.SortDirection != System.ComponentModel.ListSortDirection.Ascending)
            {
                throw new InvalidOperationException("WPF sort path did not populate SortDescriptions / direction");
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

            // CurrentCell / SelectedCells reflect the linked cell-selection engine.
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
            if (!_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true))
            {
                throw new InvalidOperationException("grid.CommitEdit(Row) failed");
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
            Console.WriteLine($"[probe]   after Ctrl: count={_grid.SelectedItems.Count}, r0={r0.IsSelected}, r1={r1.IsSelected}");
            if (_grid.SelectedItems.Count != 2 || !r0.IsSelected || !r1.IsSelected)
            {
                throw new InvalidOperationException("Ctrl did not add to the selection");
            }

            _grid.HandleShimRowClicked(r2, shift); // anchor r1 → range r1..r2
            Console.WriteLine($"[probe]   after Shift: count={_grid.SelectedItems.Count}, r0={r0.IsSelected}, r1={r1.IsSelected}, r2={r2.IsSelected}");
            if (_grid.SelectedItems.Count != 2 || r0.IsSelected || !r1.IsSelected || !r2.IsSelected)
            {
                throw new InvalidOperationException("Shift range incorrect");
            }

            _grid.HandleShimRowClicked(r0, none); // plain click resets
            if (_grid.SelectedItems.Count != 1 || !r0.IsSelected || r1.IsSelected || r2.IsSelected)
            {
                throw new InvalidOperationException("plain click did not reset to single selection");
            }

            _grid.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Extended;
        });

        Step("real SelectedItems + SelectionChanged are driven by the Selector engine", () =>
        {
            _grid!.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Extended;
            while (_grid.Items.Count < 3)
            {
                _grid.Items.Add(new Person($"S{_grid.Items.Count}", 20 + _grid.Items.Count, "Town"));
            }
            _grid.UpdateLayout();

            var added = 0;
            var removed = 0;
            var fired = 0;
            void OnSel(object? s, System.Windows.Controls.SelectionChangedEventArgs e)
            {
                fired++;
                added += e.AddedItems.Count;
                removed += e.RemovedItems.Count;
            }
            _grid.SelectionChanged += OnSel;

            var gen = _grid.ItemContainerGenerator;
            var r0 = gen.ContainerFromIndex(0) as WpfDataGridRow;
            var r1 = gen.ContainerFromIndex(1) as WpfDataGridRow;

            const global::Windows.System.VirtualKeyModifiers none = global::Windows.System.VirtualKeyModifiers.None;
            const global::Windows.System.VirtualKeyModifiers ctrl = global::Windows.System.VirtualKeyModifiers.Control;

            _grid.HandleShimRowClicked(r0!, none); // baseline: select just r0
            added = removed = fired = 0; // measure from a known [r0] baseline

            _grid.HandleShimRowClicked(r1!, ctrl); // add r1 → one net add, one event

            // The real WPF SelectedItems collection (Selector/MultiSelector) now
            // reflects the selection, and SelectionChanged fires on net change.
            Console.WriteLine($"[probe]   SelectedItems={_grid.SelectedItems.Count}, SelectionChanged fired={fired}, added={added}, removed={removed}, SelectedItem set={_grid.SelectedItem is not null}");
            if (_grid.SelectedItems.Count != 2
                || !_grid.SelectedItems.Contains(r0!.Item) || !_grid.SelectedItems.Contains(r1!.Item)
                || fired != 1 || added != 1 || removed != 0 || _grid.SelectedItem is null)
            {
                throw new InvalidOperationException(
                    $"real SelectedItems/SelectionChanged not driven (count={_grid.SelectedItems.Count}, fired={fired}, added={added})");
            }

            // Plain click collapses to a single selection — engine removes the other.
            removed = 0;
            _grid.HandleShimRowClicked(r0!, none);
            Console.WriteLine($"[probe]   after plain click: SelectedItems={_grid.SelectedItems.Count}, removed={removed}");
            if (_grid.SelectedItems.Count != 1 || !_grid.SelectedItems.Contains(r0!.Item) || removed < 1)
            {
                throw new InvalidOperationException("engine did not collapse SelectedItems to one on plain click");
            }

            _grid.SelectionChanged -= OnSel;
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

        Step("Star column expands to fill; MinWidth clamps", () =>
        {
            // City → Star, with a MinWidth floor; Name stays Auto.
            _grid!.Columns[2].Width = new System.Windows.Controls.DataGridLength(
                1, System.Windows.Controls.DataGridLengthUnitType.Star);
            _grid.Columns[2].MinWidth = 80;
            _grid.HandleShimHeaderClicked(_grid.Columns[0]); // rebuild
            _grid.UpdateLayout();
            _grid.UpdateLayout();

            var host = FindDescendant(_grid, "PART_ShimRowsHost") as Microsoft.UI.Xaml.Controls.Panel;
            var headerPanel = host is null ? null : VisualTreeHelper.GetChild(host, 0);
            var nameHeader = headerPanel is null ? null : VisualTreeHelper.GetChild(headerPanel, 0) as FrameworkElement;
            var cityHeader = headerPanel is null ? null : VisualTreeHelper.GetChild(headerPanel, 2) as FrameworkElement;
            if (nameHeader is null || cityHeader is null)
            {
                throw new InvalidOperationException("could not resolve headers for star test");
            }

            Console.WriteLine($"[probe]   Name(auto)={nameHeader.Width}, City(star)={cityHeader.Width}, grid={_grid.ActualWidth}");
            // Star column should consume remaining space → much wider than the
            // auto Name column, and at least its 80px MinWidth.
            if (cityHeader.Width < 80 || cityHeader.Width <= nameHeader.Width)
            {
                throw new InvalidOperationException(
                    $"Star did not expand (City={cityHeader.Width}, Name={nameHeader.Width})");
            }
        });

        Step("editing: read-only coercion + cancelable edit events", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var cell = row0?.TryGetCell(1); // Age
            var ageCol = _grid.Columns[1];
            if (cell is null)
            {
                throw new InvalidOperationException("no Age cell");
            }

            // DataGrid.IsReadOnly blocks editing.
            _grid.IsReadOnly = true;
            if (cell.BeginEdit(null)) throw new InvalidOperationException("grid read-only should block edit");
            _grid.IsReadOnly = false;

            // Column.IsReadOnly blocks editing.
            ageCol.IsReadOnly = true;
            if (cell.BeginEdit(null)) throw new InvalidOperationException("column read-only should block edit");
            ageCol.IsReadOnly = false;

            // BeginningEdit cancellation.
            void Begin(object? s, System.Windows.Controls.DataGridBeginningEditEventArgs e) => e.Cancel = true;
            _grid.BeginningEdit += Begin;
            if (cell.BeginEdit(null)) throw new InvalidOperationException("BeginningEdit cancel should block");
            _grid.BeginningEdit -= Begin;

            var beginning = 0;
            void CountBeginning(object? s, System.Windows.Controls.DataGridBeginningEditEventArgs e) => beginning++;

            // CellEditEnding veto keeps editing and discards the change.
            _grid.BeginningEdit += CountBeginning;
            if (!cell.BeginEdit(null)) throw new InvalidOperationException("begin should succeed");
            _grid.BeginningEdit -= CountBeginning;
            ((TextBox)cell.Content).Text = "123";
            void Ending(object? s, System.Windows.Controls.DataGridCellEditEndingEventArgs e) => e.Cancel = true;
            _grid.CellEditEnding += Ending;
            var cellEnding = 0;
            var rowEnding = 0;
            void CountEnding(object? s, System.Windows.Controls.DataGridCellEditEndingEventArgs e) => cellEnding++;
            void CountRowEnding(object? s, System.Windows.Controls.DataGridRowEditEndingEventArgs e) => rowEnding++;
            _grid.CellEditEnding += CountEnding;
            _grid.RowEditEnding += CountRowEnding;
            if (_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true)) throw new InvalidOperationException("commit should be vetoed");
            if (!cell.IsEditing) throw new InvalidOperationException("should remain editing after veto");
            _grid.CellEditEnding -= Ending;
            _grid.CellEditEnding -= CountEnding;
            _grid.RowEditEnding -= CountRowEnding;
            if (beginning != 1 || cellEnding != 1 || rowEnding != 0)
            {
                throw new InvalidOperationException($"unexpected event counts after veto (begin={beginning}, cell={cellEnding}, row={rowEnding})");
            }

            // Now the commit succeeds and writes back.
            cellEnding = 0;
            rowEnding = 0;
            _grid.CellEditEnding += CountEnding;
            _grid.RowEditEnding += CountRowEnding;
            if (!_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true)) throw new InvalidOperationException("commit should succeed");
            _grid.CellEditEnding -= CountEnding;
            _grid.RowEditEnding -= CountRowEnding;
            var age = ((Person)row0!.Item!).Age;
            Console.WriteLine($"[probe]   after vetoed+committed edit: Age={age}, IsEditing={cell.IsEditing}, CellEditEnding={cellEnding}, RowEditEnding={rowEnding}");
            if (age != 123 || cell.IsEditing || cellEnding != 1 || rowEnding != 1)
            {
                throw new InvalidOperationException($"edit not committed (Age={age})");
            }
        });

        Step("checkbox column renders + toggles write back", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var activeCell = row0?.TryGetCell(3); // Active (checkbox) column
            if (activeCell?.Content is not Microsoft.UI.Xaml.Controls.CheckBox box)
            {
                throw new InvalidOperationException(
                    $"Active cell content is {activeCell?.Content?.GetType().Name ?? "null"}, expected CheckBox");
            }

            var person = (Person)row0!.Item!;
            var before = person.IsActive;
            if (box.IsChecked != before)
            {
                throw new InvalidOperationException($"checkbox not bound (IsChecked={box.IsChecked}, IsActive={before})");
            }

            box.IsChecked = !before; // fires Checked/Unchecked → write-back
            Console.WriteLine($"[probe]   toggled IsActive {before} → {person.IsActive}");
            if (person.IsActive == before)
            {
                throw new InvalidOperationException("checkbox toggle did not write back");
            }
        });

        Step("combobox column renders + selection writes back", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var pickCell = row0?.TryGetCell(4); // CityPick (combo) column
            if (pickCell?.Content is not Microsoft.UI.Xaml.Controls.ComboBox combo)
            {
                throw new InvalidOperationException(
                    $"CityPick cell content is {pickCell?.Content?.GetType().Name ?? "null"}, expected ComboBox");
            }

            var person = (Person)row0!.Item!;
            combo.SelectedValue = "Paris"; // fires SelectionChanged → write-back
            Console.WriteLine($"[probe]   combo selected Paris → City={person.City}");
            if (person.City != "Paris")
            {
                throw new InvalidOperationException($"combo selection did not write back (City={person.City})");
            }
        });

        Step("validation: invalid edit keeps editing + flags cell, valid clears", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var ageCell = row0?.TryGetCell(1); // Age
            if (ageCell is null)
            {
                throw new InvalidOperationException("no Age cell");
            }

            // Invalid value (out of 0..150) → commit refused, cell flagged.
            if (!ageCell.BeginEdit(null)) throw new InvalidOperationException("begin failed");
            ((TextBox)ageCell.Content).Text = "999";
            if (_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true)) throw new InvalidOperationException("invalid commit should fail");
            Console.WriteLine($"[probe]   invalid: HasError={ageCell.HasValidationError}, IsEditing={ageCell.IsEditing}, msg={ageCell.ValidationError}");
            if (!ageCell.HasValidationError || !ageCell.IsEditing)
            {
                throw new InvalidOperationException("invalid edit should flag the cell and stay editing");
            }

            // Valid value → commits and clears the error.
            ((TextBox)ageCell.Content).Text = "42";
            if (!_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true)) throw new InvalidOperationException("valid commit should succeed");
            if (ageCell.HasValidationError || ((Person)row0!.Item!).Age != 42)
            {
                throw new InvalidOperationException("valid edit should clear error and write back");
            }
        });

        Step("row edit transaction: IEditableObject + RowEditEnding", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var ageCell = row0?.TryGetCell(1);
            var person = row0?.Item as Person;
            if (ageCell is null || person is null)
            {
                throw new InvalidOperationException("no Age cell / person");
            }

            var commits = 0;
            var cancels = 0;
            void OnRowEnding(object? s, System.Windows.Controls.DataGridRowEditEndingEventArgs e)
            {
                if (e.EditAction == System.Windows.Controls.DataGridEditAction.Commit) commits++;
                else cancels++;
            }
            _grid.RowEditEnding += OnRowEnding;
            var endBefore = person.EndEditCount;
            var cancelBefore = person.CancelEditCount;

            // Begin → transaction open.
            if (!ageCell.BeginEdit(null)) throw new InvalidOperationException("begin failed");
            if (!person.InEdit) throw new InvalidOperationException("IEditableObject.BeginEdit not called");

            // Commit → EndEdit + RowEditEnding(Commit), value written.
            ((TextBox)ageCell.Content).Text = "55";
            if (!_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true)) throw new InvalidOperationException("commit failed");
            if (person.InEdit || person.EndEditCount != endBefore + 1 || person.Age != 55 || commits != 1)
            {
                throw new InvalidOperationException($"commit transaction wrong (InEdit={person.InEdit}, EndEditΔ={person.EndEditCount - endBefore}, Age={person.Age}, commits={commits})");
            }

            // Begin again, then cancel → CancelEdit reverts the snapshot.
            if (!ageCell.BeginEdit(null)) throw new InvalidOperationException("begin2 failed");
            var cancelCellEnding = 0;
            var cancelRowEnding = 0;
            void CountCancelCellEnding(object? s, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
            {
                if (e.EditAction == System.Windows.Controls.DataGridEditAction.Cancel) cancelCellEnding++;
            }
            void CountCancelRowEnding(object? s, System.Windows.Controls.DataGridRowEditEndingEventArgs e)
            {
                if (e.EditAction == System.Windows.Controls.DataGridEditAction.Cancel) cancelRowEnding++;
            }
            _grid.CellEditEnding += CountCancelCellEnding;
            _grid.RowEditEnding += CountCancelRowEnding;
            ageCell.CancelEdit();
            _grid.CellEditEnding -= CountCancelCellEnding;
            _grid.RowEditEnding -= CountCancelRowEnding;
            Console.WriteLine($"[probe]   after cancel: InEdit={person.InEdit}, CancelEditΔ={person.CancelEditCount - cancelBefore}, cancels={cancels}, Age={person.Age}");
            if (person.InEdit || person.CancelEditCount != cancelBefore + 1 || cancels != 1
                || cancelCellEnding != 1 || cancelRowEnding != 1)
            {
                throw new InvalidOperationException("cancel transaction wrong");
            }

            _grid.RowEditEnding -= OnRowEnding;
        });

        Step("row validation rule flags the row and blocks commit", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var ageCell = row0?.TryGetCell(1);
            if (ageCell is null || row0 is null)
            {
                throw new InvalidOperationException("no Age cell");
            }

            // 10 passes the cell rule (0..150) but fails the row rule (>=18).
            if (!ageCell.BeginEdit(null)) throw new InvalidOperationException("begin failed");
            ((TextBox)ageCell.Content).Text = "10";
            if (_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true)) throw new InvalidOperationException("row rule should block commit");
            Console.WriteLine($"[probe]   row error after Age=10: {row0.HasRowValidationError} ({row0.RowValidationError})");
            if (!row0.HasRowValidationError || !ageCell.IsEditing)
            {
                throw new InvalidOperationException("row rule should flag the row and keep editing");
            }

            // 30 passes both → commits and clears the row error.
            ((TextBox)ageCell.Content).Text = "30";
            if (!_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true)) throw new InvalidOperationException("valid row commit should succeed");
            if (row0.HasRowValidationError || ((Person)row0.Item!).Age != 30)
            {
                throw new InvalidOperationException("valid commit should clear the row error");
            }
        });

        Step("row headers render with a current-row glyph (HeadersVisibility.All)", () =>
        {
            _grid!.HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.All;
            _grid.BuildShimVisualTree();
            _grid.UpdateLayout();

            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            if (row0 is null)
            {
                throw new InvalidOperationException("no row0");
            }

            _grid.HandleShimRowClicked(row0); // select → ▶ glyph

            var headerHost = FindDescendant(row0, "PART_RowHeader") as Microsoft.UI.Xaml.Controls.ContentControl;
            if (headerHost?.Content is not System.Windows.Controls.Primitives.DataGridRowHeader rowHeader)
            {
                throw new InvalidOperationException(
                    $"PART_RowHeader content is {headerHost?.Content?.GetType().Name ?? "null"}, expected DataGridRowHeader");
            }

            Console.WriteLine($"[probe]   row0 header glyph = '{rowHeader.Content}', cell0 col matches = {ReferenceEquals(row0.TryGetCell(0)?.Column, _grid.Columns[0])}");
            if (rowHeader.Content as string != "▶")
            {
                throw new InvalidOperationException($"expected current-row glyph, got '{rowHeader.Content}'");
            }

            // Cells stay column-indexed (row header is a separate element).
            if (!ReferenceEquals(row0.TryGetCell(0)?.Column, _grid.Columns[0]))
            {
                throw new InvalidOperationException("row header shifted the cell indices");
            }

            _grid.HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.Column;
        });

        Step("command routing: class-scoped command reaches a descendant cell", () =>
        {
            _grid!.UpdateLayout();
            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            var cell = row0?.TryGetCell(0);
            if (cell is null)
            {
                throw new InvalidOperationException("no cell to target");
            }

            // A command class-bound to DataGrid, executed against a cell that is
            // a visual descendant of the grid, must route to the handler.
            var ran = false;
            var cmd = new System.Windows.Input.RoutedCommand("probeRoute", typeof(WpfDataGrid));
            System.Windows.Input.CommandManager.RegisterClassCommandBinding(
                typeof(WpfDataGrid),
                new System.Windows.Input.CommandBinding(cmd,
                    (_, e) => { ran = true; e.Handled = true; }));

            cmd.Execute(null, cell);
            Console.WriteLine($"[probe]   class command routed to descendant cell = {ran}");
            if (!ran)
            {
                throw new InvalidOperationException("class-scoped command did not route to the descendant cell");
            }
        });

        Step("add-new row: placeholder edit enters routed WPF add-new path", () =>
        {
            _grid!.CanUserAddRows = true;
            _grid.UpdateLayout();
            var rowsBefore = _grid.Items.Count;
            Console.WriteLine($"[probe]   add-new setup: CanUserAddRows={_grid.CanUserAddRows}, PlaceholderPos={_grid.Items.NewItemPlaceholderPosition}, Items={rowsBefore}");
            if (_grid.Items.NewItemPlaceholderPosition != System.ComponentModel.NewItemPlaceholderPosition.AtEnd
                || rowsBefore < 1)
            {
                throw new InvalidOperationException("placeholder row was not surfaced when CanUserAddRows became true");
            }

            var adding = 0;
            var initializing = 0;
            void OnAdding(object? s, System.Windows.Controls.AddingNewItemEventArgs e)
            {
                adding++;
                e.NewItem = new Person("New", 22, "Town");
            }
            void OnInitializing(object? s, System.Windows.Controls.InitializingNewItemEventArgs e) => initializing++;
            _grid.AddingNewItem += OnAdding;
            _grid.InitializingNewItem += OnInitializing;

            var placeholderRow = _grid.ItemContainerGenerator.ContainerFromIndex(rowsBefore - 1) as WpfDataGridRow;
            var placeholderCell = placeholderRow?.TryGetCell(1);
            if (placeholderCell is null)
            {
                throw new InvalidOperationException("placeholder row/cell not realized");
            }

            if (!placeholderCell.BeginEdit(null))
            {
                throw new InvalidOperationException("placeholder BeginEdit failed");
            }

            _grid.UpdateLayout();
            if (!_grid.Items.IsAddingNew || _grid.Items.CurrentAddItem is not Person person)
            {
                throw new InvalidOperationException("placeholder begin did not start an add transaction");
            }

            if (adding != 1 || initializing != 1)
            {
                throw new InvalidOperationException($"unexpected add-new event counts (adding={adding}, initializing={initializing})");
            }

            var newRow = _grid.ItemContainerGenerator.ContainerFromItem(person) as WpfDataGridRow;
            var ageCell = newRow?.TryGetCell(1);
            if (ageCell?.Content is not TextBox box)
            {
                throw new InvalidOperationException("new row/cell was not realized after placeholder begin");
            }

            box.Text = "28";
            person.Age = 28;
            if (!_grid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true))
            {
                throw new InvalidOperationException("placeholder-created row commit failed");
            }

            _grid.UpdateLayout();
            Console.WriteLine($"[probe]   add-new: AddingNewItem={adding}, InitializingNewItem={initializing}, Items={_grid.Items.Count}, CurrentAddItem={_grid.Items.CurrentAddItem is null}, Age={person.Age}");
            if (_grid.Items.IsAddingNew || _grid.Items.Count != rowsBefore + 1 || person.Age != 28)
            {
                throw new InvalidOperationException("routed add-new flow did not keep the item and restore the placeholder");
            }

            _grid.AddingNewItem -= OnAdding;
            _grid.InitializingNewItem -= OnInitializing;
            _grid.CanUserAddRows = false;
        });

        Step("row details: template expands per RowDetailsVisibilityMode + selection", () =>
        {
            // A details template that shows the person's city.
            var template = (Microsoft.UI.Xaml.DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
                "<TextBlock x:Name='PART_DetailText' Text='{Binding City}' />" +
                "</DataTemplate>");
            _grid!.RowDetailsTemplate = template;

            var loading = 0;
            var unloading = 0;
            var visChanged = 0;
            void OnLoading(object? s, System.Windows.Controls.DataGridRowDetailsEventArgs e) => loading++;
            void OnUnloading(object? s, System.Windows.Controls.DataGridRowDetailsEventArgs e) => unloading++;
            void OnVisChanged(object? s, System.Windows.Controls.DataGridRowDetailsEventArgs e) => visChanged++;
            _grid.LoadingRowDetails += OnLoading;
            _grid.UnloadingRowDetails += OnUnloading;
            _grid.RowDetailsVisibilityChanged += OnVisChanged;

            // Mode = Visible → every real row's details host expands now.
            _grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.Visible;
            _grid.BuildShimVisualTree();
            _grid.UpdateLayout();

            var row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            row0!.ApplyTemplate();
            var detailsHost = FindDescendant(row0, "PART_DetailsHost") as Microsoft.UI.Xaml.Controls.ContentControl;
            var detailText = FindDescendant(row0, "PART_DetailText") as TextBlock;
            Console.WriteLine($"[probe]   Visible mode: host.Visibility={detailsHost?.Visibility}, text='{detailText?.Text}', loading={loading}");
            if (detailsHost?.Visibility != Visibility.Visible || detailText is null
                || detailText.Text != ((Person)row0.Item!).City || loading < 1)
            {
                throw new InvalidOperationException(
                    $"Visible mode did not expand details (vis={detailsHost?.Visibility}, text='{detailText?.Text}', loading={loading})");
            }

            // Mode = VisibleWhenSelected → details collapse until the row is selected.
            // Move selection to row1 so row0 is unselected for the precondition.
            _grid.SelectionMode = System.Windows.Controls.DataGridSelectionMode.Single;
            var row1 = _grid.ItemContainerGenerator.ContainerFromIndex(1) as WpfDataGridRow;
            _grid.HandleShimRowClicked(row1!);
            loading = 0;
            _grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
            _grid.BuildShimVisualTree();
            _grid.UpdateLayout();
            row0 = _grid.ItemContainerGenerator.ContainerFromIndex(0) as WpfDataGridRow;
            row0!.ApplyTemplate();
            detailsHost = FindDescendant(row0, "PART_DetailsHost") as Microsoft.UI.Xaml.Controls.ContentControl;
            if (detailsHost?.Visibility != Visibility.Collapsed)
            {
                throw new InvalidOperationException(
                    $"VisibleWhenSelected should collapse details for the unselected row (vis={detailsHost?.Visibility})");
            }

            visChanged = 0;
            _grid.HandleShimRowClicked(row0); // select → details expand
            detailsHost = FindDescendant(row0, "PART_DetailsHost") as Microsoft.UI.Xaml.Controls.ContentControl;
            Console.WriteLine($"[probe]   after select: host.Visibility={detailsHost?.Visibility}, visChanged={visChanged}");
            if (detailsHost?.Visibility != Visibility.Visible || visChanged < 1)
            {
                throw new InvalidOperationException(
                    $"selecting the row should expand details (vis={detailsHost?.Visibility}, visChanged={visChanged})");
            }

            _grid.LoadingRowDetails -= OnLoading;
            _grid.UnloadingRowDetails -= OnUnloading;
            _grid.RowDetailsVisibilityChanged -= OnVisChanged;
            _grid.RowDetailsTemplate = null;
            _grid.RowDetailsVisibilityMode = System.Windows.Controls.DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
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
