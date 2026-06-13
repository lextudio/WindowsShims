using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace System.Windows.Controls;

public partial class DataGridRow : Control
{
    public static readonly DependencyProperty DetailsVisibilityProperty =
        DependencyProperty.Register(nameof(DetailsVisibility), typeof(Visibility),
            typeof(DataGridRow), new PropertyMetadata(Visibility.Collapsed));

    internal static readonly DependencyPropertyKey IsNewItemPropertyKey =
        DependencyProperty.RegisterReadOnly("IsNewItem", typeof(bool), typeof(DataGridRow),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsNewItemProperty = IsNewItemPropertyKey.DependencyProperty;

    public object? Item { get; set; }

    private bool _isEditing;

    public bool IsEditing
    {
        get => _isEditing;
        internal set
        {
            if (_isEditing == value)
            {
                return;
            }

            _isEditing = value;
            RefreshRowHeaderGlyph();
        }
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            UpdateSelectionVisual();
        }
    }

    public bool IsNewItem
    {
        get => (bool)GetValue(IsNewItemProperty);
        internal set => SetValue(IsNewItemPropertyKey, value);
    }

    public Visibility DetailsVisibility
    {
        get => (Visibility)GetValue(DetailsVisibilityProperty);
        set => SetValue(DetailsVisibilityProperty, value);
    }

    internal DataGrid? DataGridOwner { get; set; }

    public Style? ShimAppliedRowStyle { get; private set; }

    internal DataGridCellsPresenter? CellsPresenter { get; set; }

    internal DataGridDetailsPresenter? DetailsPresenter { get; set; }

    internal DataGridRowHeader? RowHeader { get; set; }

    internal BindingGroup? BindingGroup { get; set; }

    internal ContainerTracking<DataGridRow>? Tracker { get; set; }

    // Session 69: row index within the rendered set (0-based), used to stripe
    // alternating row backgrounds without the WPF AlternationIndex attached property.
    internal int ShimRowIndex { get; set; }

    internal bool DetailsLoaded { get; set; }

    // WPF UIElement.Focus() has no FocusState; route to programmatic focus.
    public bool Focus() => Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

    public void BringIntoView() => StartBringIntoView();

    internal void PrepareRow(object item, DataGrid dataGrid)
    {
        Item = item;
        DataGridOwner = dataGrid;
        DataContext = item;
        IsNewItem =
            ReferenceEquals(item, System.Windows.Data.CollectionView.NewItemPlaceholder) ||
            ReferenceEquals(item, DataGrid.NewItemPlaceholder) ||
            ReferenceEquals(item, dataGrid.Items.CurrentAddItem);
        // Initialize the tracker so the upstream DataGrid notification chain
        // (DataGrid.NotifyPropertyChanged → _rowTrackingRoot → row) can reach
        // this container. BuildShimVisualTree calls StartTracking after PrepareRow.
        Tracker ??= new ContainerTracking<DataGridRow>(this);
        // If the template is already applied (row reused), rebuild now.
        BuildCells();
    }

    internal void ClearRow(DataGridRow oldContainer, DataGrid dataGrid) { }
    internal void ClearRow(DataGrid dataGrid) { }

    internal void ScrollCellIntoView(DataGridColumn column) { }
    internal void ScrollCellIntoView(int columnIndex) { }

    internal DataGridCell? TryGetCell(int index)
        => index >= 0 && index < _cells.Count ? _cells[index] : null;

    // ── Session 26: the row hosts its own cells ──────────────────────────────
    // DataGridRow is the real visual container: its template hosts a
    // PART_CellsHost panel, and the row builds one DataGridCell per visible
    // column (each cell's content produced by the column, bound to the item).

