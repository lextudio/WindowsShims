using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// Uno-specific additions to the WPF DataGrid control root. Only members that
// do NOT appear in the linked upstream DataGrid.cs should live here. The
// upstream file is compiled as a partial on HAS_UNO so both parts merge.
public partial class DataGrid
{
    // Session 52: when the linked WPF CommitEdit command calls back into the
    // current cell, let the cell run only its local value-write/validation/end
    // logic instead of re-entering DataGrid.CommitEdit().
    internal bool ShimExecutingCommitEditCommand { get; set; }

    // Same idea for the linked WPF CancelEdit command path.
    internal bool ShimExecutingCancelEditCommand { get; set; }

    // Same idea for the linked WPF BeginEdit command path.
    internal bool ShimExecutingBeginEditCommand { get; set; }

    internal bool ShimHandlingPlaceholderBeginEdit { get; set; }

    internal bool ShimValidateRowCommit(DataGridRow? row)
    {
        if (row is null)
        {
            return true;
        }

        foreach (var rule in RowValidationRules)
        {
            var result = rule.Validate(row.Item, System.Globalization.CultureInfo.CurrentCulture);
            if (!result.IsValid)
            {
                row.SetRowError(result.ErrorContent?.ToString());
                return false;
            }
        }

        row.ClearRowError();
        return true;
    }

    // UpdateVisualState: the upstream calls this (0-arg) which calls the
    // virtual ChangeVisualState. Provide the 0-arg overload in the shim part.
    internal void UpdateVisualState() => ChangeVisualState(true);

    // The shim render path is code-built rather than presenter-driven, so
    // option changes like CanUserAddRows need an explicit rebuild before the
    // normal layout pass if the visual tree is already realized.
    public new void UpdateLayout()
    {
        BuildShimVisualTree();
        base.UpdateLayout();
    }

    internal bool ShimBeginEditPlaceholder(DataGridCell placeholderCell, RoutedEventArgs? editingEventArgs)
    {
        if (ShimHandlingPlaceholderBeginEdit)
        {
            return false;
        }

        ShimHandlingPlaceholderBeginEdit = true;
        try
        {
            var placeholderColumn = placeholderCell.Column ?? Columns.Cast<DataGridColumn>().FirstOrDefault(column => column.IsVisible);
            if (placeholderColumn is null)
            {
                return false;
            }

            var newItem = Items.CurrentAddItem ?? AddNewItem();
            SetCurrentCellToNewItem(newItem, placeholderColumn);
            UpdateLayout();

            DataGridCell? newCell = null;
            var row =
                ItemContainerGenerator.ContainerFromItem(newItem) as DataGridRow ??
                FindShimRowForItem(newItem);

            row?.UpdateLayout();

            if (row is not null && placeholderCell.Column is { } column)
            {
                for (var i = 0; i < Columns.Count; i++)
                {
                    if (ReferenceEquals(Columns[i], column))
                    {
                        newCell = row.TryGetCell(i);
                        break;
                    }
                }
            }

            newCell ??= CurrentCellContainer;
            if (newCell is not null && !ReferenceEquals(newCell, placeholderCell))
            {
                CurrentCellContainer = newCell;
                var wasExecuting = ShimExecutingBeginEditCommand;
                ShimExecutingBeginEditCommand = true;
                try
                {
                    return newCell.BeginEdit(editingEventArgs);
                }
                finally
                {
                    ShimExecutingBeginEditCommand = wasExecuting;
                }
            }

            return false;
        }
        finally
        {
            ShimHandlingPlaceholderBeginEdit = false;
        }
    }

    private void SetCurrentCellToNewItem(object newItem, DataGridColumn fallbackColumn)
    {
        var column = CurrentCell.Column ?? fallbackColumn;
        var index = Items.IndexOf(newItem);
        var info = ItemInfoFromIndex(index);
        CurrentCell = info is not null
            ? new DataGridCellInfo(info, column, this)
            : new DataGridCellInfo(newItem, column, this);
    }

    private DataGridRow? FindShimRowForItem(object item)
    {
        if (GetTemplateChild("PART_ShimRowsHost") is not Microsoft.UI.Xaml.Controls.Panel host)
        {
            return null;
        }

        foreach (var child in host.Children)
        {
            if (child is DataGridRow row && ReferenceEquals(row.Item, item))
            {
                return row;
            }
        }

        return null;
    }

    // The WPF static-ctor OverrideMetadata(typeof(DataGrid)) call is a no-op
    // under the shim, and the library's Themes/Generic.xaml is not reliably in
    // the consumer's ms-appx resource map. So the shim assigns a minimal
    // ControlTemplate directly (built via XamlReader) — self-contained, no
    // dependency on default-style probing. The template root hosts
    // PART_ShimRowsHost, which the code render path populates.
    private const string ShimTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='White' BorderBrush='#CCCCCC' BorderThickness='1'>" +
        "<ScrollViewer HorizontalScrollBarVisibility='Auto' VerticalScrollBarVisibility='Auto'>" +
        "<StackPanel x:Name='PART_ShimRowsHost' MinWidth='120' MinHeight='40' />" +
        "</ScrollViewer></Border></ControlTemplate>";

