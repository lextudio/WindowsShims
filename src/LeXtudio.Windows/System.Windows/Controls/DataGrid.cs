using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// Uno-specific additions to the WPF DataGrid control root. Only members that
// do NOT appear in the linked upstream DataGrid.cs should live here. The
// upstream file is compiled as a partial on HAS_UNO so both parts merge.
public partial class DataGrid
{
    private Microsoft.UI.Xaml.ElementTheme _shimFluentDefaultsTheme = Microsoft.UI.Xaml.ElementTheme.Default;
    private bool _shimFluentThemeHooked;

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
        // Ensure the shim template is applied so GetTemplateChild can find
        // PART_ShimRowsHost. On first call the template was set in the
        // constructor (via EnsureShimStyleKey) but hasn't been instantiated yet.
        ApplyTemplate();
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
        "<Grid Background='{TemplateBinding Background}'>" +
        // Session 123: Fluent brushes are supplied through the DataGrid DPs so
        // user styling still wins and this template remains layout-only.
        "<ScrollViewer x:Name='PART_ShimRowsScroll' HorizontalScrollBarVisibility='Auto' VerticalScrollBarVisibility='Auto'>" +
        "<StackPanel x:Name='PART_ShimRowsHost' MinWidth='120' MinHeight='40' />" +
        "</ScrollViewer>" +
        // Draw the complete frame as an overlay. A wrapping Border reserves an
        // inner layout pixel, which puts the last row line beside the bottom
        // frame instead of on the same device-pixel coordinate.
        "<Border x:Name='PART_ShimOuterBorder' BorderBrush='{TemplateBinding BorderBrush}' " +
        "BorderThickness='{TemplateBinding BorderThickness}' IsHitTestVisible='False' />" +
        // Session 120: floating drag-header/drop-indicator overlay (see the "Column
        // reorder by drag" region) — a top-level sibling so the floating header can be
        // positioned anywhere over the grid, independent of the scrolling rows/header.
        "<Canvas x:Name='PART_ShimDragOverlay' IsHitTestVisible='False' />" +
        "</Grid></ControlTemplate>";

    // Session 119 (Slice 5): opt-in virtualized template. PART_ShimRowsHost is a live
    // DataGridRowsPresenter (IsItemsHost) instead of a StackPanel, so the Slice 4
    // VirtualizingStackPanel engine generates only the rows in view. Off by default —
    // the manual BuildShimVisualTree path is unchanged unless ShimSetRowVirtualization(true)
    // is called.
    private const string ShimVirtualizedTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
        "xmlns:p='using:System.Windows.Controls.Primitives'>" +
        // Same Fluent DP-driven chrome as the manual template.
        "<Grid Background='{TemplateBinding Background}'>" +
        "<Grid>" +
        "<Grid.RowDefinitions><RowDefinition Height='Auto'/><RowDefinition Height='*'/></Grid.RowDefinitions>" +
        // Row 0: pinned column-header host (does not scroll vertically), matching WPF.
        "<Border x:Name='PART_ShimHeaderHost' Grid.Row='0'/>" +
        // Row 1: ScrollViewer hosting the virtualizing presenter. Horizontal scroll is
        // enabled for wide tables; the pinned header is translated to match its horizontal
        // offset (ShimHookHeaderScrollSync) so columns stay aligned.
        "<ScrollViewer x:Name='PART_ShimRowsScroll' Grid.Row='1' VerticalScrollBarVisibility='Auto' HorizontalScrollBarVisibility='Auto'>" +
        "<p:DataGridRowsPresenter x:Name='PART_ShimRowsHost' IsItemsHost='True' MinWidth='120' MinHeight='40' />" +
        "</ScrollViewer>" +
        // Session 120: floating drag-header/drop-indicator overlay, spanning both rows
        // so the floating header can be positioned over the pinned header OR the
        // scrolling rows area.
        "<Canvas x:Name='PART_ShimDragOverlay' Grid.RowSpan='2' IsHitTestVisible='False' />" +
        "</Grid>" +
        "<Border x:Name='PART_ShimOuterBorder' BorderBrush='{TemplateBinding BorderBrush}' " +
        "BorderThickness='{TemplateBinding BorderThickness}' IsHitTestVisible='False' />" +
        "</Grid></ControlTemplate>";

    // Row count above which the manual render auto-switches to the virtualized presenter.
    private const int ShimAutoVirtualizeThreshold = 1000;

    private bool _shimUseRowsPresenter;
    private bool _shimFluentDefaultsApplied;

    internal bool ShimUseRowsPresenter => _shimUseRowsPresenter;

    // Session 121 (frozen columns, Slice 1): opt-in swap of each row's cell host from the
    // manual BuildCells()-populated StackPanel to a live DataGridCellsPresenter (mirroring
    // the header-presenter swap, session 120 B1 slice 1). Default off — BuildCells is
    // unaffected unless this is called. Column width/resize/notification batches still
    // target the manual _cells list and are not wired to the presenter path yet, so
    // enabling this only proves/renders cell content, not the full cell feature set —
    // frozen-column arrange itself is a later slice once generation is confirmed working.
    private bool _shimUseCellsPresenter;

    internal bool ShimUseCellsPresenter => _shimUseCellsPresenter;

    internal bool ShimSetCellsPresenterHost(bool enabled)
    {
        _shimUseCellsPresenter = enabled;
        BuildShimVisualTree();
        return enabled;
    }

