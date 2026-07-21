using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace System.Windows.Controls;

// Session 78: DataGridRow upstream partial linked. The upstream file provides:
//   - IsSelected DP (Selector.IsSelectedProperty.AddOwner) + Selected/Unselected events
//   - IsEditing read-only DP
//   - IsNewItem read-only DP
//   - DataGridOwner property (backed by _owner)
//   - Tracker property (backed by _tracker)
//   - GetRowContainingElement static helper
//   - Instance constructor: _tracker = new ContainerTracking<DataGridRow>(this)
// This partial provides HAS_UNO-specific implementation: manual cell/detail building,
// shim styling helpers, and local DP registrations that the upstream version can't
// compile under Uno (callbacks referencing Dispatcher, coerce infrastructure, etc.).
public partial class DataGridRow : Control
{
    // ── DPs not brought from upstream (callbacks reference WPF-only APIs) ────

    public static readonly DependencyProperty DetailsVisibilityProperty =
        DependencyProperty.Register(nameof(DetailsVisibility), typeof(Visibility),
            typeof(DataGridRow), new PropertyMetadata(Visibility.Collapsed));

    // ── Local-only properties ─────────────────────────────────────────────────

    public Visibility DetailsVisibility
    {
        get => (Visibility)GetValue(DetailsVisibilityProperty);
        set => SetValue(DetailsVisibilityProperty, value);
    }

    public Style? ShimAppliedRowStyle { get; private set; }

    internal System.Windows.Data.BindingGroup? BindingGroup { get; set; }

    // Session 69: row index within the rendered set (0-based), for striping.
    internal int ShimRowIndex { get; set; }

    // WPF UIElement.Focus() — route to programmatic focus.
    public bool Focus() => Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

    public void BringIntoView() => StartBringIntoView();

    internal void ScrollCellIntoView(DataGridColumn column) { }

    // ── Session 26: the row hosts its own cells ──────────────────────────────
    // DataGridRow is the real visual container: its template hosts a
    // PART_CellsHost panel, and the row builds one DataGridCell per visible
    // column (each cell's content produced by the column, bound to the item).

    // Session 57: the row template is now vertical — the cells row sits above a
    // PART_DetailsHost that expands to host the materialized RowDetailsTemplate.
    // Session 120 added a fixed-height child element (PART_RowSeparator, a plain
    // Border with an explicit Height so it survives the virtualized path's
    // infinite-width-constraint BorderThickness-collapses-to-zero-height quirk)
    // as a guaranteed-visible row-bottom line, at a time when per-cell gridline
    // rendering (DataGridCell.ApplyShimGridLines, driven by the real WPF
    // GridLinesVisibility/HorizontalGridLinesBrush DPs) wasn't yet reliable.
    // Session 122: that per-cell mechanism is now correct and reliably applied
    // (every cell BuildCells() creates calls ApplyShimGridLines) — so every row
    // was drawing its bottom separator TWICE: once per-cell, using the real
    // GridLinesVisibility color (Black by default) and honoring GridLinesVisibility,
    // and a second time via this always-on, GridLinesVisibility-agnostic 1px
    // light-gray (#D0D0D0) Border layered immediately below the cells. The two
    // lines don't perfectly coincide in Y (independent elements, independent
    // layout rounding), producing a visible "double line, slightly offset,
    // different color" artifact rather than one clean separator. Removed —
    // matches real WPF, which has no such extra always-on element; the cell
    // gridlines alone are the row separator, exactly as GridLinesVisibility
    // governs them.
    private const string RowTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='{TemplateBinding Background}' " +
        "BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>" +
        "<StackPanel Orientation='Vertical'>" +
        "<Grid>" +
        "<Grid.ColumnDefinitions><ColumnDefinition Width='Auto'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>" +
        "<ContentControl x:Name='PART_RowHeader' Grid.Column='0' " +
        "HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch' />" +
        "<StackPanel x:Name='PART_CellsHost' Grid.Column='1' Orientation='Horizontal' />" +
        "</Grid>" +
        "<ContentControl x:Name='PART_DetailsHost' Visibility='Collapsed' />" +
        "</StackPanel></Border></ControlTemplate>";

    // Session 121 (frozen columns, Slice 1): same shape as RowTemplateXaml, except
    // PART_CellsHost is a live DataGridCellsPresenter (self-populating via the ambient
    // DataGridRowOwner/Item wiring in its own OnApplyTemplate) instead of a plain StackPanel
    // BuildCells() populates by hand. Opt-in via DataGrid.ShimSetCellsPresenterHost — default
    // off, so this template is only used once a caller asks for it.
    private const string CellsPresenterRowTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
        "xmlns:p='using:System.Windows.Controls.Primitives'>" +
        "<Border Background='{TemplateBinding Background}' " +
        "BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>" +
        "<StackPanel Orientation='Vertical'>" +
        "<Grid>" +
        "<Grid.ColumnDefinitions><ColumnDefinition Width='Auto'/><ColumnDefinition Width='Auto'/></Grid.ColumnDefinitions>" +
        "<ContentControl x:Name='PART_RowHeader' Grid.Column='0' " +
        "HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch' />" +
        "<p:DataGridCellsPresenter x:Name='PART_CellsHost' Grid.Column='1' />" +
        "</Grid>" +
        "<ContentControl x:Name='PART_DetailsHost' Visibility='Collapsed' />" +
        "</StackPanel></Border></ControlTemplate>";