    private void EnsureShimStyleKey()
    {
        if (Template is not null)
        {
            return;
        }

        try
        {
            Template = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(ShimTemplateXaml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataGrid] shim template load failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Session 25/26 — shim render path. Populates PART_ShimRowsHost with a
    // header row plus one DataGridRow per item. Each DataGridRow hosts its own
    // cells (session 26), so the on-screen tree matches the WPF row/cell APIs.
    // Still intentionally simple: no virtualization, no ItemContainerGenerator
    // containers, separate from the upstream PART_RowsPresenter machinery.
    internal void BuildShimVisualTree()
    {
        if (GetTemplateChild("PART_ShimRowsHost") is not Microsoft.UI.Xaml.Controls.Panel host)
        {
            return;
        }

        var editingItem = CurrentCellContainer is { IsEditing: true } editingCell
            ? editingCell.RowDataItem
            : null;
        var editingColumn = CurrentCellContainer is { IsEditing: true } ? CurrentCellContainer.Column : null;

        EnsureShimNewItemPlaceholderState();
        HookShimChangeNotifications();

        InternalColumns.RefreshDisplayIndexMap();
        _visibleColumns = ColumnsInDisplayOrder().Where(c => c.IsVisible).ToList();

        host.Children.Clear();
        ItemContainerGenerator.ResetContainers();
        // Reset the tracker chain before rebuilding so the upstream
        // DataGrid.NotifyPropertyChanged → _rowTrackingRoot iteration always
        // reflects the live set of realized rows. (Partial class shares the
        // private _rowTrackingRoot field with the upstream DataGrid.cs.)
        _rowTrackingRoot = null;
        host.Children.Add(BuildHeaderRow());
        if (DataGridExtensions.DataGridFilter.GetIsAutoFilterEnabled(this))
            host.Children.Add(BuildFilterRow());

        var rowIndex = 0;
        foreach (var item in OrderedItems())
        {
            if (item is null)
            {
                continue;
            }

            var row = new DataGridRow();
            row.PrepareRow(item, this); // also initializes row.Tracker
            row.ShimRowIndex = rowIndex++;
            // Default row separator (1px bottom border). Overridden to red by SetRowError.
            if (!row.HasRowValidationError)
            {
                row.BorderBrush = DataGridRow.SeparatorBrush;
                row.BorderThickness = DataGridRow.SeparatorThickness;
            }
            row.ApplyShimRowStyle();
            row.ApplyShimRowBackground();
            row.Tracker!.StartTracking(ref _rowTrackingRoot);
            // Re-apply selection across rebuilds (sort / reactivity) from the
            // real engine's SelectedItems, so the highlight follows the item(s).
            if (IsRowItemSelected(item))
            {
                row.IsSelected = true;
            }

            // Register the row so the linked WPF code can resolve containers
            // (selection, scroll-into-view, row details) via the generator.
            ItemContainerGenerator.RegisterContainer(item, row);
            host.Children.Add(row);
        }

        PruneRealRowSelection();
        PruneRealCellSelection();

        ItemContainerGenerator.NotifyContainersGenerated();

        if (editingItem is not null && editingColumn is not null)
        {
            RestoreEditingCellAfterRebuild(editingItem, editingColumn);
        }

        // Schedule an Auto-width pass if any visible column is non-absolute.
        if (_visibleColumns.Any(IsAutoWidth))
        {
            _autoWidthPending = true;
            if (!_autoWidthHooked)
            {
                LayoutUpdated += OnAutoWidthLayoutUpdated;
                _autoWidthHooked = true;
            }
        }
    }

    private void RestoreEditingCellAfterRebuild(object editingItem, DataGridColumn editingColumn)
    {
        if (ItemContainerGenerator.ContainerFromItem(editingItem) is not DataGridRow row)
        {
            return;
        }

        // Template may not be applied yet (OnApplyTemplate fires after the layout pass).
        // Force it now so TryGetCell can access the realized cells.
        row.ApplyTemplate();

        for (var i = 0; i < Columns.Count; i++)
        {
            if (!ReferenceEquals(Columns[i], editingColumn))
            {
                continue;
            }

            if (row.TryGetCell(i) is not { IsEditing: false } cell)
            {
                return;
            }

            CurrentCellContainer = cell;
            var wasExecuting = ShimExecutingBeginEditCommand;
            ShimExecutingBeginEditCommand = true;
            try
            {
                cell.BeginEdit(null);
            }
            finally
            {
                ShimExecutingBeginEditCommand = wasExecuting;
            }

            return;
        }
    }

    private void EnsureShimNewItemPlaceholderState()
    {
        if (Items.IsAddingNew)
        {
            return;
        }

        if (CanUserAddRows)
        {
            if (Items.NewItemPlaceholderPosition == System.ComponentModel.NewItemPlaceholderPosition.None)
            {
                Items.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.AtEnd;
            }
        }
        else if (Items.NewItemPlaceholderPosition != System.ComponentModel.NewItemPlaceholderPosition.None)
        {
            Items.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.None;
        }
    }

    // ── Session 41: Auto column width ────────────────────────────────────────
    // Absolute widths are honored directly (ShimColumnWidth). For Auto/Star/
    // SizeToCells/SizeToHeader, cells/headers are left auto-sized, then a
    // one-shot post-layout pass sets a uniform per-column width to the widest
    // realized content so columns size to content and stay aligned.
    private List<DataGridColumn> _visibleColumns = new();
    private readonly List<DataGridColumnHeader> _headerCells = new();
    // Filter row cells tracked in parallel with _headerCells so the auto-width
    // pass can synchronize their widths along with header and data cells.
    private readonly List<Microsoft.UI.Xaml.FrameworkElement> _filterCells = new();
    private bool _autoWidthPending;
    private bool _autoWidthHooked;

    // Non-absolute columns (Auto/SizeTo*/Star) need the post-layout pass.
    private static bool IsAutoWidth(DataGridColumn column)
        => column.ActualWidth <= 0 && !column.Width.IsAbsolute;

    private static double Clamp(DataGridColumn column, double width)
        => Math.Clamp(width, column.MinWidth, column.MaxWidth);

    private void OnAutoWidthLayoutUpdated(object? sender, object e)
    {
        if (!_autoWidthPending)
        {
            return;
        }

        _autoWidthPending = false;

        var rows = ItemContainerGenerator.Containers.OfType<DataGridRow>().ToList();
        var widths = new double[_visibleColumns.Count];
        var starWeights = new double[_visibleColumns.Count];
        var fixedTotal = 0.0;
        var totalStar = 0.0;

        // Pass 1: fixed (absolute) + auto (measured) widths, clamped.
        for (var i = 0; i < _visibleColumns.Count && i < _headerCells.Count; i++)
        {
            var column = _visibleColumns[i];
            if (column.Width.IsStar)
            {
                starWeights[i] = column.Width.Value > 0 ? column.Width.Value : 1;
                totalStar += starWeights[i];
                continue;
            }

            double w;
            if (!IsAutoWidth(column))
            {
                w = column.ActualWidth > 0 ? column.ActualWidth : column.Width.Value;
            }
            else
            {
                w = _headerCells[i].DesiredSize.Width;
                foreach (var row in rows)
                {
                    if (row.TryGetCell(i) is { } cell)
                    {
                        w = Math.Max(w, cell.DesiredSize.Width);
                    }
                }
            }

            widths[i] = Clamp(column, w);
            fixedTotal += widths[i];
        }

        // Pass 2: distribute remaining viewport width among star columns.
        if (totalStar > 0)
        {
            var available = ActualWidth - 2; // border chrome
            var remaining = Math.Max(0, available - fixedTotal);
            for (var i = 0; i < _visibleColumns.Count; i++)
            {
                if (starWeights[i] > 0)
                {
                    widths[i] = Clamp(_visibleColumns[i], remaining * (starWeights[i] / totalStar));
                }
            }
        }

        // Apply to header cells, data cells, and filter cells.
        for (var i = 0; i < _visibleColumns.Count && i < _headerCells.Count; i++)
        {
            if (widths[i] <= 0)
            {
                continue;
            }

            _headerCells[i].Width = widths[i];
            foreach (var row in rows)
            {
                if (row.TryGetCell(i) is { } cell)
                {
                    cell.Width = widths[i];
                }
            }
            if (i < _filterCells.Count)
                _filterCells[i].Width = widths[i];
        }
    }

    // Called when a single column's Width DP changes (e.g. after a gripper drag).
    // Applies the new width to the header cell, data cells, and filter cell for
    // that column without rebuilding the whole visual tree.
    private void ShimApplyColumnWidth(DataGridColumn column)
    {
        var i = _visibleColumns.IndexOf(column);
        if (i < 0 || i >= _headerCells.Count)
            return;

        var w = ShimColumnWidth(column);
        if (double.IsNaN(w) || w <= 0)
            return;

        w = Clamp(column, w);
        _headerCells[i].Width = w;
        foreach (var row in ItemContainerGenerator.Containers.OfType<DataGridRow>())
        {
            if (row.TryGetCell(i) is { } cell)
                cell.Width = w;
        }
        if (i < _filterCells.Count)
            _filterCells[i].Width = w;
    }

    // ── Column resize by header edge drag ────────────────────────────────────
    private DataGridColumn? _resizeColumn;
    private DataGridColumnHeader? _resizeHeader;
    private double _resizeLastX;
    private bool _resizeActive;
    private const double ResizeEdgeThickness = 6.0;
    private const double ResizeDragThreshold = 1.0;
    private enum HeaderResizeEdge
    {
        None,
        Left,
        Right
    }

    private bool TryBeginHeaderResize(DataGridColumnHeader header, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!CanUserResizeColumns)
        {
            return false;
        }

        var point = e.GetCurrentPoint(header);
        if (!point.Properties.IsLeftButtonPressed
            || ResolveHeaderResizeColumn(header, point.Position.X) is not { CanUserResize: true, IsVisible: true } column)
        {
            return false;
        }

        _resizeHeader = header;
        _resizeColumn = column;
        _resizeLastX = e.GetCurrentPoint(HeaderPointerHost(header)).Position.X;
        _resizeActive = false;
        header.CapturePointer(e.Pointer);
        e.Handled = true;
        return true;
    }

    private static bool IsOnHeaderResizeEdge(DataGridColumnHeader header, double x)
        => HeaderResizeEdgeAt(header, x) is not HeaderResizeEdge.None;

    private static HeaderResizeEdge HeaderResizeEdgeAt(DataGridColumnHeader header, double x)
    {
        var width = header.ActualWidth > 0 ? header.ActualWidth : header.Width;
        if (double.IsNaN(width) || width <= 0)
        {
            return HeaderResizeEdge.None;
        }

        if (x <= ResizeEdgeThickness)
        {
            return HeaderResizeEdge.Left;
        }

        return x >= Math.Max(0, width - ResizeEdgeThickness)
            ? HeaderResizeEdge.Right
            : HeaderResizeEdge.None;
    }

    private DataGridColumn? ResolveHeaderResizeColumn(DataGridColumnHeader header, double x)
    {
        return HeaderResizeEdgeAt(header, x) switch
        {
            HeaderResizeEdge.Right => header.Column,
            HeaderResizeEdge.Left => PreviousVisibleColumn(header.Column),
            _ => null
        };
    }

    private DataGridColumn? PreviousVisibleColumn(DataGridColumn? column)
    {
        if (column is null)
        {
            return null;
        }

        var index = _visibleColumns.IndexOf(column);
        if (index > 0)
        {
            return _visibleColumns[index - 1];
        }

        index = Columns.IndexOf(column);
        for (var i = index - 1; i >= 0; i--)
        {
            if (Columns[i].IsVisible)
            {
                return Columns[i];
            }
        }

        return null;
    }

    private bool ContinueHeaderResize(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_resizeColumn is null || _resizeHeader is null)
        {
            return false;
        }

        var x = e.GetCurrentPoint(HeaderPointerHost(_resizeHeader)).Position.X;
        var delta = x - _resizeLastX;
        if (Math.Abs(delta) < ResizeDragThreshold)
        {
            e.Handled = true;
            return true;
        }

        _resizeActive = ShimTryResizeColumn(_resizeColumn, delta) || _resizeActive;
        _resizeLastX = x;
        e.Handled = true;
        return true;
    }

