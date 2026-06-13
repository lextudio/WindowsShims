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

    public bool IsSelected { get; set; }

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

    public void BringIntoView() { }

    internal void PrepareRow(object item, DataGrid dataGrid)
    {
        Item = item;
        DataGridOwner = dataGrid;
    }

    internal void ClearRow(DataGridRow oldContainer, DataGrid dataGrid) { }
    internal void ClearRow(DataGrid dataGrid) { }

    internal void ScrollCellIntoView(DataGridColumn column) { }
    internal void ScrollCellIntoView(int columnIndex) { }

    internal DataGridCell? TryGetCell(int index) => null;

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