    // ── Session 48: row-level validation indicator ──────────────────────────
    internal bool HasRowValidationError { get; private set; }

    internal string? RowValidationError { get; private set; }

    internal void SetRowError(string? error)
    {
        HasRowValidationError = true;
        RowValidationError = error;
        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(0xFF, 0xCC, 0x00, 0x00));
        BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
        RefreshRowHeaderGlyph();
    }

    internal void ClearRowError()
    {
        if (!HasRowValidationError)
        {
            return;
        }

        HasRowValidationError = false;
        RowValidationError = null;
        BorderBrush = null;
        BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        RefreshRowHeaderGlyph();
    }

    // Selection highlight (WinUI list-accent-ish light blue).
    private static Microsoft.UI.Xaml.Media.Brush SelectedBrush => DataGridFluentTheme.Selection;

    // Session 69: apply the stripe background (RowBackground or AlternatingRowBackground).
    // Called from BuildShimVisualTree after ShimRowIndex is set, and from
    // DataGridRow.NotifyPropertyChanged when grid.RowBackground / AlternatingRowBackground changes.
    internal void ApplyShimRowBackground()
    {
        if (!IsSelected)
            Background = DataGridOwner?.ShimRowBackground(ShimRowIndex);
    }

    internal void ApplyShimRowStyle()
    {
        if (DataGridOwner is not { } owner)
        {
            ShimAppliedRowStyle = null;
            return;
        }

        ShimAppliedRowStyle =
            owner.RowStyle ??
            owner.ItemContainerStyle ??
            (owner.RowStyleSelector ?? owner.ItemContainerStyleSelector)?.SelectStyle(Item!, this);
    }

    private void UpdateSelectionVisual()
    {
        // Row-level selection tints the row; cells stay transparent so the
        // tint shows through. Cell-level selection (SelectionUnit.Cell) paints
        // the cell itself and is managed separately on DataGridCell.
        Background = IsSelected ? SelectedBrush : DataGridOwner?.ShimRowBackground(ShimRowIndex);
        RefreshRowHeaderGlyph();

        // VisibleWhenSelected: selection toggles the details section. Recompute
        // and, if the effective visibility changed, raise RowDetailsVisibilityChanged.
        if (DataGridOwner is { } owner
            && owner.RowDetailsVisibilityMode == DataGridRowDetailsVisibilityMode.VisibleWhenSelected)
        {
            var before = DetailsVisibility;
            BuildRowDetails(owner);
            if (DetailsVisibility != before)
            {
                owner.OnRowDetailsVisibilityChanged(
                    new DataGridRowDetailsEventArgs(this, DetailsPresenter?.DetailsElement!));
            }
        }
    }

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _rowTemplate;

    protected override void InitializeDefaultStyleKey()
    {
        if (_rowTemplate == null)
        {
            _rowTemplate = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(RowTemplateXaml);
        }

        Template = _rowTemplate;
        // Session 121 (frozen columns, Slice 1): ShimApplyCellsPresenterTemplateIfNeeded
        // (below) may reassign Template again right after construction, once DataGridOwner
        // is known — this method runs from the base Control constructor, before PrepareRow
        // sets DataGridOwner, so the presenter-vs-manual choice can't be made here.

        // WinUI DP change hooks for IsSelected and IsEditing.
        // AddOwner is a no-op shim so upstream OnIsSelectedChanged never fires;
        // replicate its two effects here: update visuals + raise Selected/Unselected.
        RegisterPropertyChangedCallback(IsSelectedProperty, (sender, dp) =>
        {
            UpdateSelectionVisual();
            var row = (DataGridRow)sender;
            bool sel = row.IsSelected;
            row.RaiseEvent(new RoutedEventArgs(sel ? SelectedEvent : UnselectedEvent, row));
        });
        RegisterPropertyChangedCallback(IsEditingProperty, (_, __) => RefreshRowHeaderGlyph());
        PointerEntered += (_, _) =>
        {
            if (!IsSelected)
                Background = DataGridFluentTheme.RowHover;
        };
        PointerExited += (_, _) =>
        {
            if (!IsSelected)
                Background = DataGridOwner?.ShimRowBackground(ShimRowIndex);
        };
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Session 121 (frozen columns, Slice 1): under the presenter-hosted template,
        // PART_CellsHost is a live DataGridCellsPresenter that self-populates via its own
        // ambient DataGridRowOwner/Item wiring (upstream OnApplyTemplate) once it's parented
        // and templated — BuildCells()'s manual per-cell construction only applies to the
        // default (StackPanel) template.
        if (GetTemplateChild("PART_CellsHost") is Primitives.DataGridCellsPresenter presenter)
        {
            Primitives.DataGridCellsPresenter.ShimEnsureTemplate(presenter);
            // Session 121 (frozen columns, Slice 1): same IsVirtualizing-inheritance issue
            // session 120 root-caused for the pinned header presenter — VirtualizingPanel.
            // IsVirtualizing is a property-value-inherited attached DP; without a local
            // value here, an ancestor's inherited value (or the type-default racing it) can
            // put this panel on the wrong generation branch. Force Standard/non-virtualizing
            // locally, matching the header presenter's fix, since row cells are already fully
            // enumerated by DataGridRow (one row = one item; no windowing concept here).
            VirtualizingPanel.SetIsVirtualizing(presenter, false);
            presenter.ApplyTemplate();

            // Session 121 (frozen columns, Slice 1): same IsItemsHost timing bug session 120
            // hit for the header presenter (ShimRetryHeaderItemsHost) — IsItemsHost="True" in
            // PART_RowCellsPanel's template XAML sets the shim's Panel.IsItemsHost DP at
            // template-expansion time, before the panel is attached under the presenter, so
            // OnIsItemsHostChanged's one-shot ParentPresenter/GetItemsOwner call sees null and
            // never wires InternalItemsHost. Toggle it off/on now that the panel is confirmed
            // parented (ApplyTemplate above already ran), and force a re-measure since
            // IsItemsHostProperty has no AffectsMeasure metadata.
            if (FindVisualDescendant<DataGridCellsPanel>(presenter) is { } cellsPanel)
            {
                cellsPanel.IsItemsHost = false;
                cellsPanel.IsItemsHost = true;
                cellsPanel.InvalidateMeasure();
            }
        }
        else
        {
            BuildCells();
        }
    }

    private static T? FindVisualDescendant<T>(Microsoft.UI.Xaml.DependencyObject root) where T : class
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
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

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _cellsPresenterRowTemplate;

    // Session 121 (frozen columns, Slice 1): called right after construction (before
    // PrepareRow/BuildCells run), once DataGridOwner.ShimUseCellsPresenter is knowable —
    // InitializeDefaultStyleKey (runs from the base Control constructor) always applies the
    // default manual-cells template first, since DataGridOwner isn't set yet at that point.
    internal void ShimApplyCellsPresenterTemplateIfNeeded(bool useCellsPresenter)
    {
        if (useCellsPresenter)
        {
            _cellsPresenterRowTemplate ??= (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(CellsPresenterRowTemplateXaml);
            Template = _cellsPresenterRowTemplate;
        }
        else if (ReferenceEquals(Template, _cellsPresenterRowTemplate))
        {
            // Session 121 (frozen columns, Slice 4): symmetric toggle-back for recycled
            // containers (VirtualizationMode.Recycling) — a row instance realized while the
            // cells presenter was enabled, then recycled and re-prepared after it's been
            // disabled, must not keep the presenter template. Otherwise this branch is a
            // no-op (default template from InitializeDefaultStyleKey already applies).
            Template = _rowTemplate;
        }
    }


    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        // Row-level click is handled by cells; no additional routing needed here.
    }

    private List<DataGridCell> _cells = new();

    internal void BuildCells()
    {
        if (DataGridOwner == null || Item == null)
            return;
        if (GetTemplateChild("PART_CellsHost") is not Microsoft.UI.Xaml.Controls.StackPanel host)
            return;

        host.Children.Clear();
        _cells.Clear();

        foreach (var column in DataGridOwner.ColumnsInDisplayOrder())
        {
            if (column.Visibility != Visibility.Visible)
                continue;

            var cell = new DataGridCell { Column = column };
            cell.SetOwnerRow(this);

            // Parent BEFORE BuildVisualTree() assigns Content/DataContext below —
            // see DataGridCell.BuildVisualTree()'s comment for the actual root cause
            // (DataContext never reaches the generated element via inheritance;
            // fixed there by setting it explicitly). Parenting first at least gives
            // the natural, non-reentrant measure cascade a template to flow through.
            host.Children.Add(cell);
            _cells.Add(cell);

            cell.BuildVisualTree();
            DataGridOwner.TryReselectCell(cell);
            cell.Width = DataGridOwner.ShimColumnWidth(column);
            cell.ApplyShimGridLines();
        }

        BuildRowDetails(DataGridOwner);
        BuildRowHeader(DataGridOwner);
    }

    internal void ShimNotifyCells(
        DependencyObject dependencyObject,
        string propertyName,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target)
    {
        foreach (var cell in EffectiveCells())
        {
            cell.NotifyPropertyChanged(dependencyObject, propertyName, args, target);
        }
    }

    internal DataGridCell? ShimTryGetCell(int index)
        => (uint)index < (uint)_cells.Count ? _cells[index] : null;

    // Session 121 (frozen columns, Slice 2): realized cells regardless of which
    // path built them — real upstream TryGetCell(int) already does this same
    // CellsPresenter-first check for index-based lookups (DataGrid.cs's width/
    // resize call sites all use it), but a couple of call sites (style/gridline
    // notification batch) need the full realized set, not one index at a time.
    internal IEnumerable<DataGridCell> EffectiveCells()
        => CellsPresenter is { } presenter
            ? presenter.ItemContainerGenerator.Containers.OfType<DataGridCell>()
            : _cells;

    private Visibility ComputeDetailsVisibility(DataGrid owner)
    {
        return owner.RowDetailsVisibilityMode switch
        {
            DataGridRowDetailsVisibilityMode.Visible => Visibility.Visible,
            DataGridRowDetailsVisibilityMode.VisibleWhenSelected => IsSelected ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed,
        };
    }

    private void BuildRowDetails(DataGrid owner)
    {
        DetailsVisibility = ComputeDetailsVisibility(owner);

        if (GetTemplateChild("PART_DetailsHost") is not Microsoft.UI.Xaml.Controls.ContentControl host)
            return;

        var visibility = DetailsVisibility;
        host.Visibility = visibility;

        if (visibility == Visibility.Visible && (owner.RowDetailsTemplate != null || owner.RowDetailsTemplateSelector != null))
        {
            var presenter = new DataGridDetailsPresenter();
            presenter.SetShimOwnerRow(this);
            DetailsPresenter = presenter;
            host.Content = presenter;
            // SyncProperties sets Content = Item and transfers the details template.
            // If the selector returned a WPF template bridge, SyncProperties stores it
            // on the presenter rather than setting ContentTemplate.
            presenter.SyncProperties();
            if (presenter.ShimTemplateBridge is { } bridge)
            {
                // Bridge factory — build the element directly and bypass WinUI template mechanism.
                presenter.Content = bridge.LoadContent(Item, presenter);
                presenter.ShimTemplateBridge = null;
            }
            else if (presenter.ShimContentFactory is { } factory)
            {
                // Legacy C# factory path.
                presenter.Content = factory(Item);
                presenter.ShimContentFactory = null;
            }
            else
            {
                // Normal DataTemplate path — WinUI ContentPresenter applies ContentTemplate.
                // Set DataContext so {Binding} expressions inside the template can resolve.
                presenter.DataContext = Item;
            }
            DetailsLoaded = false;
            owner.OnLoadingRowDetailsWrapper(this);
        }
        else
        {
            DetailsPresenter = null;
            host.Content = null;
        }
    }

    private DataGridRowHeader? _rowHeaderElement;

    private void BuildRowHeader(DataGrid owner)
    {
        if (GetTemplateChild("PART_RowHeader") is not Microsoft.UI.Xaml.Controls.ContentControl host)
            return;

        if (owner.HeadersVisibility.HasFlag(DataGridHeadersVisibility.Row))
        {
            // ParentRow is resolved from the visual tree by the upstream header
            // (DataGridHelper.FindParent), so the header must be parented first.
            _rowHeaderElement = new DataGridRowHeader();
            _rowHeaderElement.SetShimOwnerRow(this);
            RowHeader = _rowHeaderElement;
            host.Content = _rowHeaderElement;
            host.Visibility = Visibility.Visible;
            _rowHeaderElement.ApplyShimGridLines();
            // Pull Header content/width down from the row + grid (the upstream
            // OnApplyTemplate path doesn't run without a default header template).
            _rowHeaderElement.SyncProperties();
        }
        else
        {
            _rowHeaderElement = null;
            RowHeader = null;
            host.Content = null;
            host.Visibility = Visibility.Collapsed;
        }

        RefreshRowHeaderGlyph();
    }

    private void RefreshRowHeaderGlyph()
    {
        if (_rowHeaderElement == null)
            return;

        if (HasRowValidationError)
            _rowHeaderElement.Content = "⚠";
        else if (IsEditing)
            _rowHeaderElement.Content = "✎";
        else if (IsSelected)
            _rowHeaderElement.Content = "▶";
        else
            _rowHeaderElement.Content = null;
    }

    // WPF UIElement.MoveFocus; routes to keyboard navigation.
    public bool MoveFocus(Input.TraversalRequest request) => false;

    public bool IsVisible => Visibility == Visibility.Visible;
}