    private bool EndHeaderResize(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_resizeHeader is null)
        {
            return false;
        }

        _resizeHeader.ReleasePointerCapture(e.Pointer);
        _resizeHeader = null;
        _resizeColumn = null;
        _resizeActive = false;
        e.Handled = true;
        return true;
    }

    private Microsoft.UI.Xaml.UIElement HeaderPointerHost(DataGridColumnHeader fallback)
        => _headerHostPanel is not null ? _headerHostPanel : fallback;

    // Core column-resize commit used by the shim header/gripper path. WPF's
    // full redistribution algorithm also adjusts neighboring/star columns; the
    // shim render path keeps the user-resized column as an explicit pixel width
    // and then synchronizes the realized header/filter/data cells.
    internal bool ShimTryResizeColumn(DataGridColumn column, double horizontalChange)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!CanUserResizeColumns || !column.CanUserResize || !column.IsVisible)
        {
            return false;
        }

        var currentWidth = ShimResizeBaseWidth(column);
        if (double.IsNaN(currentWidth) || currentWidth <= 0)
        {
            return false;
        }

        var resizedWidth = DataGridColumnResizeShim.ComputeWidth(currentWidth, horizontalChange, column.MinWidth, column.MaxWidth);
        if (Math.Abs(resizedWidth - currentWidth) < 0.5)
        {
            return false;
        }

        InternalColumns.OnColumnResizeStarted();
        try
        {
            column.Width = new DataGridLength(resizedWidth);
            ShimApplyColumnWidth(column);
        }
        finally
        {
            InternalColumns.OnColumnResizeCompleted(cancel: false);
        }

        return true;
    }

    internal bool ShimTryAutoSizeColumn(DataGridColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (!CanUserResizeColumns || !column.CanUserResize || !column.IsVisible)
        {
            return false;
        }

        var bestFitWidth = ShimBestFitColumnWidth(column);
        if (double.IsNaN(bestFitWidth) || bestFitWidth <= 0)
        {
            return false;
        }

        var currentWidth = ShimResizeBaseWidth(column);
        var resizedWidth = DataGridColumnResizeShim.ClampWidth(bestFitWidth, column.MinWidth, column.MaxWidth);
        if (!double.IsNaN(currentWidth) && Math.Abs(resizedWidth - currentWidth) < 0.5)
        {
            return false;
        }

        InternalColumns.OnColumnResizeStarted();
        try
        {
            column.Width = new DataGridLength(resizedWidth);
            ShimApplyColumnWidth(column);
        }
        finally
        {
            InternalColumns.OnColumnResizeCompleted(cancel: false);
        }

        return true;
    }

    private double ShimBestFitColumnWidth(DataGridColumn column)
    {
        var visibleIndex = _visibleColumns.IndexOf(column);
        if (visibleIndex < 0)
        {
            visibleIndex = Columns.IndexOf(column);
        }

        if (visibleIndex < 0 && column.DisplayIndex >= 0)
        {
            visibleIndex = column.DisplayIndex;
        }

        if (visibleIndex < 0)
        {
            return double.NaN;
        }

        var width = TextBestFitWidth(column.Header?.ToString());
        if (visibleIndex < _headerCells.Count)
        {
            width = Math.Max(width, ElementBestFitWidth(_headerCells[visibleIndex]));
        }

        if (visibleIndex < _filterCells.Count)
        {
            width = Math.Max(width, ElementBestFitWidth(_filterCells[visibleIndex]));
        }

        foreach (var row in ItemContainerGenerator.Containers.OfType<DataGridRow>())
        {
            if (row.TryGetCell(visibleIndex) is { } cell)
            {
                width = Math.Max(width, ElementBestFitWidth(cell));
            }
        }

        return width > 0 ? DataGridColumnResizeShim.ClampWidth(width, column.MinWidth, column.MaxWidth) : double.NaN;
    }

    private static double ElementBestFitWidth(Microsoft.UI.Xaml.FrameworkElement element)
    {
        var width = Math.Max(element.DesiredSize.Width, element.ActualWidth);
        if (double.IsNaN(width) || width <= 0)
        {
            width = element.Width;
        }

        return Math.Max(width, TextBestFitWidth(ElementText(element)));
    }

    private static string? ElementText(object? value)
    {
        return value switch
        {
            null => null,
            Microsoft.UI.Xaml.Controls.TextBlock textBlock => textBlock.Text,
            Microsoft.UI.Xaml.Controls.TextBox textBox => textBox.Text,
            ContentControl contentControl => ElementText(contentControl.Content),
            Microsoft.UI.Xaml.Controls.ContentControl contentControl => ElementText(contentControl.Content),
            _ => value.ToString()
        };
    }

    private static double TextBestFitWidth(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return Math.Clamp(text.Length * 7.0 + 24.0, 20.0, 800.0);
    }

    private double ShimResizeBaseWidth(DataGridColumn column)
    {
        if (column.ActualWidth > 0)
        {
            return column.ActualWidth;
        }

        var visibleIndex = _visibleColumns.IndexOf(column);
        if (visibleIndex >= 0 && visibleIndex < _headerCells.Count)
        {
            var headerWidth = _headerCells[visibleIndex].ActualWidth > 0
                ? _headerCells[visibleIndex].ActualWidth
                : _headerCells[visibleIndex].Width;
            if (!double.IsNaN(headerWidth) && headerWidth > 0)
            {
                return headerWidth;
            }
        }

        return column.Width.DisplayValue > 0 ? column.Width.DisplayValue : column.Width.Value;
    }

    // Items in display order, with active column filters applied. Sorting is
    // handled by ItemCollection.SortDescriptions (WPF PerformSort path).
    private IEnumerable<object?> OrderedItems()
        => Items.Cast<object?>()
                .Where(item => DataGridExtensions.DataGridFilter.MatchesAllFilters(this, item))
                .ToList();

    private Microsoft.UI.Xaml.Controls.Border BuildHeaderRow()
    {
        var header = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
        };

        // Top-left corner placeholder when row headers are visible, so column
        // headers line up with the row-header-indented cells.
        if (AreRowHeadersVisible)
        {
            header.Children.Add(new Microsoft.UI.Xaml.Controls.Border { Width = RowHeaderShimWidth });
        }

        _headerCells.Clear();
        foreach (var column in _visibleColumns)
        {
            var headerCell = new DataGridColumnHeader
            {
                Content = HeaderContent(column),
                Width = ShimColumnWidth(column),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Microsoft.UI.Xaml.Thickness(4, 2, 4, 2),
            };
            headerCell.PrepareColumnHeader(column.Header, column);
            headerCell.Content = HeaderContent(column);
            headerCell.ApplyShimFrozenState();
            headerCell.ApplyShimColumnHeaderStyle();
            headerCell.ApplyShimGridLines();
            headerCell.PointerPressed += OnHeaderPointerPressed;
            headerCell.PointerMoved += OnHeaderPointerMoved;
            headerCell.PointerReleased += OnHeaderPointerReleased;
            headerCell.PointerCaptureLost += OnHeaderPointerCaptureLost;
            headerCell.DoubleTapped += OnHeaderDoubleTapped;
            _headerCells.Add(headerCell);
            header.Children.Add(headerCell);
        }

        _headerHostPanel = header;
        // Wrap in a Border so the header row gets a bottom separator line.
        return new Microsoft.UI.Xaml.Controls.Border
        {
            BorderBrush = _rowSeparatorBrush,
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 2),
            Child = header,
        };
    }

    // Light gray brush shared by the filter row background and the row separator lines.
    private static readonly Microsoft.UI.Xaml.Media.Brush _filterRowBackground =
        new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0));
    private static readonly Microsoft.UI.Xaml.Media.Brush _rowSeparatorBrush =
        new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(0xFF, 0xD0, 0xD0, 0xD0));

    // Builds a filter row below the column headers when IsAutoFilterEnabled is true.
    // Each cell is a Text box, Hex box ("0x" prefix), or a Flags popup depending on
    // the FilterControlTemplate stored on the column by DataGridFilterColumn.SetTemplate.
    // Also clears/populates _filterCells so the auto-width pass keeps all rows aligned.
    private Microsoft.UI.Xaml.Controls.Border BuildFilterRow()
    {
        _filterCells.Clear();

        var innerRow = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
        };

        if (AreRowHeadersVisible)
            innerRow.Children.Add(new Microsoft.UI.Xaml.Controls.Border { Width = RowHeaderShimWidth });

        foreach (var column in _visibleColumns)
        {
            var kind = DataGridExtensions.FilterKind.Text;
            Type? flagsType = null;
            if (DataGridExtensions.DataGridFilterColumn.GetTemplate(column)
                    is DataGridExtensions.FilterControlTemplate fct)
            {
                kind = fct.Kind;
                flagsType = fct.FlagsType;
            }

            Microsoft.UI.Xaml.FrameworkElement cell = kind switch
            {
                DataGridExtensions.FilterKind.Hex   => BuildHexFilterCell(column),
                DataGridExtensions.FilterKind.Flags => BuildFlagsFilterCell(column, flagsType),
                _                                   => BuildTextFilterCell(column),
            };
            _filterCells.Add(cell);
            innerRow.Children.Add(cell);
        }

        // Wrap in a Border to provide a distinct background and a separator line below.
        return new Microsoft.UI.Xaml.Controls.Border
        {
            Background = _filterRowBackground,
            BorderBrush = _rowSeparatorBrush,
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 1),
            Child = innerRow,
        };
    }

    // Plain case-insensitive substring TextBox.
    private Microsoft.UI.Xaml.Controls.TextBox BuildTextFilterCell(DataGridColumn column)
    {
        var state = DataGridExtensions.DataGridFilter.GetState(this);
        var current = state.ColumnFilterText.TryGetValue(column, out var text)
            ? text
            : (state.ColumnFilters.TryGetValue(column, out var f)
               ? (f as DataGridExtensions.SubstringContentFilter)?.Text
               : null) ?? string.Empty;
        var box = new Microsoft.UI.Xaml.Controls.TextBox
        {
            Text = current,
            Width = ShimColumnWidth(column),
            PlaceholderText = "Filter…",
            Margin = new Microsoft.UI.Xaml.Thickness(4, 1, 4, 1),
        };
        box.TextChanged += (s, _) =>
        {
            var text = ((Microsoft.UI.Xaml.Controls.TextBox)s!).Text;
            var st = DataGridExtensions.DataGridFilter.GetState(this);
            if (string.IsNullOrEmpty(text))
                st.ColumnFilterText.Remove(column);
            else
                st.ColumnFilterText[column] = text;

            st.ColumnFilters[column] = string.IsNullOrEmpty(text)
                ? null
                : (st.ContentFilterFactory?.Create(text)
                   ?? new DataGridExtensions.SubstringContentFilter(text));
            BuildShimVisualTree();
        };
        return box;
    }

    // "0x" prefix + TextBox that matches via hex representation.
    private Microsoft.UI.Xaml.Controls.StackPanel BuildHexFilterCell(DataGridColumn column)
    {
        var state = DataGridExtensions.DataGridFilter.GetState(this);
        var current = (state.ColumnFilters.TryGetValue(column, out var f)
                       ? (f as DataGridExtensions.HexContentFilter)?.Text
                       : null) ?? string.Empty;
        var box = new Microsoft.UI.Xaml.Controls.TextBox
        {
            Text = current,
            MinWidth = 30,
            PlaceholderText = "hex…",
        };
        box.TextChanged += (s, _) =>
        {
            var text = ((Microsoft.UI.Xaml.Controls.TextBox)s!).Text;
            var st = DataGridExtensions.DataGridFilter.GetState(this);
            st.ColumnFilters[column] = string.IsNullOrEmpty(text)
                ? null
                : new DataGridExtensions.HexContentFilter(text);
            BuildShimVisualTree();
        };
        var row = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
            Width = ShimColumnWidth(column),
            Margin = new Microsoft.UI.Xaml.Thickness(4, 1, 4, 1),
        };
        row.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
        {
            Text = "0x",
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 2, 0),
        });
        row.Children.Add(box);
        return row;
    }

    // ToggleButton + Flyout + CheckBox list for each flag value.
    private Microsoft.UI.Xaml.Controls.Primitives.ToggleButton BuildFlagsFilterCell(DataGridColumn column, Type? flagsType)
    {
        var state = DataGridExtensions.DataGridFilter.GetState(this);
        var currentMask = (state.ColumnFilters.TryGetValue(column, out var f)
                           ? (f as DataGridExtensions.MaskContentFilter)?.Mask
                           : null) ?? -1;

        var toggle = new Microsoft.UI.Xaml.Controls.Primitives.ToggleButton
        {
            Content = currentMask == -1 ? "All" : "Filtered",
            Width = ShimColumnWidth(column),
            Margin = new Microsoft.UI.Xaml.Thickness(4, 1, 4, 1),
        };

        if (flagsType is null)
            return toggle;

        // Enumerate public static fields of the flags enum (skip *Mask fields).
        var flagItems = new System.Collections.Generic.List<(string Name, int Value)>();
        foreach (var field in flagsType.GetFields(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (field.Name.EndsWith("Mask", StringComparison.Ordinal)) continue;
            int v;
            try { v = Convert.ToInt32(field.GetRawConstantValue()); } catch { continue; }
            flagItems.Add((field.Name, v));
        }

        var flyoutContent = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            MaxHeight = 300,
            Spacing = 2,
        };

        // "All" / reset checkbox
        var allBox = new Microsoft.UI.Xaml.Controls.CheckBox
        {
            Content = "<All>",
            IsChecked = currentMask == -1,
        };
        flyoutContent.Children.Add(allBox);

        var perFlagBoxes = new System.Collections.Generic.List<(Microsoft.UI.Xaml.Controls.CheckBox cb, int val)>();
        foreach (var (name, val) in flagItems)
        {
            var cb = new Microsoft.UI.Xaml.Controls.CheckBox
            {
                Content = $"{name} ({val:X4})",
                IsChecked = currentMask == -1 || (currentMask & val) != 0,
                Tag = val,
            };
            perFlagBoxes.Add((cb, val));
            flyoutContent.Children.Add(cb);
        }

        void ApplyMask()
        {
            int mask = 0;
            bool anyChecked = false;
            foreach (var (cb, val) in perFlagBoxes)
            {
                if (cb.IsChecked == true) { mask |= val; anyChecked = true; }
            }
            var st = DataGridExtensions.DataGridFilter.GetState(this);
            if (!anyChecked || allBox.IsChecked == true)
            {
                st.ColumnFilters[column] = null;
                toggle.Content = "All";
                toggle.IsChecked = false;
            }
            else
            {
                st.ColumnFilters[column] = new DataGridExtensions.MaskContentFilter(mask);
                toggle.Content = "Filtered";
            }
            BuildShimVisualTree();
        }

        allBox.Checked += (_, _) =>
        {
            foreach (var (cb, _) in perFlagBoxes) cb.IsChecked = true;
            ApplyMask();
        };
        allBox.Unchecked += (_, _) =>
        {
            foreach (var (cb, _) in perFlagBoxes) cb.IsChecked = false;
            ApplyMask();
        };
        foreach (var (cb, _) in perFlagBoxes)
        {
            cb.Checked   += (_, _) => ApplyMask();
            cb.Unchecked += (_, _) => ApplyMask();
        }

        var scrollViewer = new Microsoft.UI.Xaml.Controls.ScrollViewer
        {
            Content = flyoutContent,
            MaxHeight = 300,
        };
        var flyout = new Microsoft.UI.Xaml.Controls.Flyout { Content = scrollViewer };
        Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.SetAttachedFlyout(toggle, flyout);
        toggle.Click += (_, _) =>
        {
            Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(toggle);
            toggle.IsChecked = null; // keep toggle visually neutral; active state shown via Content
        };

        return toggle;
    }

    // ── Column reorder by drag ────────────────────────────────────────────────
    // Reuses the upstream reorder event sequence (CanUserReorderColumns gate →
    // ColumnReordering → move DisplayIndex → ColumnReordered) from
    // DataGridColumnHeadersPresenter, but driven by WinUI pointer events on the
    // manually-built header row instead of the (unused) ItemsControl presenter.
    private Microsoft.UI.Xaml.Controls.StackPanel? _headerHostPanel;
    private DataGridColumn? _reorderColumn;
    private DataGridColumnHeader? _reorderHeader;
    private double _reorderStartX;
    private bool _reorderActive;
    private Microsoft.UI.Xaml.Controls.Border? _reorderIndicator;
    private const double ReorderDragThreshold = 4.0;

    private void OnHeaderPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is DataGridColumnHeader resizeHeader && TryBeginHeaderResize(resizeHeader, e))
        {
            return;
        }

        if (!CanUserReorderColumns || _headerHostPanel is null)
            return;
        if (sender is not DataGridColumnHeader hdr || hdr.Column is not { CanUserReorder: true } col)
            return;

        // Record the candidate; do NOT capture yet so a plain click still sorts.
        _reorderHeader = hdr;
        _reorderColumn = col;
        _reorderStartX = e.GetCurrentPoint(_headerHostPanel).Position.X;
        _reorderActive = false;
    }

    private void OnHeaderDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is not DataGridColumnHeader header
            || ResolveHeaderResizeColumn(header, e.GetPosition(header).X) is not { } column)
        {
            return;
        }

        if (ShimTryAutoSizeColumn(column))
        {
            e.Handled = true;
        }
    }

    private void OnHeaderPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (ContinueHeaderResize(e))
        {
            return;
        }

        if (_reorderColumn is null || _reorderHeader is null || _headerHostPanel is null)
            return;

        var x = e.GetCurrentPoint(_headerHostPanel).Position.X;
        if (!_reorderActive)
        {
            if (Math.Abs(x - _reorderStartX) <= ReorderDragThreshold)
                return; // below the drag threshold — still a potential click
            _reorderActive = true;
            _reorderHeader.CapturePointer(e.Pointer);
            _reorderHeader.Opacity = 0.5;
        }

        UpdateReorderIndicator(ComputeDropSlot(x));
    }

    private void OnHeaderPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (EndHeaderResize(e))
        {
            return;
        }

        if (_reorderColumn is { } col && _reorderActive && _headerHostPanel is not null)
        {
            var slot = ComputeDropSlot(e.GetCurrentPoint(_headerHostPanel).Position.X);
            var target = _visibleColumns[Math.Clamp(slot, 0, _visibleColumns.Count - 1)].DisplayIndex;
            if (sender is DataGridColumnHeader hdr)
                hdr.ReleasePointerCapture(e.Pointer);
            EndReorder();
            ShimTryReorderColumn(col, target);
            e.Handled = true; // suppress the click-to-sort that would otherwise fire
        }
        else
        {
            EndReorder();
        }
    }

    private void OnHeaderPointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        EndHeaderResize(e);
        EndReorder();
    }

    // Walk the realized headers (display order) accumulating widths; return the
    // index of the slot whose left half the pointer is over (drop-before), or the
    // count for drop-after-last.
    private int ComputeDropSlot(double x)
    {
        double offset = AreRowHeadersVisible ? RowHeaderShimWidth : 0;
        for (var i = 0; i < _headerCells.Count; i++)
        {
            var w = _headerCells[i].ActualWidth > 0 ? _headerCells[i].ActualWidth : ShimColumnWidth(_visibleColumns[i]);
            if (x < offset + w / 2)
                return i;
            offset += w;
        }
        return Math.Max(0, _visibleColumns.Count - 1);
    }

    private void UpdateReorderIndicator(int slot)
    {
        if (_headerHostPanel is null)
            return;

        _reorderIndicator ??= new Microsoft.UI.Xaml.Controls.Border
        {
            Width = 2,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x78, 0xD4)),
        };

        _headerHostPanel.Children.Remove(_reorderIndicator);
        var panelIndex = (AreRowHeadersVisible ? 1 : 0) + Math.Clamp(slot, 0, _headerCells.Count);
        panelIndex = Math.Clamp(panelIndex, 0, _headerHostPanel.Children.Count);
        _headerHostPanel.Children.Insert(panelIndex, _reorderIndicator);
    }

    private void EndReorder()
    {
        if (_reorderHeader is not null)
            _reorderHeader.Opacity = 1.0;
        if (_reorderIndicator is not null)
            _headerHostPanel?.Children.Remove(_reorderIndicator);
        _reorderIndicator = null;
        _reorderHeader = null;
        _reorderColumn = null;
        _reorderActive = false;
    }

    // Core reorder commit, reusing the upstream event sequence + DisplayIndex
    // semantics. Used by the pointer handlers and exposed for probe simulation.
    // Returns true if the column actually moved.
    internal bool ShimTryReorderColumn(DataGridColumn column, int targetDisplayIndex)
    {
        if (column is null || !CanUserReorderColumns || !column.CanUserReorder)
            return false;

        targetDisplayIndex = Math.Clamp(targetDisplayIndex, 0, Columns.Count - 1);
        if (targetDisplayIndex == column.DisplayIndex)
            return false;

        var reordering = new DataGridColumnReorderingEventArgs(column);
        OnColumnReordering(reordering);
        if (reordering.Cancel)
            return false;

        column.DisplayIndex = targetDisplayIndex;
        BuildShimVisualTree();
        OnColumnReordered(new DataGridColumnEventArgs(column));
        return true;
    }

    // Header content with a sort-direction glyph when this column is the
    // active sort.
    internal object? HeaderContent(DataGridColumn column)
    {
        if (column.SortDirection is { } dir)
        {
            var glyph = dir == System.ComponentModel.ListSortDirection.Ascending ? " ▲" : " ▼";
            return (column.Header?.ToString() ?? string.Empty) + glyph;
        }

        return column.Header;
    }

    // Session 69: row background for alternating rows.
    // WPF alternation: index 0 = RowBackground, index 1 = AlternatingRowBackground
    // (AlternationCount coerced to 2 when AlternatingRowBackground is set).
    internal Microsoft.UI.Xaml.Media.Brush? ShimRowBackground(int rowIndex)
        => rowIndex % 2 == 1 && AlternatingRowBackground is { } alt ? alt : RowBackground;

    // Column width: explicit pixel widths are honored; Auto/SizeToCells/
    // SizeToHeader/Star are not computed yet, so they fall back to a default.
    // ActualWidth (if a width-computation pass ever sets it) wins.
    // ── Session 49: row headers ──────────────────────────────────────────────
    internal bool AreRowHeadersVisible
        => (HeadersVisibility & DataGridHeadersVisibility.Row) == DataGridHeadersVisibility.Row;

    internal double RowHeaderShimWidth => RowHeaderWidth > 0 ? RowHeaderWidth : 24;

    internal double ShimColumnWidth(DataGridColumn column)
    {
        var width = column.Width;
        // Absolute widths take priority: the notification chain may deliver a
        // width change before the post-layout pass re-runs, so trust the new
        // Width.Value rather than the stale ActualWidth from the previous pass.
        if (width.IsAbsolute && width.Value > 0)
            return Clamp(column, width.Value);
        // Non-absolute (Auto/Star/SizeTo*): use the ActualWidth set by the
        // post-layout pass, or NaN to let Uno auto-size to content.
        return column.ActualWidth > 0 ? column.ActualWidth : double.NaN;
    }

    // ── Session 26: reactivity ───────────────────────────────────────────────
    // Re-render when Items or Columns change. Subscriptions are idempotent
    // (hooked once); the rebuild no-ops until the template provides
    // PART_ShimRowsHost, after which it refreshes the whole grid.
    private bool _shimChangeHooked;

    private void HookShimChangeNotifications()
    {
        if (_shimChangeHooked)
        {
            return;
        }

        _shimChangeHooked = true;
        ((System.Collections.Specialized.INotifyCollectionChanged)Items).CollectionChanged += OnShimContentChanged;
        ((System.Collections.Specialized.INotifyCollectionChanged)Columns).CollectionChanged += OnShimContentChanged;
        // Session 62: row selection visuals are now driven by the real engine's
        // SelectionChanged event instead of a manual visual pass.
        SelectionChanged += OnShimSelectionChanged;
    }

    // Reflect the engine's selection onto the realized row containers. This is
    // the single source of truth for live row highlight; rebuilds re-read
    // SelectedItems directly (containers don't exist yet when the batch runs).
    private void OnShimSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        foreach (var removed in e.RemovedItems)
        {
            if (ItemContainerGenerator.ContainerFromItem(removed) is DataGridRow row)
            {
                row.IsSelected = false;
            }
        }

        foreach (var added in e.AddedItems)
        {
            if (ItemContainerGenerator.ContainerFromItem(added) is DataGridRow row)
            {
                row.IsSelected = true;
            }
        }
    }

    // Row is selected per the real engine (Selector.SelectedItems).
    private bool IsRowItemSelected(object? item) => item is not null && SelectedItems.Contains(item);

    private void OnShimContentChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => BuildShimVisualTree();

    // ── Session 28/63: input-to-selection bridge ────────────────────────────
    // Pointer press on a row routes to the linked WPF DataGrid selection engine.
    // The engine owns SelectedItems/SelectedItem/SelectionChanged and row
    // visuals reflect that state.

    // Called by a row as it (re)builds a cell, so a retained cell selection
    // re-applies to the new cell instance after a rebuild.
    internal bool TryReselectCell(DataGridCell cell)
    {
        var selected = IsCellSelectedByEngine(cell);
        if (selected)
        {
            cell.IsSelected = true;
        }

        return selected;
    }

    // ── Session 43: editing hardening ────────────────────────────────────────
    // Effective read-only: the grid or the column being read-only blocks edits.
    internal bool IsCellEffectivelyReadOnly(DataGridColumn? column)
        => IsReadOnly || (column?.IsReadOnly ?? false);

    // Forwarders so DataGridCell (a different class) can raise the protected
    // edit-lifecycle events on the linked control root and read cancellation.
    internal DataGridBeginningEditEventArgs RaiseBeginningEdit(
        DataGridColumn column, DataGridRow row, RoutedEventArgs? editingEventArgs)
    {
        var args = new DataGridBeginningEditEventArgs(column, row, editingEventArgs ?? new RoutedEventArgs());
        OnBeginningEdit(args);
        return args;
    }

    internal DataGridCellEditEndingEventArgs RaiseCellEditEnding(
        DataGridColumn column, DataGridRow row, FrameworkElement? editingElement, DataGridEditAction action)
    {
        var args = new DataGridCellEditEndingEventArgs(column, row, editingElement!, action);
        OnCellEditEnding(args);
        return args;
    }

    // ── Session 47: row edit transactions ───────────────────────────────────
    // A cell entering edit begins the row's edit transaction; committing /
    // canceling the cell ends it, driving IEditableObject and RowEditEnding.
    private DataGridRow? _editingRow;

    internal void BeginRowEdit(DataGridRow? row)
    {
        if (row is null)
        {
            return;
        }

        if (ReferenceEquals(_editingRow, row)
            && Items.IsEditingItem
            && ReferenceEquals(Items.CurrentEditItem, row.Item))
        {
            return;
        }

        _editingRow = row;
        row.IsEditing = true;
        Items.EditItem(row.Item);
    }

    internal bool CommitRowEdit(DataGridRow? row)
    {
        if (row is null || !ReferenceEquals(_editingRow, row))
        {
            return true;
        }

        // Row-level validation (session 48): run RowValidationRules against the
        // item; any failure flags the row and keeps it in edit.
        foreach (var rule in RowValidationRules)
        {
            var result = rule.Validate(row.Item, System.Globalization.CultureInfo.CurrentCulture);
            if (!result.IsValid)
            {
                row.SetRowError(result.ErrorContent?.ToString());
                return false;
            }
        }

        var args = new DataGridRowEditEndingEventArgs(row, DataGridEditAction.Commit);
        OnRowEditEnding(args);
        if (args.Cancel)
        {
            return false;
        }

        row.ClearRowError();
        Items.CommitEdit();
        row.IsEditing = false;
        _editingRow = null;
        return true;
    }

    internal void CancelRowEdit(DataGridRow? row)
    {
        if (row is null || !ReferenceEquals(_editingRow, row))
        {
            return;
        }

        var args = new DataGridRowEditEndingEventArgs(row, DataGridEditAction.Cancel);
        OnRowEditEnding(args);
        if (Items.IsEditingItem)
        {
            if (Items.CanCancelEdit)
            {
                Items.CancelEdit();
            }
            else
            {
                Items.CommitEdit();
            }
        }

        row.IsEditing = false;
        _editingRow = null;
    }

    internal void HandleShimCellClicked(DataGridCell cell)
    {
        var previousModifiers = Input.Keyboard.ModifiersOverride;
        Input.Keyboard.ModifiersOverride = Input.ModifierKeys.None;
        try
        {
            var oldCurrentCell = CurrentCellContainer;
            CurrentCellContainer = cell;
            HandleSelectionForCellInput(
                cell,
                startDragging: false,
                allowsExtendSelect: true,
                allowsMinimalSelect: true);
            SyncRealizedCellSelection();
            // The upstream setter's IsKeyboardFocusWithin guard skips
            // NotifyCurrentCellContainerChanged in headless/non-focused
            // scenarios. Drive the focus-border visual explicitly so that
            // the current cell always shows a visual indicator.
            if (oldCurrentCell is not null && !ReferenceEquals(oldCurrentCell, cell))
                oldCurrentCell.NotifyCurrentCellContainerChanged();
            cell.NotifyCurrentCellContainerChanged();
        }
        finally
        {
            Input.Keyboard.ModifiersOverride = previousModifiers;
        }
    }

    internal void HandleShimRowClicked(DataGridRow clicked)
        => HandleShimRowClicked(clicked, global::Windows.System.VirtualKeyModifiers.None);

    internal void HandleShimRowClicked(DataGridRow clicked, global::Windows.System.VirtualKeyModifiers modifiers)
    {
        var previousModifiers = Input.Keyboard.ModifiersOverride;
        Input.Keyboard.ModifiersOverride = ToWpfModifiers(modifiers);
        try
        {
            HandleSelectionForRowHeaderAndDetailsInput(clicked, startDragging: false);
        }
        finally
        {
            Input.Keyboard.ModifiersOverride = previousModifiers;
        }
    }

    private static Input.ModifierKeys ToWpfModifiers(global::Windows.System.VirtualKeyModifiers modifiers)
    {
        var result = Input.ModifierKeys.None;
        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Control) != 0)
        {
            result |= Input.ModifierKeys.Control;
        }

        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Shift) != 0)
        {
            result |= Input.ModifierKeys.Shift;
        }

        if ((modifiers & global::Windows.System.VirtualKeyModifiers.Menu) != 0)
        {
            result |= Input.ModifierKeys.Alt;
        }

        return result;
    }

    private void ClearRealRowSelection()
    {
        BeginUpdateSelectedItems();
        try
        {
            SelectedItems.Clear();
        }
        finally
        {
            EndUpdateSelectedItems();
        }
    }

    private void PruneRealRowSelection()
    {
        var present = Items.Cast<object?>().ToList();
        var removed = SelectedItems
            .Cast<object?>()
            .Where(selected => !present.Any(item => EqualsEx(item, selected)))
            .ToList();
        if (removed.Count == 0)
        {
            return;
        }

        BeginUpdateSelectedItems();
        try
        {
            foreach (var item in removed)
            {
                SelectedItems.Remove(item);
            }
        }
        finally
        {
            EndUpdateSelectedItems();
        }
    }

    private void PruneRealCellSelection()
    {
        var present = Items.Cast<object?>().ToList();
        var removed = SelectedCells
            .Cast<DataGridCellInfo>()
            .Where(cell => !present.Any(item => EqualsEx(item, cell.Item)))
            .ToList();

        if (removed.Count > 0)
        {
            using (UpdateSelectedCells())
            {
                foreach (var cell in removed)
                {
                    SelectedCells.Remove(cell);
                }
            }
        }

        if (CurrentCell.IsValid && !present.Any(item => EqualsEx(item, CurrentCell.Item)))
        {
            CurrentCell = DataGridCellInfo.Unset;
        }
    }

    private bool IsCellSelectedByEngine(DataGridCell cell)
        => (CurrentCell.IsValid
                && EqualsEx(CurrentCell.Item, cell.RowDataItem)
                && ReferenceEquals(CurrentCell.Column, cell.Column))
            || SelectedCells
            .Cast<DataGridCellInfo>()
            .Any(info => EqualsEx(info.Item, cell.RowDataItem) && ReferenceEquals(info.Column, cell.Column));

    // ── Clipboard copy ───────────────────────────────────────────────────────
    // WPF's linked command handler depends on the full selected-cell range
    // internals. The shim visual path keeps the public selection surfaces
    // current, so build the clipboard payload from SelectedCells/SelectedItems
    // and reuse the upstream column cell-copy event plus row formatter.
    internal bool ShimCopySelectionToClipboard()
    {
        if (ClipboardCopyMode == DataGridClipboardCopyMode.None)
        {
            return false;
        }

        var dataObject = ShimBuildClipboardDataObject();
        if (dataObject is null)
        {
            return false;
        }

        Clipboard.SetDataObject(dataObject, copy: true);
        return true;
    }

    internal DataObject? ShimBuildClipboardDataObject()
    {
        var columns = ColumnsInDisplayOrder().Where(column => column.IsVisible).ToList();
        if (columns.Count == 0)
        {
            return null;
        }

        var plan = ShimBuildClipboardPlan(columns);
        if (plan.Rows.Count == 0 || plan.Columns.Count == 0)
        {
            return null;
        }

        var text = new System.Text.StringBuilder();
        var csv = new System.Text.StringBuilder();

        if (ClipboardCopyMode == DataGridClipboardCopyMode.IncludeHeader)
        {
            var headerArgs = new DataGridRowClipboardEventArgs(
                null!,
                plan.Columns.Min(column => column.DisplayIndex),
                plan.Columns.Max(column => column.DisplayIndex),
                isColumnHeadersRow: true);
            foreach (var column in plan.Columns)
            {
                headerArgs.ClipboardRowContent.Add(new DataGridClipboardCellContent(null!, column, column.Header));
            }

            text.Append(headerArgs.FormatClipboardCellValues(DataFormats.UnicodeText));
            csv.Append(headerArgs.FormatClipboardCellValues(DataFormats.CommaSeparatedValue));
        }

        foreach (var row in plan.Rows)
        {
            var rowArgs = new DataGridRowClipboardEventArgs(
                row.Item,
                plan.Columns.Min(column => column.DisplayIndex),
                plan.Columns.Max(column => column.DisplayIndex),
                isColumnHeadersRow: false,
                row.Index);

            foreach (var column in plan.Columns)
            {
                var content = row.SelectedColumns is null || row.SelectedColumns.Contains(column)
                    ? column.OnCopyingCellClipboardContent(row.Item)
                    : null;
                rowArgs.ClipboardRowContent.Add(new DataGridClipboardCellContent(row.Item, column, content));
            }

            text.Append(rowArgs.FormatClipboardCellValues(DataFormats.UnicodeText));
            csv.Append(rowArgs.FormatClipboardCellValues(DataFormats.CommaSeparatedValue));
        }

        var dataObject = new DataObject();
        var unicode = text.ToString();
        dataObject.SetData(DataFormats.UnicodeText, unicode, autoConvert: false);
        dataObject.SetData(DataFormats.Text, unicode, autoConvert: false);
        dataObject.SetData(DataFormats.CommaSeparatedValue, csv.ToString(), autoConvert: false);
        return dataObject;
    }

    private ClipboardPlan ShimBuildClipboardPlan(IReadOnlyList<DataGridColumn> visibleColumns)
    {
        var itemOrder = Items.Cast<object?>().ToList();
        var selectedCells = SelectedCells
            .Cast<DataGridCellInfo>()
            .Where(cell => cell.IsValid && cell.Item is not null && cell.Column is { IsVisible: true })
            .ToList();

        if (selectedCells.Count > 0)
        {
            var selectedColumns = selectedCells
                .Select(cell => cell.Column)
                .Distinct()
                .OrderBy(column => column.DisplayIndex)
                .ToList();
            var rows = selectedCells
                .GroupBy(cell => cell.Item)
                .Select(group => new ClipboardRow(
                    group.Key,
                    itemOrder.FindIndex(item => EqualsEx(item, group.Key)),
                    group.Select(cell => cell.Column).Distinct().ToHashSet()))
                .Where(row => row.Index >= 0)
                .OrderBy(row => row.Index)
                .ToList();

            return new ClipboardPlan(selectedColumns, rows);
        }

        var selectedItems = SelectedItems
            .Cast<object?>()
            .Where(item => item is not null)
            .Distinct()
            .Select(item => new ClipboardRow(item!, itemOrder.FindIndex(candidate => EqualsEx(candidate, item)), null))
            .Where(row => row.Index >= 0)
            .OrderBy(row => row.Index)
            .ToList();
        if (selectedItems.Count > 0)
        {
            return new ClipboardPlan(visibleColumns.ToList(), selectedItems);
        }

        if (CurrentCell.IsValid && CurrentCell.Item is not null && CurrentCell.Column is { IsVisible: true } currentColumn)
        {
            var rowIndex = itemOrder.FindIndex(item => EqualsEx(item, CurrentCell.Item));
            if (rowIndex >= 0)
            {
                return new ClipboardPlan([currentColumn], [new ClipboardRow(CurrentCell.Item, rowIndex, null)]);
            }
        }

        return new ClipboardPlan([], []);
    }

    private sealed record ClipboardPlan(IReadOnlyList<DataGridColumn> Columns, IReadOnlyList<ClipboardRow> Rows);
    private sealed record ClipboardRow(object Item, int Index, ISet<DataGridColumn>? SelectedColumns);

    private void SyncRealizedCellSelection()
    {
        foreach (var row in ItemContainerGenerator.Containers.OfType<DataGridRow>())
        {
            for (var i = 0; i < Columns.Count; i++)
            {
                if (row.TryGetCell(i) is { } realizedCell)
                {
                    realizedCell.SyncIsSelected(IsCellSelectedByEngine(realizedCell));
                }
            }
        }
    }

    // ── Session 33: keyboard navigation ──────────────────────────────────────
    // Up/Down arrows move the single selection between rows.
    private const int ShimPageSize = 5;

    protected override void OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);
        var modifiers = Input.Keyboard.Modifiers;
        if (e.Key == global::Windows.System.VirtualKey.C
            && (modifiers & Input.ModifierKeys.Control) != 0
            && ShimCopySelectionToClipboard())
        {
            e.Handled = true;
            return;
        }

        if (e.Key == global::Windows.System.VirtualKey.A
            && (modifiers & Input.ModifierKeys.Control) != 0
            && ShimSelectAllCells())
        {
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case global::Windows.System.VirtualKey.Left:
                MoveCurrentCellByOffset(0, -1, (modifiers & Input.ModifierKeys.Shift) != 0);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Right:
                MoveCurrentCellByOffset(0, 1, (modifiers & Input.ModifierKeys.Shift) != 0);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Up:
                if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                {
                    MoveCurrentCellByOffset(-1, 0, (modifiers & Input.ModifierKeys.Shift) != 0);
                }
                else
                {
                    MoveSelectionByOffset(-1);
                }
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Down:
                if (SelectionUnit == DataGridSelectionUnit.Cell || SelectionUnit == DataGridSelectionUnit.CellOrRowHeader)
                {
                    MoveCurrentCellByOffset(1, 0, (modifiers & Input.ModifierKeys.Shift) != 0);
                }
                else
                {
                    MoveSelectionByOffset(1);
                }
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.Home:
                MoveSelectionToIndex(0);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.End:
                MoveSelectionToIndex(int.MaxValue);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.PageUp:
                MoveSelectionByOffset(-ShimPageSize);
                e.Handled = true;
                break;
            case global::Windows.System.VirtualKey.PageDown:
                MoveSelectionByOffset(ShimPageSize);
                e.Handled = true;
                break;
        }
    }

    internal void MoveSelectionByOffset(int delta)
    {
        var rows = ItemContainerGenerator.Containers.OfType<DataGridRow>().ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var current = rows.FindIndex(r => r.IsSelected);
        var target = current < 0
            ? (delta > 0 ? 0 : rows.Count - 1)
            : Math.Clamp(current + delta, 0, rows.Count - 1);

        HandleShimRowClicked(rows[target]);
    }

    internal void MoveSelectionToIndex(int index)
    {
        var rows = ItemContainerGenerator.Containers.OfType<DataGridRow>().ToList();
        if (rows.Count == 0)
        {
            return;
        }

        HandleShimRowClicked(rows[Math.Clamp(index, 0, rows.Count - 1)]);
    }

    internal bool MoveCurrentCellByOffset(int rowDelta, int columnDelta, bool extendSelection)
    {
        var items = Items.Cast<object?>().Where(item => item is not null).ToList();
        var columns = ColumnsInDisplayOrder().Where(column => column.IsVisible).ToList();
        if (items.Count == 0 || columns.Count == 0)
        {
            return false;
        }

        var rowIndex = CurrentCell.IsValid
            ? items.FindIndex(item => EqualsEx(item, CurrentCell.Item))
            : 0;
        if (rowIndex < 0)
        {
            rowIndex = 0;
        }

        var columnIndex = CurrentCell.IsValid && CurrentCell.Column is not null
            ? columns.FindIndex(column => ReferenceEquals(column, CurrentCell.Column))
            : 0;
        if (columnIndex < 0)
        {
            columnIndex = 0;
        }

        var targetRowIndex = Math.Clamp(rowIndex + rowDelta, 0, items.Count - 1);
        var targetColumnIndex = Math.Clamp(columnIndex + columnDelta, 0, columns.Count - 1);
        return MoveCurrentCellTo(items[targetRowIndex]!, columns[targetColumnIndex], extendSelection);
    }

    internal bool MoveCurrentCellTo(DataGridRow row, DataGridColumn column, bool extendSelection)
        => row.Item is not null && MoveCurrentCellTo(row.Item, column, extendSelection);

    private bool MoveCurrentCellTo(object item, DataGridColumn column, bool extendSelection)
    {
        if (!column.IsVisible)
        {
            return false;
        }

        var next = new DataGridCellInfo(item, column, this);
        var oldCurrentCell = CurrentCellContainer;
        CurrentCell = next;
        var row = FindShimRowForItem(item);
        CurrentCellContainer = row is null ? null : FindShimCell(row, column);

        if (SelectionUnit == DataGridSelectionUnit.FullRow)
        {
            if (row is not null)
            {
                HandleShimRowClicked(row);
            }
            else if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
            }
        }
        else
        {
            using (UpdateSelectedCells())
            {
                if (!extendSelection)
                {
                    SelectedCells.Clear();
                }

                if (!SelectedCells.Contains(next))
                {
                    SelectedCells.Add(next);
                }
            }
        }

        if (oldCurrentCell is not null && !ReferenceEquals(oldCurrentCell, CurrentCellContainer))
        {
            oldCurrentCell.NotifyCurrentCellContainerChanged();
        }

        CurrentCellContainer?.NotifyCurrentCellContainerChanged();
        SyncRealizedCellSelection();
        return true;
    }

    internal bool ShimSelectAllCells()
    {
        var items = Items.Cast<object?>().Where(item => item is not null).ToList();
        var columns = ColumnsInDisplayOrder().Where(column => column.IsVisible).ToList();
        if (items.Count == 0 || columns.Count == 0)
        {
            return false;
        }

        if (SelectionUnit == DataGridSelectionUnit.FullRow)
        {
            BeginUpdateSelectedItems();
            try
            {
                SelectedItems.Clear();
                foreach (var item in items)
                {
                    SelectedItems.Add(item);
                    if (FindShimRowForItem(item!) is { } row)
                    {
                        row.IsSelected = true;
                    }
                }
            }
            finally
            {
                EndUpdateSelectedItems();
            }

            return true;
        }

        using (UpdateSelectedCells())
        {
            SelectedCells.Clear();
            foreach (var item in items)
            {
                foreach (var column in columns)
                {
                    SelectedCells.Add(new DataGridCellInfo(item!, column, this));
                }
            }
        }

        CurrentCell = new DataGridCellInfo(items[0]!, columns[0], this);
        var firstRow = FindShimRowForItem(items[0]!);
        CurrentCellContainer = firstRow is null ? null : FindShimCell(firstRow, columns[0]);
        SyncRealizedCellSelection();
        return true;
    }

    private DataGridCell? FindShimCell(DataGridRow row, DataGridColumn column)
    {
        var columns = ColumnsInDisplayOrder().Where(c => c.IsVisible).ToList();
        var index = columns.FindIndex(c => ReferenceEquals(c, column));
        return index >= 0 ? row.TryGetCell(index) : null;
    }

    // Session 50 reuse: header click drives the real WPF sort path. PerformSort
    // raises the Sorting event, toggles direction, and updates
    // Items.SortDescriptions; ItemCollection.Refresh applies the sort and
    // raises Reset, which rebuilds the rendered rows in sorted order.
    internal void HandleShimHeaderClicked(DataGridColumn column) => PerformSort(column);

    // Session 67: column-header notification chain — mirrors the row-cell chain
    // from session 66. The upstream DataGrid.NotifyPropertyChanged would route
    // through ColumnHeadersPresenter (null in the shim) to headers; this shim
    // dispatch iterates _headerCells directly instead.
    private void ShimNotifyColumnHeaders(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        foreach (var header in _headerCells)
            header.NotifyPropertyChanged(d, e);
    }

    internal IEnumerable<DataGridColumn> ColumnsInDisplayOrder()
    {
        InternalColumns.RefreshDisplayIndexMap();
        for (var displayIndex = 0; displayIndex < Columns.Count; displayIndex++)
        {
            yield return ColumnFromDisplayIndex(displayIndex);
        }
    }
}

internal static class DataGridColumnResizeShim
{
    internal static double ComputeWidth(double currentWidth, double horizontalChange, double minWidth, double maxWidth)
        => ClampWidth(currentWidth + horizontalChange, minWidth, maxWidth);

    internal static double ClampWidth(double width, double minWidth, double maxWidth)
        => Math.Clamp(width, minWidth, maxWidth);
}