    // Session 57: the row template is now vertical — the cells row sits above a
    // PART_DetailsHost that expands to host the materialized RowDetailsTemplate.
    private const string RowTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='{TemplateBinding Background}' " +
        "BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>" +
        "<StackPanel Orientation='Vertical'>" +
        "<StackPanel Orientation='Horizontal'>" +
        "<ContentControl x:Name='PART_RowHeader' />" +
        "<StackPanel x:Name='PART_CellsHost' Orientation='Horizontal' />" +
        "</StackPanel>" +
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
    private static readonly Microsoft.UI.Xaml.Media.Brush _selectedBrush =
        new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(0xFF, 0xCC, 0xE8, 0xFF));

    // Session 69: apply the stripe background (RowBackground or AlternatingRowBackground).
    // Called from BuildShimVisualTree after ShimRowIndex is set, and from
    // DataGridRow.NotifyPropertyChanged when grid.RowBackground / AlternatingRowBackground changes.
    internal void ApplyShimRowBackground()
    {
        if (!_isSelected)
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
        Background = _isSelected ? _selectedBrush : DataGridOwner?.ShimRowBackground(ShimRowIndex);
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
                    new DataGridRowDetailsEventArgs(this, _detailsPresenter?.DetailsElement!));
            }
        }
    }

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _rowTemplate;

    private readonly List<DataGridCell> _cells = new();

    protected override void InitializeDefaultStyleKey()
    {
        if (Template is not null)
        {
            return;
        }

        try
        {
            _rowTemplate ??= (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(RowTemplateXaml);
            Template = _rowTemplate;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DataGridRow] template load failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        BuildCells();
        UpdateSelectionVisual();
    }

    // Pointer press selects this row through the owner (shim single-select).
    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        DataGridOwner?.HandleShimRowClicked(this, e.KeyModifiers);
    }

    private void BuildCells()
    {
        if (GetTemplateChild("PART_CellsHost") is not Microsoft.UI.Xaml.Controls.Panel host)
        {
            return;
        }

        if (DataGridOwner is not { } owner || Item is not { } item)
        {
            return;
        }

        host.Children.Clear();
        _cells.Clear();

        foreach (var column in owner.ColumnsInDisplayOrder())
        {
            if (!column.IsVisible)
            {
                continue;
            }

            var cell = new DataGridCell
            {
                Column = column,
                RowOwner = this,
                Width = owner.ShimColumnWidth(column),
                Margin = new Microsoft.UI.Xaml.Thickness(4, 2, 4, 2),
            };
            cell.BuildVisualTree();
            cell.ApplyShimFrozenState();
            cell.ApplyShimCellStyle();
            cell.ApplyShimGridLines();
            // Re-apply a retained cell selection to the rebuilt cell instance.
            owner.TryReselectCell(cell);
            _cells.Add(cell);
            host.Children.Add(cell);
        }

        BuildRowHeader(owner);
        BuildRowDetails(owner);
    }

    // ── Session 57: row details ──────────────────────────────────────────────
    // The row materializes the grid's RowDetailsTemplate into PART_DetailsHost
    // when the effective visibility (mode + selection + template + real item)
    // resolves to Visible, reusing the linked WPF Loading/Unloading wrappers.
    private DataGridDetailsPresenter? _detailsPresenter;

    // Effective details visibility, mirroring the upstream
    // OnCoerceDetailsVisibility switch over RowDetailsVisibilityMode.
    private Visibility ComputeDetailsVisibility(DataGrid owner)
    {
        var hasTemplate = owner.RowDetailsTemplate is not null || owner.RowDetailsTemplateSelector is not null;
        var isRealItem =
            !ReferenceEquals(Item, System.Windows.Data.CollectionView.NewItemPlaceholder) &&
            !ReferenceEquals(Item, DataGrid.NewItemPlaceholder);

        return owner.RowDetailsVisibilityMode switch
        {
            DataGridRowDetailsVisibilityMode.Collapsed => Visibility.Collapsed,
            DataGridRowDetailsVisibilityMode.Visible =>
                hasTemplate && isRealItem ? Visibility.Visible : Visibility.Collapsed,
            DataGridRowDetailsVisibilityMode.VisibleWhenSelected =>
                _isSelected && hasTemplate && isRealItem ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed,
        };
    }

    private void BuildRowDetails(DataGrid owner)
    {
        if (GetTemplateChild("PART_DetailsHost") is not Microsoft.UI.Xaml.Controls.ContentControl host)
        {
            return;
        }

        var visibility = ComputeDetailsVisibility(owner);
        DetailsVisibility = visibility;

        if (visibility != Visibility.Visible)
        {
            // Tearing down a previously-loaded details section raises Unloading.
            if (_detailsPresenter is not null && DetailsLoaded)
            {
                owner.OnUnloadingRowDetailsWrapper(this);
            }

            host.Content = null;
            host.Visibility = Visibility.Collapsed;
            _detailsPresenter = null;
            DetailsPresenter = null;
            return;
        }

        var content = owner.RowDetailsTemplate?.LoadContent() as FrameworkElement;
        _detailsPresenter = new DataGridDetailsPresenter
        {
            ParentDataGrid = owner,
            DataContext = Item,
            Content = content,
        };
        DetailsPresenter = _detailsPresenter;
        host.Content = _detailsPresenter;
        host.Visibility = Visibility.Visible;

        // The template just expanded, so DetailsElement is available — reuse the
        // linked WPF wrapper to raise LoadingRowDetails exactly once.
        owner.OnLoadingRowDetailsWrapper(this);
    }

    // ── Session 49: row header ───────────────────────────────────────────────
    private DataGridRowHeader? _rowHeaderElement;

    private void BuildRowHeader(DataGrid owner)
    {
        if (GetTemplateChild("PART_RowHeader") is not Microsoft.UI.Xaml.Controls.ContentControl host)
        {
            return;
        }

        if (!owner.AreRowHeadersVisible)
        {
            host.Content = null;
            host.Width = 0;
            _rowHeaderElement = null;
            return;
        }

        host.Width = owner.RowHeaderShimWidth;
        _rowHeaderElement = new DataGridRowHeader
        {
            ParentDataGrid = owner,
            ParentRow = this,
            Width = owner.RowHeaderShimWidth,
            HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
        };
        _rowHeaderElement.ApplyShimGridLines();
        host.Content = _rowHeaderElement;
        RefreshRowHeaderGlyph();
    }

    // Glyph priority: validation error > editing > current/selected row.
    private void RefreshRowHeaderGlyph()
    {
        if (_rowHeaderElement is null)
        {
            return;
        }

        _rowHeaderElement.Content =
            HasRowValidationError ? "⚠" :  // ⚠
            _isEditing ? "✎" :             // ✎
            _isSelected ? "▶" :            // ▶
            string.Empty;
    }

    // The upstream DataGrid.UpdateColumnsOnRows calls this signature; the shim
    // lets BuildShimVisualTree (triggered by Columns.CollectionChanged) own the
    // full rebuild so a per-row cells-only rebuild is unnecessary here.
    protected internal virtual void OnColumnsChanged(
        System.Collections.ObjectModel.ObservableCollection<DataGridColumn> columns,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }

    internal void NotifyPropertyChanged(
        DependencyObject dependencyObject,
        string propertyName,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target)
    {
        // Session 69: row-level property changes (background striping).
        if (DataGridHelper.ShouldNotifyRows(target))
        {
            if (args.Property == DataGrid.RowBackgroundProperty
                || args.Property == DataGrid.AlternatingRowBackgroundProperty)
            {
                ApplyShimRowBackground();
            }
            else if (args.Property == DataGrid.RowStyleProperty
                || args.Property == DataGrid.RowStyleSelectorProperty)
            {
                ApplyShimRowStyle();
            }
        }

        // Forward cell-targeting notifications to each realized cell. The
        // upstream would route through DataGridCellsPresenter; the shim routes
        // directly through the _cells backing list.
        if (DataGridHelper.ShouldNotifyCells(target)
            || DataGridHelper.ShouldNotifyCellsPresenter(target)
            || DataGridHelper.ShouldRefreshCellContent(target))
        {
            foreach (var cell in _cells)
            {
                cell.NotifyPropertyChanged(dependencyObject, propertyName, args, target);
            }
        }
    }

    // WPF UIElement.MoveFocus; routes to keyboard navigation.
    public bool MoveFocus(Input.TraversalRequest request) => false;

    public bool IsVisible => Visibility == Visibility.Visible;
}
