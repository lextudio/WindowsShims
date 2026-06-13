using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

public partial class DataGridColumnHeader : ContentControl, IProvideDataGridColumn
{
    public DataGridColumn? Column { get; internal set; }

    public int DisplayIndex => Column?.DisplayIndex ?? -1;

    public bool IsVisible => Visibility == Visibility.Visible;

    // Pointer press routes to the owning grid's sort handler (session 30).
    protected override void OnPointerPressed(Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Column is { DataGridOwner: { } owner } column)
        {
            owner.HandleShimHeaderClicked(column);
        }
    }

    // Session 67: column-header notification chain. The upstream
    // DataGridColumnHeadersPresenter routes column-property changes here via the
    // ContainerTracking<DataGridColumnHeader> linked list; the shim routes
    // directly from DataGrid.ShimNotifyColumnHeaders.
    internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridColumn col && !ReferenceEquals(col, Column))
            return;
        if (e.Property == DataGridColumn.WidthProperty)
            Width = Column?.DataGridOwner?.ShimColumnWidth(Column) ?? double.NaN;
        else if (e.Property == DataGridColumn.HeaderProperty || e.Property == DataGridColumn.SortDirectionProperty)
            Content = Column?.DataGridOwner?.HeaderContent(Column) ?? Column?.Header;
        else if (e.Property == DataGridColumn.VisibilityProperty)
            Visibility = Column?.Visibility ?? Visibility.Visible;
    }
}
