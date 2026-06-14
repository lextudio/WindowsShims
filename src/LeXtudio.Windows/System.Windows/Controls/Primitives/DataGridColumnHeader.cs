using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

// Session 77: DataGridColumnHeader now inherits ButtonBase (promoted from
// static class to abstract class). The upstream DataGridColumnHeader.cs is
// linked as a partial — it carries SeparatorBrush/SeparatorVisibility DPs,
// gripper hookup, ContainerTracking, OnClick → PerformSort, and mouse routing.
// This partial provides the HAS_UNO-specific overrides and the Column setter.
public partial class DataGridColumnHeader : ButtonBase, IProvideDataGridColumn
{
    // Session 77: upstream _column field is included from the linked file.
    // Expose a setter so BuildShimVisualTree can wire up the column association.
    public DataGridColumn? Column
    {
        get => _column;
        internal set => _column = value;
    }

    // DisplayIndex and IsFrozen are now read-only DPs from the upstream file.
    // IsFrozen: upstream declares IsFrozenPropertyKey — use it in the setter.

    public bool IsVisible => Visibility == Visibility.Visible;

    public bool HasShimGridLine =>
        BorderThickness.Left > 0 || BorderThickness.Right > 0 ||
        BorderThickness.Top > 0 || BorderThickness.Bottom > 0;

    public Style? ShimAppliedColumnHeaderStyle { get; private set; }

    // ── Shim-specific helpers ─────────────────────────────────────────────────

    internal void ApplyShimFrozenState()
    {
        bool frozen = Column is { DataGridOwner: { } owner } column
            ? column.DisplayIndex < owner.FrozenColumnCount
            : Column?.IsFrozen == true;
        SetValue(IsFrozenPropertyKey.DependencyProperty, frozen);
        Opacity = frozen ? 0.96 : 1.0;
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

        BorderThickness = horizontal || vertical
            ? new Microsoft.UI.Xaml.Thickness(0, 0, vertical ? 1 : 0, horizontal ? 1 : 0)
            : new Microsoft.UI.Xaml.Thickness(0);
        BorderBrush = (horizontal || vertical)
            ? (vertical ? owner?.VerticalGridLinesBrush : owner?.HorizontalGridLinesBrush)
            : null;
    }

    // Session 67 / 68: notification chain dispatch from DataGrid.ShimNotifyColumnHeaders.
    // The upstream NotifyPropertyChanged uses DataGridHelper.TransferProperty which is
    // WPF-only; the shim version handles the subset meaningful in the shim render path.
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
}
