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

    public bool IsEditing { get; internal set; }

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

    internal DataGridCellsPresenter? CellsPresenter { get; set; }

    internal DataGridDetailsPresenter? DetailsPresenter { get; set; }

    internal DataGridRowHeader? RowHeader { get; set; }

    internal BindingGroup? BindingGroup { get; set; }

    internal ContainerTracking<DataGridRow>? Tracker { get; set; }

    internal bool DetailsLoaded { get; set; }

    // WPF UIElement.Focus() has no FocusState; route to programmatic focus.
    public bool Focus() => Focus(Microsoft.UI.Xaml.FocusState.Programmatic);

    public void BringIntoView() => StartBringIntoView();

    internal void PrepareRow(object item, DataGrid dataGrid)
    {
        Item = item;
        DataGridOwner = dataGrid;
        DataContext = item;
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

    private const string RowTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='{TemplateBinding Background}'>" +
        "<StackPanel x:Name='PART_CellsHost' Orientation='Horizontal' />" +
        "</Border></ControlTemplate>";

    // Selection highlight (WinUI list-accent-ish light blue).
    private static readonly Microsoft.UI.Xaml.Media.Brush _selectedBrush =
        new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(0xFF, 0xCC, 0xE8, 0xFF));

    private void UpdateSelectionVisual()
    {
        // Row-level selection tints the row; cells stay transparent so the
        // tint shows through. Cell-level selection (SelectionUnit.Cell) paints
        // the cell itself and is managed separately on DataGridCell.
        Background = _isSelected ? _selectedBrush : null;
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

        foreach (var column in owner.Columns)
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
            // Re-apply a retained cell selection to the rebuilt cell instance.
            owner.TryReselectCell(cell);
            _cells.Add(cell);
            host.Children.Add(cell);
        }
    }

    internal void OnColumnsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { }

    internal void NotifyPropertyChanged(
        DependencyObject dependencyObject,
        string propertyName,
        DependencyPropertyChangedEventArgs args,
        DataGridNotificationTarget target) { }

    // WPF UIElement.MoveFocus; routes to keyboard navigation.
    public bool MoveFocus(Input.TraversalRequest request) => false;

    public bool IsVisible => Visibility == Visibility.Visible;
}
