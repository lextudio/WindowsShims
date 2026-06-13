using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

public partial class DataGridColumnHeader : ContentControl, IProvideDataGridColumn
{
    public DataGridColumn? Column { get; internal set; }

    public int DisplayIndex => Column?.DisplayIndex ?? -1;

    public bool IsFrozen { get; private set; }

    public bool HasShimGridLine { get; private set; }

    public Style? ShimAppliedColumnHeaderStyle { get; private set; }

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
        else if (e.Property == DataGridColumn.IsFrozenProperty || e.Property == DataGrid.FrozenColumnCountProperty)
            ApplyShimFrozenState();
        else if (e.Property == DataGrid.ColumnHeaderStyleProperty || e.Property == DataGridColumn.HeaderStyleProperty)
            ApplyShimColumnHeaderStyle();
    }

    internal void ApplyShimFrozenState()
    {
        IsFrozen = Column is { DataGridOwner: { } owner } column
            ? column.DisplayIndex < owner.FrozenColumnCount
            : Column?.IsFrozen == true;
        Opacity = IsFrozen ? 0.96 : 1.0;
    }

    internal void ApplyShimColumnHeaderStyle()
    {
        ShimAppliedColumnHeaderStyle = Column?.HeaderStyle ?? Column?.DataGridOwner?.ColumnHeaderStyle;
    }

    internal void ApplyShimGridLines()
    {
        var owner = Column?.DataGridOwner;
        var visibility = owner?.GridLinesVisibility ?? DataGridGridLinesVisibility.None;
        var horizontal = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Horizontal;
        var vertical = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Vertical;

        HasShimGridLine = horizontal || vertical;
        BorderThickness = HasShimGridLine
            ? new Microsoft.UI.Xaml.Thickness(0, 0, vertical ? 1 : 0, horizontal ? 1 : 0)
            : new Microsoft.UI.Xaml.Thickness(0);
        BorderBrush = HasShimGridLine
            ? (vertical ? owner?.VerticalGridLinesBrush : owner?.HorizontalGridLinesBrush)
            : null;
    }
}