    // Switches the live render between the manual StackPanel path and the virtualized
    // DataGridRowsPresenter path, rebuilding the template. Returns true if the
    // virtualized host is in place afterwards.
    internal bool ShimSetRowVirtualization(bool enabled)
    {
        if (_shimUseRowsPresenter == enabled && Template is not null)
            return enabled;

        _shimUseRowsPresenter = enabled;
        try
        {
            Template = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(enabled ? ShimVirtualizedTemplateXaml : ShimTemplateXaml);
            ApplyTemplate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataGrid] virtualized template load failed: {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        // Manual path builds rows; virtualized path builds the pinned header and lets the
        // presenter self-populate rows on measure. BuildShimVisualTree handles both via its
        // host-type branch.
        BuildShimVisualTree();

        return enabled;
    }

    private void EnsureShimStyleKey()
    {
        if (!_shimFluentThemeHooked)
        {
            _shimFluentThemeHooked = true;
            ActualThemeChanged += (_, _) =>
            {
                ApplyShimFluentDefaults(force: true);
                BuildShimVisualTree();
                base.UpdateLayout();
            };
        }

        ApplyShimFluentDefaults(force: false);

        if (Template is not null)
        {
            return;
        }

        try
        {
            Template = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(
                    _shimUseRowsPresenter ? ShimVirtualizedTemplateXaml : ShimTemplateXaml);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataGrid] shim template load failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void ApplyShimFluentDefaults(bool force)
    {
        var theme = RequestedTheme != Microsoft.UI.Xaml.ElementTheme.Default ? RequestedTheme : ActualTheme;
        if (!force && _shimFluentDefaultsApplied && _shimFluentDefaultsTheme == theme)
        {
            return;
        }

        _shimFluentDefaultsApplied = true;
        _shimFluentDefaultsTheme = theme;
        Background = DataGridFluentTheme.GridBackgroundFor(this);
        BorderBrush = DataGridFluentTheme.OuterBorderFor(this);
        BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
        HorizontalGridLinesBrush = DataGridFluentTheme.GridLineFor(this);
        VerticalGridLinesBrush = DataGridFluentTheme.GridLineFor(this);
        RowBackground = new Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF));
        AlternatingRowBackground = DataGridFluentTheme.AlternatingRowFor(this);
        Foreground = DataGridFluentTheme.PrimaryTextFor(this);
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

        // Virtualized path: the DataGridRowsPresenter generates its own rows in view via
        // the VirtualizingStackPanel engine. Build the pinned header, then refresh the
        // filtered/sorted view and let the presenter re-realize the visible rows.
        if (host is Primitives.DataGridRowsPresenter)
        {
            InternalColumns.RefreshDisplayIndexMap();
            _visibleColumns = ColumnsInDisplayOrder().Where(c => c.IsVisible).ToList();
            ShimBuildVirtualizedHeader();
            ShimInvalidateRealizationView();
            // Session 120 (B1 slice 2): the manual path below schedules this same pass after
            // building rows, but that code is unreached here (early return). Without it,
            // Auto/SizeTo*/Star columns under virtualization never get a real per-column width
            // pass — they keep whatever estimate DataGridCellsPanel's own internal column-width
            // determination guessed (uniform average, not content-based).
            ScheduleAutoWidthPassIfNeeded();
            return;
        }

        // Large tables: eagerly building one DataGridRow per item is O(n) and stalls on
        // huge metadata tables (e.g. MethodDef, tens of thousands of rows). Auto-switch to
        // the virtualized presenter so only the visible window is realized. Metadata grids
        // are read-only, so the not-yet-complete editing-under-virtualization parity is moot.
        if (!_shimUseRowsPresenter && Items.Count > ShimAutoVirtualizeThreshold)
        {
            ShimSetRowVirtualization(true);
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
        // Filter row is no longer rendered. Filter UI is accessed inline in each
        // column header (see BuildFilterPanelForColumn).

        BuildRowsOrGroups(host);

        PruneRealRowSelection();
        PruneRealCellSelection();

        ItemContainerGenerator.NotifyContainersGenerated();

        if (editingItem is not null && editingColumn is not null)
        {
            RestoreEditingCellAfterRebuild(editingItem, editingColumn);
        }

        ScheduleAutoWidthPassIfNeeded();
    }

    // Schedule an Auto-width pass if any visible column is non-absolute.
    // Internal so DataGridColumnCollection.Redistribute* methods can trigger
    // an immediate pass on width/min/max/space changes.
    internal void ScheduleAutoWidthPassIfNeeded()
    {
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

    // Rebuilds only the data rows without touching the header row.
    // Used by filter TextChanged handlers so the header TextBox retains focus.
    internal void RefreshFilteredRows()
    {
        if (GetTemplateChild("PART_ShimRowsHost") is not Microsoft.UI.Xaml.Controls.Panel host)
            return;

        // Virtualized path: rows are generated by the presenter; refresh the filtered view.
        if (host is Primitives.DataGridRowsPresenter)
        {
            ShimInvalidateRealizationView();
            return;
        }

        // Remove data rows (everything after the header at index 0).
        while (host.Children.Count > 1)
            host.Children.RemoveAt(host.Children.Count - 1);

        ItemContainerGenerator.ResetContainers();
        _rowTrackingRoot = null;

        BuildRowsOrGroups(host);

        PruneRealRowSelection();
        PruneRealCellSelection();
        ItemContainerGenerator.NotifyContainersGenerated();
    }

    // Session 121 (DataGrid grouping, Slice 2): shared row-building body for the
    // manual (non-virtualized) render path, used by both BuildShimVisualTree and
    // RefreshFilteredRows. When IsGrouping is false this is exactly the flat
    // per-item DataGridRow loop those two methods used to duplicate. When
    // IsGrouping is true, it instead recurses Items' group tree, interleaving a
    // GroupItem header (Slice 2 renders a fixed name+count header — GroupStyle
    // is not shimmed yet) before each group's rows/subgroups, mirroring the
    // generic ItemsControl grouping shape real WPF DataGrid reuses (there is no
    // dedicated DataGridRowGroupHeader class upstream to link — see
    // docs/session121.md).
    private void BuildRowsOrGroups(Microsoft.UI.Xaml.Controls.Panel host)
    {
        var rowIndex = 0;
        if (IsGrouping)
        {
            BuildGroupedRows(host, Items.Groups, depth: 0, ref rowIndex);
            return;
        }

        foreach (var item in OrderedItems())
        {
            if (item is null)
            {
                continue;
            }

            var row = new DataGridRow();
            row.ShimApplyCellsPresenterTemplateIfNeeded(_shimUseCellsPresenter);
            row.PrepareRow(item, this); // also initializes row.Tracker
            ShimDecorateRow(row, item, rowIndex++);
            row.Tracker!.StartTracking(ref _rowTrackingRoot);

            // Register the row so the linked WPF code can resolve containers
            // (selection, scroll-into-view, row details) via the generator.
            ItemContainerGenerator.RegisterContainer(item, row);
            host.Children.Add(row);
        }
    }

    private void BuildGroupedRows(
        Microsoft.UI.Xaml.Controls.Panel host,
        IReadOnlyList<MS.Internal.Data.CollectionViewGroupInternal> groups,
        int depth,
        ref int rowIndex)
    {
        foreach (var group in groups)
        {
            var style = GroupItem.ResolveGroupStyle(this, group, depth);
            if (style?.HidesIfEmpty == true && group.ItemCount == 0)
            {
                // No header, no rows — the group is omitted from rendering entirely.
                continue;
            }

            var header = new GroupItem();
            header.ShimPrepareGroupHeader(group, depth, this);
            // Session 121 (DataGrid grouping, Slice 4): tapping the header toggles
            // IsExpanded; a full rebuild is this render path's existing "re-derive
            // the realized view" entry point (same one filter/sort changes use).
            header.ShimToggleGroupExpansion = BuildShimVisualTree;
            host.Children.Add(header);

            if (!group.IsExpanded)
            {
                continue; // collapsed: only the header renders, no rows/subgroups
            }

            if (group.IsBottomLevel)
            {
                foreach (var item in group.Items)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    var row = new DataGridRow();
                    row.ShimApplyCellsPresenterTemplateIfNeeded(_shimUseCellsPresenter);
                    row.PrepareRow(item, this);
                    ShimDecorateRow(row, item, rowIndex++);
                    row.Tracker!.StartTracking(ref _rowTrackingRoot);
                    ItemContainerGenerator.RegisterContainer(item, row);
                    host.Children.Add(row);
                }
            }
            else
            {
                var subgroups = group.Items
                    .Cast<MS.Internal.Data.CollectionViewGroupInternal>()
                    .ToList();
                BuildGroupedRows(host, subgroups, depth + 1, ref rowIndex);
            }
        }
    }

    // Shared per-row visual decoration applied by both the manual builder and the
    // virtualized generation path: alternating background, separator border, style,
    // and selection highlight. PrepareRow / tracker / generator registration are done
    // by the caller (they differ between the two paths).
    internal void ShimDecorateRow(DataGridRow row, object item, int displayIndex)
    {
        row.ShimRowIndex = displayIndex;
        row.ApplyShimRowStyle();
        row.ApplyShimRowBackground();
        // Reflect the engine's selection so the highlight follows the item even across
        // sort / filter / container recycling.
        row.IsSelected = IsRowItemSelected(item);
    }

    // Virtualized path (Slice 7): decorate + register a row as it enters the realized
    // window, and unregister it as it is recycled out, so alternating background,
    // selection visuals, and generator-based container lookup match the manual path.
    internal override void ShimOnContainerRealized(DependencyObject container, object? item, int index)
    {
        if (container is DataGridRow row && item is not null)
        {
            // Session 121 (frozen columns, Slice 4): extend the cells-presenter opt-in to the
            // virtualized row path too. The manual path applies this right after `new
            // DataGridRow()` (before PrepareRow); the virtualized path's row instances come
            // from the linked upstream GetContainerForItemOverride() => new DataGridRow(),
            // which this shim doesn't control, so this realize hook is the equivalent
            // opportunity — still before ApplyTemplate, so the Template swap takes effect on
            // this pass. Idempotent for recycled containers (Template reassignment to the
            // same cached instance is a no-op).
            row.ShimApplyCellsPresenterTemplateIfNeeded(_shimUseCellsPresenter);

            // Ensure the row template (and thus its cells, built in OnApplyTemplate)
            // exists before decorating. The virtualized path realizes the row into the
            // live tree before measure; decorating an un-templated row otherwise leaves
            // it measured at border-only height (cells never contribute).
            row.ApplyTemplate();
            ShimDecorateRow(row, item, index);
            ItemContainerGenerator.RegisterContainer(item, row);
        }
    }

    internal override void ShimOnContainerRecycled(DependencyObject container, object? item)
    {
        ItemContainerGenerator.UnregisterContainer(container);
    }

    // WPF ItemsControl.OnBringItemIntoView hook, driven by ScrollIntoView. For a realized
    // container, bring it into view; otherwise (virtualized off-screen) ask the items host
    // to scroll the item's index into view, which devirtualizes it.
    internal override object? OnBringItemIntoView(ItemInfo info)
    {
        var container = (info.Container ?? ItemContainerGenerator.ContainerFromItem(info.Item))
            as Microsoft.UI.Xaml.FrameworkElement;
        if (container is not null)
        {
            container.StartBringIntoView();
            return null;
        }

        // Resolve the items host via the template part (InternalItemsHost may not be wired
        // yet during template application).
        if (info.Item is not null && GetTemplateChild("PART_ShimRowsHost") is VirtualizingStackPanel host)
        {
            var index = ResolveScrollIndex(info.Item);
            if (index >= 0)
            {
                host.BringIndexIntoView(index);
                // Realize the target window synchronously now, before the async
                // ScrollViewer.ViewChanged can reset the forced viewport.
                host.UpdateLayout();
            }
        }

        return null;
    }

    // Session 121 (DataGrid grouping, Slice 4): VirtualizingStackPanel's grouped
    // realizer virtualizes over the flattened header+leaf "slot" sequence (see
    // EnsureGroupedSlots), not Items directly — a leaf item's slot index is offset
    // by however many GroupHeaderSlot entries precede it. Translates via
    // CollectionViewGroupInternal.SlotIndexFromItem so BringIndexIntoView/
    // ShimForceViewport target the right row even under grouping.
    private int ResolveScrollIndex(object? item)
    {
        if (!IsGrouping)
        {
            return Items.IndexOf(item);
        }

        var offset = 0;
        foreach (var group in Items.Groups)
        {
            var found = group.SlotIndexFromItem(item, offset);
            if (found >= 0)
            {
                return found;
            }

            offset += group.SlotCount;
        }

        return -1;
    }

    internal override object? OnBringItemIntoView(object arg)
        => arg is ItemInfo info ? OnBringItemIntoView(info) : null;

    // Scrolls a (possibly off-screen / not-yet-realized) item into view on the virtualized
    // path, devirtualizing it. Returns true if the item is realized afterwards.
    internal bool ShimScrollItemIntoView(object? item)
    {
        if (item is null || GetTemplateChild("PART_ShimRowsHost") is not VirtualizingStackPanel presenter)
            return false;

        var index = ResolveScrollIndex(item);
        if (index < 0)
            return false;

        presenter.BringIndexIntoView(index);
        presenter.UpdateLayout();
        return ItemContainerGenerator.ContainerFromItem(item) is not null;
    }

    private bool _shimUseHeaderPresenter;

    internal bool ShimUseHeaderPresenter => _shimUseHeaderPresenter;

    // Session 120 (B1 slice 1, exploratory): opt-in swap of the virtualized template's pinned
    // header host from the manual BuildHeaderRow() row to a live DataGridColumnHeadersPresenter.
    // Default off — BuildHeaderRow is unaffected unless this is called. Column resize,
    // drag-reorder, and the session 70-75 live-style/gridline notification batch all still
    // target the manual _headerCells list and are NOT wired to the presenter path yet, so
    // enabling this only proves/renders column headers (content, style, frozen visuals via
    // DataGridHelper.TransferProperty), not the full header feature set.
    internal bool ShimSetHeaderPresenterHost(bool enabled)
    {
        _shimUseHeaderPresenter = enabled;
        if (_shimUseRowsPresenter)
        {
            ShimBuildVirtualizedHeader();
        }

        return enabled;
    }

    // Builds the column-header row into the virtualized template's pinned header host.
    private void ShimBuildVirtualizedHeader()
    {
        if (GetTemplateChild("PART_ShimHeaderHost") is Microsoft.UI.Xaml.Controls.Border headerHost)
        {
            if (_shimUseHeaderPresenter)
            {
                var presenter = _shimHeaderPresenterForRetry
                    ?? new Primitives.DataGridColumnHeadersPresenter();
                // Session 120 (B1, drag-reorder): wrap the presenter in a Grid so the reorder
                // drop-indicator can be a sibling overlay positioned by Margin, instead of being
                // Children.Insert()-ed into the presenter's own DataGridCellsPanel — that panel's
                // children are strictly generator-managed (AddContainerFromGenerator/
                // VirtualizeChildren index math), and an untracked foreign child risks the same
                // fragility class as session 119's Slice 12 finding.
                _headerPresenterOverlay ??= new Microsoft.UI.Xaml.Controls.Grid();
                if (!ReferenceEquals(_headerPresenterOverlay.Children.FirstOrDefault(), presenter))
                {
                    _headerPresenterOverlay.Children.Clear();
                    _headerPresenterOverlay.Children.Add(presenter);
                }
                headerHost.Child = _headerPresenterOverlay;
                // Session 120: VirtualizingPanel.IsVirtualizing is a WPF property-value-inherited
                // attached DP. ShimSetRowVirtualization(true) sets it (true) at the DataGrid/rows
                // level so DataGridRowsPresenter's cells panels virtualize columns correctly, but
                // that inherited value flows down into the pinned header's DataGridCellsPanel too,
                // overriding the type-default-metadata false the presenter's static constructor
                // registers for headers specifically. The header panel then runs cell-level column
                // virtualization keyed off the ROWS' scroll/viewport state, which is meaningless for
                // a non-scrolling pinned header — confirmed live: it realizes 0-1 columns and the
                // realized set churns between measure passes, ending at 0 realized headers. Force a
                // LOCAL value here so it wins over the inherited one.
                VirtualizingPanel.SetIsVirtualizing(presenter, false);
                // The presenter's own (linked upstream) OnApplyTemplate resolves ParentDataGrid
                // via a visual-tree walk and sets ItemsSource/ColumnHeadersPresenter from it, so
                // it must run only after the presenter is parented here — hence the explicit
                // re-apply rather than relying on the Template assignment at construction time.
                presenter.ApplyTemplate();
                _shimHeaderPresenterForRetry = presenter;
            }
            else
            {
                headerHost.Child = BuildHeaderRow();
            }
        }

        ShimHookHeaderScrollSync();
    }

    private Primitives.DataGridColumnHeadersPresenter? _shimHeaderPresenterForRetry;
    private Microsoft.UI.Xaml.Controls.Grid? _headerPresenterOverlay;

    // Session 120 (B1, drag-reorder): the presenter's own generation never runs through the
    // shim's row-realizer path (ShimOnContainerRealized), so nothing else attaches the
    // DataGrid-level pointer handlers that drive interactive column drag-reorder. Called from
    // DataGridColumnHeadersPresenter.PrepareContainerForItemOverride/ClearContainerForItemOverride
    // (ext/wpf, HAS_UNO branch) as headers are realized/cleared.
    //
    // Cached routed-event delegates so AddHandler/RemoveHandler pair by instance.
    private Microsoft.UI.Xaml.Input.PointerEventHandler? _reorderPressed;
    private Microsoft.UI.Xaml.Input.PointerEventHandler? _reorderMoved;
    private Microsoft.UI.Xaml.Input.PointerEventHandler? _reorderReleased;
    private Microsoft.UI.Xaml.Input.PointerEventHandler? _reorderCaptureLost;
    private Microsoft.UI.Xaml.Input.PointerEventHandler ReorderPressed => _reorderPressed ??= OnHeaderPointerPressed;
    private Microsoft.UI.Xaml.Input.PointerEventHandler ReorderMoved => _reorderMoved ??= OnHeaderPointerMoved;
    private Microsoft.UI.Xaml.Input.PointerEventHandler ReorderReleased => _reorderReleased ??= OnHeaderPointerReleased;
    private Microsoft.UI.Xaml.Input.PointerEventHandler ReorderCaptureLost => _reorderCaptureLost ??= OnHeaderPointerCaptureLost;

    internal void ShimHookHeaderReorderHandlers(DataGridColumnHeader header)
    {
        ShimUnhookHeaderReorderHandlers(header);
        // handledEventsToo:true — the header's ControlTemplate / inner TextBlock can mark
        // PointerPressed/Released Handled before a CLR `+=` subscription (which skips handled
        // events) would ever see it. AddHandler with handledEventsToo sees the event regardless.
        header.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, ReorderPressed, true);
        header.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, ReorderMoved, true);
        header.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, ReorderReleased, true);
        header.AddHandler(Microsoft.UI.Xaml.UIElement.PointerCaptureLostEvent, ReorderCaptureLost, true);
        header.PointerExited += OnHeaderPointerExited;
    }

    internal void ShimUnhookHeaderReorderHandlers(DataGridColumnHeader header)
    {
        header.RemoveHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, ReorderPressed);
        header.RemoveHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, ReorderMoved);
        header.RemoveHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, ReorderReleased);
        header.RemoveHandler(Microsoft.UI.Xaml.UIElement.PointerCaptureLostEvent, ReorderCaptureLost);
        header.PointerExited -= OnHeaderPointerExited;
    }

    // Session 120 diagnostic (B1): the header template's DataGridCellsPanel has
    // IsItemsHost="True" set at XAML-parse time (inside ApplyTemplate's template expansion),
    // which is BEFORE the panel is attached under the presenter in the live visual tree.
    // OnIsItemsHostChanged (linked upstream DataGridCellsPanel code) reads ParentPresenter
    // (ItemsControlSpine.GetItemsOwner, a VisualTreeHelper.GetParent walk) synchronously at that
    // moment and gets null, so DataGridColumnHeadersPresenter.InternalItemsHost never gets wired
    // and no headers are ever generated. Unlike VirtualizingStackPanel (which re-resolves its
    // owner on every MeasureOverride), this upstream panel only wires up once, in that one event.
    // Called by a probe (or future real code) after a layout pass confirms the panel is actually
    // parented, to force the event to refire with a resolvable owner. Returns whether the panel
    // was found (diagnostic signal distinct from whether generation then succeeded).
    internal bool ShimRetryHeaderItemsHost()
    {
        if (_shimHeaderPresenterForRetry is not { } presenter)
        {
            return false;
        }

        if (FindVisualDescendant<DataGridCellsPanel>(presenter) is not { } headerCellsPanel)
        {
            return false;
        }

        headerCellsPanel.IsItemsHost = false;
        headerCellsPanel.IsItemsHost = true;
        // IsItemsHostProperty has no AffectsMeasure metadata, so toggling it does not by itself
        // schedule a re-measure; without this, OnIsItemsHostChanged's InternalItemsHost wiring
        // (if it succeeds now) would sit unused until something else happens to invalidate layout.
        headerCellsPanel.InvalidateMeasure();
        return true;
    }

    private static T? FindVisualDescendant<T>(Microsoft.UI.Xaml.DependencyObject root) where T : class
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindVisualDescendant<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    private bool _headerSyncHooked;

    // Session 121 (frozen columns, Slice 3): test/probe seam — GetTemplateChild is protected,
    // and reflecting into it from outside this assembly hit an unrelated generic-method
    // resolution error on this shim's Control hierarchy, so a direct accessor is simpler.
    internal Microsoft.UI.Xaml.Controls.ScrollViewer? ShimGetRowsScrollViewer()
    {
        if (GetTemplateChild("PART_ShimRowsScroll") is Microsoft.UI.Xaml.Controls.ScrollViewer named)
        {
            return named;
        }

        // GetTemplateChild's name-scope lookup is unreliable for ControlTemplates built at
        // runtime via XamlReader.Load (the same class of issue documented elsewhere in this
        // codebase for TemplatedParent) — fall back to a plain visual-tree search.
        return ShimFindVisualDescendant<Microsoft.UI.Xaml.Controls.ScrollViewer>(this);
    }

    internal Microsoft.UI.Xaml.Controls.Border? ShimGetOuterBorder()
        => GetTemplateChild("PART_ShimOuterBorder") as Microsoft.UI.Xaml.Controls.Border;

    private static T? ShimFindVisualDescendant<T>(Microsoft.UI.Xaml.DependencyObject root) where T : class
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (ShimFindVisualDescendant<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    // Keeps the pinned header aligned with horizontal scrolling of the virtualized rows:
    // translate the header host by the rows' horizontal offset.
    private void ShimHookHeaderScrollSync()
    {
        if (_headerSyncHooked)
            return;

        if (GetTemplateChild("PART_ShimRowsScroll") is Microsoft.UI.Xaml.Controls.ScrollViewer scroller
            && GetTemplateChild("PART_ShimHeaderHost") is Microsoft.UI.Xaml.FrameworkElement headerHost)
        {
            scroller.ViewChanged += (_, _) =>
            {
                headerHost.RenderTransform = new Microsoft.UI.Xaml.Media.TranslateTransform
                {
                    X = -scroller.HorizontalOffset,
                };
            };
            _headerSyncHooked = true;
        }
    }

    // Slice 11: filtering now lives on the ItemCollection view (WPF ICollectionView.Filter),
    // so Items itself is the filtered+sorted sequence both render paths read — no parallel
    // realization-view shim. Suppresses the content-changed→rebuild while applying the view.
    private bool _shimSuppressContentRebuild;

    // Pushes the active DataGridExtensions column filters onto the ItemCollection view and
    // refreshes the rendered rows. Items becomes the filtered+sorted source; the virtualized
    // presenter and the manual builder both realize over it directly.
    private void ShimApplyFilterView()
    {
        var state = DataGridExtensions.DataGridFilter.GetState(this);
        var anyActive = state.IsAutoFilterEnabled && state.ColumnFilters.Values.Any(f => f is not null);
        Items.Filter = anyActive
            ? item => DataGridExtensions.DataGridFilter.MatchesAllFilters(this, item)
            : null;

        _shimSuppressContentRebuild = true;
        try
        {
            Items.Refresh();
        }
        finally
        {
            _shimSuppressContentRebuild = false;
        }

        RefreshFilteredRows();
    }

    // Re-realize the virtualized presenter (filter/sort/items changed).
    private void ShimInvalidateRealizationView()
    {
        if (GetTemplateChild("PART_ShimRowsHost") is VirtualizingStackPanel presenter)
        {
            presenter.ShimResetRealization();
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

    // Session 120 (B1 slice 2): under the opt-in header-presenter path, `_headerCells`
    // (populated only by BuildHeaderRow()) stays empty, so the Auto/Star width pass below
    // silently no-ops. Resolve the width-relevant header cells from whichever path is live —
    // generation order in the presenter's registry may not match display order (column
    // reorder), so match by Column reference rather than by index.
    private IReadOnlyList<DataGridColumnHeader> EffectiveHeaderCells()
    {
        if (!_shimUseHeaderPresenter || ColumnHeadersPresenter is not { } presenter)
        {
            return _headerCells;
        }

        var byColumn = new Dictionary<DataGridColumn, DataGridColumnHeader>();
        foreach (var container in presenter.ItemContainerGenerator.Containers)
        {
            if (container is DataGridColumnHeader { Column: { } column } header)
            {
                byColumn[column] = header;
            }
        }

        var result = new List<DataGridColumnHeader>(_visibleColumns.Count);
        foreach (var column in _visibleColumns)
        {
            if (byColumn.TryGetValue(column, out var header))
            {
                result.Add(header);
            }
        }

        return result;
    }

    private void OnAutoWidthLayoutUpdated(object? sender, object e)
    {
        if (!_autoWidthPending)
        {
            return;
        }

        _autoWidthPending = false;

        var headerCells = EffectiveHeaderCells();
        var rows = ItemContainerGenerator.Containers.OfType<DataGridRow>().ToList();
        var widths = new double[_visibleColumns.Count];
        var starWeights = new double[_visibleColumns.Count];
        var fixedTotal = 0.0;
        var totalStar = 0.0;

        // Pass 1: fixed (absolute) + auto (measured) widths, clamped.
        for (var i = 0; i < _visibleColumns.Count && i < headerCells.Count; i++)
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
                w = headerCells[i].DesiredSize.Width;
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

        // Apply to header cells, data cells, filter cells, and column ActualWidth.
        for (var i = 0; i < _visibleColumns.Count && i < headerCells.Count; i++)
        {
            if (widths[i] <= 0)
            {
                continue;
            }

            headerCells[i].Width = widths[i];
            _visibleColumns[i].SetActualWidth(widths[i]);
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
        var headerCells = EffectiveHeaderCells();
        if (i < 0 || i >= headerCells.Count)
            return;

        var w = ShimColumnWidth(column);
        if (double.IsNaN(w) || w <= 0)
            return;

        w = Clamp(column, w);
        headerCells[i].Width = w;
        _visibleColumns[i].SetActualWidth(w);
        foreach (var row in ItemContainerGenerator.Containers.OfType<DataGridRow>())
        {
            if (row.TryGetCell(i) is { } cell)
                cell.Width = w;
        }
        if (i < _filterCells.Count)
            _filterCells[i].Width = w;
    }

    // ── Column resize by header edge drag ────────────────────────────────────
    // WPF's linked DataGridColumnHeader owns gripper event hookup through
    // PART_LeftHeaderGripper / PART_RightHeaderGripper template parts. The
    // Uno-specific bridge is in DataGridColumnCollection.uno.cs, where the
    // upstream resize callback is committed into the shim width path.

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

        var headerCells = EffectiveHeaderCells();
        var width = TextBestFitWidth(column.Header?.ToString());
        if (visibleIndex < headerCells.Count)
        {
            width = Math.Max(width, ElementBestFitWidth(headerCells[visibleIndex]));
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

        if (double.IsNaN(width) || width <= 0)
        {
            return TextBestFitWidth(column.Header?.ToString());
        }

        return DataGridColumnResizeShim.ClampWidth(width, column.MinWidth, column.MaxWidth);
    }

    private static double ElementBestFitWidth(Microsoft.UI.Xaml.FrameworkElement element)
    {
        var textWidth = TextBestFitWidth(ElementText(element));
        if (textWidth > 0)
        {
            return textWidth;
        }

        var desiredWidth = element.DesiredSize.Width;
        if (double.IsNaN(desiredWidth) || desiredWidth <= 0 || !double.IsNaN(element.Width))
        {
            desiredWidth = 0;
        }

        // Best-fit should be based on content, not the width currently assigned
        // by layout or a previous resize. Using ActualWidth/Width here makes a
        // double-click preserve/stretch to the current width instead of
        // shrinking back to content.
        return double.IsNaN(desiredWidth) || desiredWidth <= 0 ? 20 : desiredWidth;
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
        var headerCells = EffectiveHeaderCells();
        if (visibleIndex >= 0 && visibleIndex < headerCells.Count)
        {
            var headerWidth = headerCells[visibleIndex].ActualWidth > 0
                ? headerCells[visibleIndex].ActualWidth
                : headerCells[visibleIndex].Width;
            if (!double.IsNaN(headerWidth) && headerWidth > 0)
            {
                return headerWidth;
            }
        }

        return column.Width.DisplayValue > 0 ? column.Width.DisplayValue : column.Width.Value;
    }

    // Items in display order, with active column filters applied. Sorting is
    // handled by ItemCollection.SortDescriptions (WPF PerformSort path).
    // Items is already the filtered+sorted view (ItemCollection.Filter / SortDescriptions),
    // so the display sequence is just Items; filtering is no longer re-applied here.
    private IEnumerable<object?> OrderedItems()
        => Items.Cast<object?>().ToList();

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
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12,
                MinHeight = 32,
                Padding = new Microsoft.UI.Xaml.Thickness(12, 0, 8, 0),
                Background = DataGridFluentTheme.HeaderBackgroundFor(this),
                Foreground = DataGridFluentTheme.SecondaryTextFor(this),
            };
            headerCell.PrepareColumnHeader(column.Header, column);
            headerCell.Content = HeaderContent(column);
            headerCell.ApplyShimFrozenState();
            headerCell.ApplyShimColumnHeaderStyle();
            headerCell.ApplyShimGridLines();
            // handledEventsToo:true — see ShimHookHeaderReorderHandlers for why the CLR `+=`
            // events miss handled events.
            headerCell.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, ReorderPressed, true);
            headerCell.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, ReorderMoved, true);
            headerCell.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, ReorderReleased, true);
            headerCell.AddHandler(Microsoft.UI.Xaml.UIElement.PointerCaptureLostEvent, ReorderCaptureLost, true);
            headerCell.PointerExited += OnHeaderPointerExited;
            _headerCells.Add(headerCell);
            header.Children.Add(headerCell);

            if (!double.IsNaN(ShimColumnWidth(column)))
            {
                headerCell.Width = ShimColumnWidth(column);
            }

            // Measure header text width directly (header cell has no XAML
            // template so its Measure does not content-size). Use a standalone
            // TextBlock so column.ActualWidth reflects content before the
            // post-layout auto-width pass.
            if (double.IsNaN(ShimColumnWidth(column)) && column.Header is string hdr)
            {
                var tb = new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = hdr,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 14,
                };
                tb.Measure(new global::Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                var hw = Math.Max(tb.DesiredSize.Width + 32, column.MinWidth);
                if (hw > 0)
                {
                    headerCell.Width = hw;
                    column.SetActualWidth(hw);
                }
            }
        }

        _headerHostPanel = header;
        // Session 122: this used to also draw its own 2px light-gray (#D0D0D0)
        // bottom border here — but every header cell already draws its own
        // bottom border via ApplyShimGridLines (real WPF's GridLinesVisibility/
        // HorizontalGridLinesBrush, Black by default), so with the default
        // GridLinesVisibility=All this was a second, different-colored,
        // slightly-Y-offset line drawn directly under the first — the same
        // double-separator artifact fixed for data rows (DataGridRow.cs,
        // PART_RowSeparator removal). No separate separator needed here either;
        // wrapped in a plain (now borderless) Border only to keep this method's
        // return type unchanged for callers.
        return new Microsoft.UI.Xaml.Controls.Border { Child = header };
    }

    // Light gray background shared by the filter row (its own bottom border,
    // drawn below, isn't redundant with anything else — filter cells are plain
    // TextBox/HexBox/Flags controls that don't call ApplyShimGridLines).
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
            ShimApplyFilterView();
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
            ShimApplyFilterView();
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
            ShimApplyFilterView();
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

    // ── Per-column-header filter button + flyout ──────────────────────────────
    // When IsAutoFilterEnabled, each column header gets a "▾" button on the
    // right side. Clicking it opens a flyout with the appropriate filter control
    // (Text, Hex, or Flags). The filter row is no longer rendered.

    // Builds the funnel-icon filter button for a column header.
    // The button is placed at the far-right edge of the header cell by HeaderContent's Grid layout.
    private Microsoft.UI.Xaml.FrameworkElement BuildFilterPanelForColumn(DataGridColumn column)
    {
        var state = DataGridExtensions.DataGridFilter.GetState(this);
        var hasActiveFilter = state.ColumnFilters.TryGetValue(column, out var activeFilter) && activeFilter != null;

        var funnelPath = (Microsoft.UI.Xaml.Shapes.Path)
            Microsoft.UI.Xaml.Markup.XamlReader.Load(
                "<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                "Data='M0,0 L10,0 6,4 6,9 4,9 4,4 Z' " +
                "Fill='Gray' Stretch='Uniform' Width='10' Height='12' " +
                "VerticalAlignment='Center' HorizontalAlignment='Center' />");

        var filterButton = new Microsoft.UI.Xaml.Controls.Button
        {
            Content = funnelPath,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            Padding = new Microsoft.UI.Xaml.Thickness(2),
            MinWidth = 14,
            MinHeight = 14,
            BorderThickness = new Microsoft.UI.Xaml.Thickness(0),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            Opacity = hasActiveFilter ? 1.0 : 0.6,
        };

        // Button and filter input overlap in the same Grid cell, matching WPF HexFilterControl.xaml:
        // icon is always behind; input panel overlays it and is shown/hidden via Visibility.
        var panel = new Microsoft.UI.Xaml.Controls.Grid
        {
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
        };
        panel.Children.Add(filterButton);

        if (DataGridExtensions.DataGridFilterColumn.GetTemplate(column) is DataGridExtensions.FilterControlTemplate fct)
        {
            var filterInput = BuildFilterInlineContent(column, fct);
            filterInput.Margin = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0);
            panel.Children.Add(filterInput);

            // Show input immediately when a filter is already active (e.g. after rebuild).
            if (hasActiveFilter)
            {
                filterButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                filterInput.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }

            filterButton.Click += (_, _) =>
            {
                filterButton.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                filterInput.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                var focusTarget = filterInput as Microsoft.UI.Xaml.Controls.Control
                    ?? (Microsoft.UI.Xaml.Controls.Control?)
                       (filterInput as Microsoft.UI.Xaml.Controls.Panel)?.Children
                       .OfType<Microsoft.UI.Xaml.Controls.TextBox>().FirstOrDefault();
                focusTarget?.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            };

            // When the input loses focus and has no active filter, collapse back to the icon.
            filterInput.LostFocus += (_, _) =>
            {
                var st = DataGridExtensions.DataGridFilter.GetState(this);
                var stillActive = st.ColumnFilters.TryGetValue(column, out var f) && f != null;
                if (!stillActive)
                {
                    filterInput.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    filterButton.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                }
            };
        }

        return panel;
    }

    private Microsoft.UI.Xaml.FrameworkElement BuildFilterInlineContent(
        DataGridColumn column, DataGridExtensions.FilterControlTemplate filterTemplate)
    {
        return filterTemplate.Kind switch
        {
            DataGridExtensions.FilterKind.Hex => BuildHexFilterInline(column),
            DataGridExtensions.FilterKind.Flags => BuildFlagsFilterInline(column, filterTemplate.FlagsType),
            _ => BuildTextFilterInline(column),
        };
    }

    // Text filter inline — an auto-sized TextBox embedded in the header.
    private Microsoft.UI.Xaml.FrameworkElement BuildTextFilterInline(DataGridColumn column)
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
            PlaceholderText = "Filter…",
            MinWidth = 120,
        };
        box.TextChanged += (s, _) =>
        {
            var t = ((Microsoft.UI.Xaml.Controls.TextBox)s!).Text;
            var st = DataGridExtensions.DataGridFilter.GetState(this);
            if (string.IsNullOrEmpty(t))
                st.ColumnFilterText.Remove(column);
            else
                st.ColumnFilterText[column] = t;

            st.ColumnFilters[column] = string.IsNullOrEmpty(t)
                ? null
                : (st.ContentFilterFactory?.Create(t)
                   ?? new DataGridExtensions.SubstringContentFilter(t));
            ShimApplyFilterView();
        };
        return box;
    }

    // Hex filter flyout — "0x" prefix + TextBox.
    private Microsoft.UI.Xaml.FrameworkElement BuildHexFilterInline(DataGridColumn column)
    {
        var state = DataGridExtensions.DataGridFilter.GetState(this);
        var current = (state.ColumnFilters.TryGetValue(column, out var f)
                       ? (f as DataGridExtensions.HexContentFilter)?.Text
                       : null) ?? string.Empty;

        var panel = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
        };

        panel.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock
        {
            Text = "0x",
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 4, 0),
        });

        var box = new Microsoft.UI.Xaml.Controls.TextBox
        {
            Text = current,
            PlaceholderText = "hex…",
        };
        void ApplyFilterText()
        {
            var t = box.Text;
            var st = DataGridExtensions.DataGridFilter.GetState(this);
            st.ColumnFilters[column] = string.IsNullOrEmpty(t)
                ? null
                : new DataGridExtensions.HexContentFilter(t);
            ShimApplyFilterView();
        }

        box.Tag = (Action)ApplyFilterText;
        box.TextChanged += (s, _) =>
        {
            ApplyFilterText();
        };
        panel.Children.Add(box);
        return panel;
    }

    // Flags filter flyout — CheckBox list for each flag value.
    private Microsoft.UI.Xaml.FrameworkElement BuildFlagsFilterInline(DataGridColumn column, Type? flagsType)
    {
        var state = DataGridExtensions.DataGridFilter.GetState(this);
        var currentMask = (state.ColumnFilters.TryGetValue(column, out var f)
                           ? (f as DataGridExtensions.MaskContentFilter)?.Mask
                           : null) ?? -1;

        var content = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            MaxHeight = 300,
            Spacing = 2,
        };

        if (flagsType is null)
            return new Microsoft.UI.Xaml.Controls.TextBlock { Text = "No filter options" };

        var flagItems = new List<(string Name, int Value)>();
        foreach (var field in flagsType.GetFields(
                     System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (field.Name.EndsWith("Mask", StringComparison.Ordinal)) continue;
            int v;
            try { v = Convert.ToInt32(field.GetRawConstantValue()); } catch { continue; }
            flagItems.Add((field.Name, v));
        }

        // "All" / reset checkbox
        var allBox = new Microsoft.UI.Xaml.Controls.CheckBox
        {
            Content = "<All>",
            IsChecked = currentMask == -1,
        };
        content.Children.Add(allBox);

        var perFlagBoxes = new List<(Microsoft.UI.Xaml.Controls.CheckBox cb, int val)>();
        foreach (var (name, val) in flagItems)
        {
            var cb = new Microsoft.UI.Xaml.Controls.CheckBox
            {
                Content = $"{name} ({val:X4})",
                IsChecked = currentMask == -1 || (currentMask & val) != 0,
                Tag = val,
            };
            perFlagBoxes.Add((cb, val));
            content.Children.Add(cb);
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
            st.ColumnFilters[column] = (!anyChecked || allBox.IsChecked == true)
                ? null
                : new DataGridExtensions.MaskContentFilter(mask);
            ShimApplyFilterView();
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

        return new Microsoft.UI.Xaml.Controls.ScrollViewer
        {
            Content = content,
            MaxHeight = 300,
        };
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

    // Session 120: floating drag-header visual (WPF's DataGridColumnFloatingHeader,
    // reimplemented rather than reused — that upstream class paints a live VisualBrush
    // snapshot of the source header, which WinUI/Uno has no equivalent for. This builds a
    // lightweight stand-in instead: a semi-transparent clone of the header's chrome/text
    // that follows the pointer horizontally, hosted in PART_ShimDragOverlay (a top-level
    // Canvas sibling added to both root templates) so it isn't constrained by the header
    // row's own layout (StackPanel/Grid) the way the drop-indicator is.
    private Microsoft.UI.Xaml.Controls.Border? _floatingHeader;
    private double _floatingHeaderStartLeft;

    private Microsoft.UI.Xaml.Controls.Canvas? DragOverlay
        => GetTemplateChild("PART_ShimDragOverlay") as Microsoft.UI.Xaml.Controls.Canvas;

    private void StartFloatingHeader(DataGridColumnHeader header, DataGridColumn column)
    {
        if (DragOverlay is not { } overlay)
            return;

        var transform = header.TransformToVisual(overlay);
        var origin = transform.TransformPoint(new global::Windows.Foundation.Point(0, 0));

        // Session 120: header.ActualHeight/DesiredSize/RenderSize all read (0,0) here even
        // though the header renders at its real size on screen (confirmed via DevFlow's
        // independent bounds probe) — the manual BuildHeaderRow() path only ever sets
        // Width explicitly (ActualWidth mirrors that direct assignment), never Height, and
        // reading the live measured/arranged Height back through these CLR properties
        // doesn't reflect the compositor's actual on-screen size in this environment.
        // Rather than depend on that unreliable read, leave Height unset (Auto) and let the
        // Border size vertically from its own content — a TextBlock reliably self-sizes
        // from its text regardless of any measure-constraint quirk, unlike a snapshot sized
        // to a value that turned out to be 0.
        _floatingHeader = new Microsoft.UI.Xaml.Controls.Border
        {
            Width = header.ActualWidth,
            Padding = new Microsoft.UI.Xaml.Thickness(0, 2, 0, 2),
            Opacity = 0.85,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                global::Windows.UI.Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0)),
            Child = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = column.Header?.ToString() ?? "",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            },
        };

        _floatingHeaderStartLeft = origin.X;
        Microsoft.UI.Xaml.Controls.Canvas.SetLeft(_floatingHeader, origin.X);
        Microsoft.UI.Xaml.Controls.Canvas.SetTop(_floatingHeader, origin.Y);
        Microsoft.UI.Xaml.Controls.Canvas.SetZIndex(_floatingHeader, 100);
        overlay.Children.Add(_floatingHeader);
    }

    private void UpdateFloatingHeader(double deltaX)
    {
        if (_floatingHeader is null)
            return;

        Microsoft.UI.Xaml.Controls.Canvas.SetLeft(_floatingHeader, _floatingHeaderStartLeft + deltaX);
    }

    private void EndFloatingHeader()
    {
        if (_floatingHeader is not null)
        {
            DragOverlay?.Children.Remove(_floatingHeader);
        }

        _floatingHeader = null;
    }

    // Session 120 (B1, drag-reorder): coordinate reference frame for GetCurrentPoint, generalized
    // across the manual header row (a StackPanel) and the header-presenter overlay (a Grid
    // wrapping the presenter) — both are plain UIElements, so either works as a relative-position
    // origin regardless of which one actually hosts the realized header cells.
    private Microsoft.UI.Xaml.UIElement? EffectiveHeaderReferenceElement()
        => _shimUseHeaderPresenter ? _headerPresenterOverlay : _headerHostPanel;

    internal static Action<string>? ReorderLogger;
    private static void ReorderLog(string msg) => ReorderLogger?.Invoke(msg);

    private void OnHeaderPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        ReorderLog($"OnHeaderPointerPressed CALLED sender={sender?.GetType().Name}");
        // Reorder: record candidate but don't capture yet (plain click still sorts).
        var canReorder = CanUserReorderColumns;
        var refElem = EffectiveHeaderReferenceElement();
        var hdr = sender as DataGridColumnHeader;
        var col = hdr?.Column;
        var canColReorder = col?.CanUserReorder ?? false;

        ReorderLog($"  conditions: canReorder={canReorder} refElem={refElem != null} hdr={hdr != null} col={col != null} canColReorder={canColReorder}");

        if (canReorder && refElem is { } reference
            && hdr is not null && col is { CanUserReorder: true })
        {
            _reorderHeader = hdr;
            _reorderColumn = col;
            _reorderStartX = e.GetCurrentPoint(reference).Position.X;
            _reorderActive = false;
            ReorderLog($"  PointerPressed col='{col.Header}' startX={_reorderStartX:F1}");
        }
        else
        {
            ReorderLog($"  PointerPressed IGNORED");
        }
    }

    private void OnHeaderPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_reorderColumn is null || _reorderHeader is null || EffectiveHeaderReferenceElement() is not { } reference)
            return;

        var x = e.GetCurrentPoint(reference).Position.X;
        if (!_reorderActive)
        {
            if (Math.Abs(x - _reorderStartX) <= ReorderDragThreshold)
            {
                ReorderLog($"PointerMoved below threshold x={x:F1} start={_reorderStartX:F1}");
                return;
            }
            _reorderActive = true;
            var captureResult = _reorderHeader.CapturePointer(e.Pointer);
            _reorderHeader.Opacity = 0.5;
            StartFloatingHeader(_reorderHeader, _reorderColumn);
            ReorderLog($"PointerMoved -> reorderActive x={x:F1} captureResult={captureResult}");
        }

        UpdateFloatingHeader(x - _reorderStartX);
        UpdateReorderIndicator(ComputeDropSlot(x, out var offset), offset);
    }

    private void OnHeaderPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_reorderColumn is { } col && _reorderActive && EffectiveHeaderReferenceElement() is { } reference)
        {
            var slot = ComputeDropSlot(e.GetCurrentPoint(reference).Position.X, out _);
            var target = _visibleColumns[Math.Clamp(slot, 0, _visibleColumns.Count - 1)].DisplayIndex;
            if (sender is DataGridColumnHeader hdr)
                hdr.ReleasePointerCapture(e.Pointer);
            EndReorder();
            ReorderLog($"PointerReleased col='{col.Header}' slot={slot} target={target}");
            ShimTryReorderColumn(col, target);
            e.Handled = true;
        }
        else
        {
            ReorderLog($"PointerReleased no-op reorderActive={_reorderActive} col={_reorderColumn?.Header}");
            EndReorder();
        }
    }

    private void OnHeaderPointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Do NOT end the reorder session here. On Uno-Skia-macOS, PointerCaptureLost can fire
        // mid-gesture — before the real PointerReleased that follows it — even though the header
        // keeps receiving that subsequent release just fine. Treating capture-lost as a hard abort
        // reset _reorderActive before the release arrived, turning a real drop into a no-op. Capture
        // loss alone is not a reliable "the gesture ended" signal here; only PointerReleased (or a
        // genuinely new PointerPressed) should end the session.
        ReorderLog($"PointerCaptureLost (ignored) reorderActive={_reorderActive} col={_reorderColumn?.Header}");
    }

    private void OnHeaderPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is DataGridColumnHeader header)
        {
            header.ClearShimCursor();
        }
    }

    // Walk the realized headers (display order) accumulating widths; return the
    // index of the slot whose left half the pointer is over (drop-before), or the
    // count for drop-after-last. dropOffset is the cumulative pixel X of that slot's
    // left edge, used to position the presenter-overlay indicator (see below).
    private int ComputeDropSlot(double x, out double dropOffset)
    {
        var headerCells = EffectiveHeaderCells();
        double offset = AreRowHeadersVisible ? RowHeaderShimWidth : 0;
        for (var i = 0; i < headerCells.Count; i++)
        {
            var w = headerCells[i].ActualWidth > 0 ? headerCells[i].ActualWidth : ShimColumnWidth(_visibleColumns[i]);
            if (x < offset + w / 2)
            {
                dropOffset = offset;
                return i;
            }
            offset += w;
        }

        dropOffset = offset;
        return Math.Max(0, _visibleColumns.Count - 1);
    }

    internal bool ShimTryReorderColumnByHeaderDrag(
        int sourceDisplayIndex,
        double startX,
        double releaseX,
        out int dropSlot,
        out int targetDisplayIndex)
    {
        dropSlot = -1;
        targetDisplayIndex = -1;

        if (!CanUserReorderColumns
            || sourceDisplayIndex < 0
            || sourceDisplayIndex >= _visibleColumns.Count
            || EffectiveHeaderReferenceElement() is null)
        {
            return false;
        }

        if (Math.Abs(releaseX - startX) <= ReorderDragThreshold)
        {
            return false;
        }

        dropSlot = ComputeDropSlot(releaseX, out _);
        targetDisplayIndex = _visibleColumns[Math.Clamp(dropSlot, 0, _visibleColumns.Count - 1)].DisplayIndex;
        var sourceColumn = ColumnFromDisplayIndex(sourceDisplayIndex);
        return ShimTryReorderColumn(sourceColumn, targetDisplayIndex);
    }

    private void UpdateReorderIndicator(int slot, double offset)
    {
        _reorderIndicator ??= new Microsoft.UI.Xaml.Controls.Border
        {
            Width = 2,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                global::Windows.UI.Color.FromArgb(0xFF, 0x00, 0x78, 0xD4)),
        };

        if (_shimUseHeaderPresenter)
        {
            // Overlay mode: the indicator is a Grid sibling of the presenter, positioned by
            // Margin rather than by list order (a single-cell Grid has no "position in the
            // horizontal flow" the way the manual StackPanel's child order provided for free).
            if (_headerPresenterOverlay is not { } overlay)
                return;

            _reorderIndicator.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
            _reorderIndicator.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
            _reorderIndicator.Margin = new Microsoft.UI.Xaml.Thickness(offset, 0, 0, 0);
            if (!overlay.Children.Contains(_reorderIndicator))
                overlay.Children.Add(_reorderIndicator);
        }
        else
        {
            if (_headerHostPanel is not { } panel)
                return;

            var headerCells = EffectiveHeaderCells();
            panel.Children.Remove(_reorderIndicator);
            var panelIndex = (AreRowHeadersVisible ? 1 : 0) + Math.Clamp(slot, 0, headerCells.Count);
            panelIndex = Math.Clamp(panelIndex, 0, panel.Children.Count);
            panel.Children.Insert(panelIndex, _reorderIndicator);
        }
    }

    private void EndReorder()
    {
        if (_reorderHeader is not null)
            _reorderHeader.Opacity = 1.0;
        if (_reorderIndicator is not null)
        {
            _headerPresenterOverlay?.Children.Remove(_reorderIndicator);
            _headerHostPanel?.Children.Remove(_reorderIndicator);
        }
        EndFloatingHeader();
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
        var grid = new Microsoft.UI.Xaml.Controls.Grid();

        // 2-column layout: [* content] [Auto filter button]
        grid.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new Microsoft.UI.Xaml.Controls.ColumnDefinition { Width = Microsoft.UI.Xaml.GridLength.Auto });

        // 2-row layout: [Auto sort indicator] [* text]
        grid.RowDefinitions.Add(new Microsoft.UI.Xaml.Controls.RowDefinition { Height = Microsoft.UI.Xaml.GridLength.Auto });
        grid.RowDefinitions.Add(new Microsoft.UI.Xaml.Controls.RowDefinition { Height = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Star) });

        // Sort indicator at top, centered horizontally
        if (column.SortDirection is { } dir)
        {
            var glyphData = dir == System.ComponentModel.ListSortDirection.Ascending
                ? "M0,5 L4,0 L8,5 Z"
                : "M0,0 L4,5 L8,0 Z";

            var arrowPath = (Microsoft.UI.Xaml.Shapes.Path)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(
                    "<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
                    $"Data='{glyphData}' " +
                    "Fill='Gray' Stretch='Uniform' Width='8' Height='6' " +
                    "HorizontalAlignment='Center' VerticalAlignment='Bottom' " +
                    "Margin='0,1,0,0' />");

            Microsoft.UI.Xaml.Controls.Grid.SetColumn(arrowPath, 0);
            grid.Children.Add(arrowPath);
        }

        // Header text centered
        var textBlock = new Microsoft.UI.Xaml.Controls.TextBlock
        {
            Text = column.Header?.ToString() ?? "",
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
        };
        Microsoft.UI.Xaml.Controls.Grid.SetColumn(textBlock, 0);
        Microsoft.UI.Xaml.Controls.Grid.SetRow(textBlock, 1);
        grid.Children.Add(textBlock);

        // Filter panel at the far right edge, spanning both rows.
        // Contains the funnel icon button (always visible) and the filter input
        // inline (visibility toggled by clicking the button).
        if (DataGridExtensions.DataGridFilter.GetIsAutoFilterEnabled(this) &&
            DataGridExtensions.DataGridFilterColumn.GetTemplate(column) != null)
        {
            var filterPanel = BuildFilterPanelForColumn(column);
            Microsoft.UI.Xaml.Controls.Grid.SetColumn(filterPanel, 1);
            Microsoft.UI.Xaml.Controls.Grid.SetRowSpan(filterPanel, 2);
            grid.Children.Add(filterPanel);
        }

        return grid;
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
    {
        // Filter-view refreshes manage their own row rebuild (header focus preserved);
        // don't let the resulting collection-reset trigger a full rebuild.
        if (_shimSuppressContentRebuild)
            return;
        BuildShimVisualTree();
    }

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
            default:
                // Real WPF wires TextSearch.DoSearch from ItemsControl.OnTextInput (a
                // separate routed event carrying composed/IME text). WinUI's
                // KeyRoutedEventArgs only carries a VirtualKey, so this maps the
                // unmodified-letter/digit subset directly instead of hooking a second
                // event — sufficient for the prefix-search use case (typed names),
                // without pulling in full IME composition handling.
                if (modifiers == Input.ModifierKeys.None && IsTextSearchEnabled)
                {
                    var ch = ShimVirtualKeyToChar(e.Key);
                    if (ch is not null)
                    {
                        TextSearch.EnsureInstance(this)?.DoSearch(ch);
                        e.Handled = true;
                    }
                }
                break;
        }
    }

    private static string? ShimVirtualKeyToChar(global::Windows.System.VirtualKey key)
    {
        var value = (int)key;
        if (key >= global::Windows.System.VirtualKey.A && key <= global::Windows.System.VirtualKey.Z)
        {
            return ((char)value).ToString();
        }

        if (key >= global::Windows.System.VirtualKey.Number0 && key <= global::Windows.System.VirtualKey.Number9)
        {
            return ((char)value).ToString();
        }

        return null;
    }

    internal void MoveSelectionByOffset(int delta)
    {
        if (Items.Count == 0)
        {
            return;
        }

        // Work in item-index space (not just realized rows) so navigation reaches
        // virtualized off-screen rows; the current item comes from the engine selection.
        var current = SelectedItem is not null ? Items.IndexOf(SelectedItem) : -1;
        var target = current < 0
            ? (delta > 0 ? 0 : Items.Count - 1)
            : Math.Clamp(current + delta, 0, Items.Count - 1);

        MoveSelectionToIndex(target);
    }

    // Overrides the spine's focus-only fallback: reuses the real scroll-into-view +
    // row-click selection path (MoveSelectionToIndex), so TextSearch's incremental
    // search realizes and selects a virtualized/off-screen match, not just focuses
    // an already-generated one.
    internal override void NavigateToItem(object? item)
    {
        if (item is null)
        {
            return;
        }

        MoveSelectionToIndex(Items.IndexOf(item));
    }

    internal void MoveSelectionToIndex(int index)
    {
        if (Items.Count == 0)
        {
            return;
        }

        var clamped = Math.Clamp(index, 0, Items.Count - 1);
        var item = Items[clamped];

        // Scroll the target into view (realizes it if virtualized off-screen), then apply
        // row-click selection on the now-realized container; fall back to engine selection.
        OnBringItemIntoView(NewItemInfo(item));
        if (ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row)
        {
            HandleShimRowClicked(row);
        }
        else
        {
            SelectedItem = item;
        }
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
    // dispatch iterates the live header cells directly instead. Session 120
    // (B1 slice 4): use EffectiveHeaderCells() so style/gridline notifications
    // (GridLinesVisibility, CellStyle, ColumnHeaderStyle, RowStyleSelector,
    // etc.) reach the presenter's realized headers too, not just the manual
    // BuildHeaderRow() ones.
    private void ShimNotifyColumnHeaders(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        foreach (var header in EffectiveHeaderCells())
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
