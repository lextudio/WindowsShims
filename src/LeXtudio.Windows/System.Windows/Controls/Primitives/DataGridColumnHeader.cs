using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

// Session 77: DataGridColumnHeader now inherits ButtonBase (promoted from
// static class to abstract class). The upstream DataGridColumnHeader.cs is
// linked as a partial — it carries SeparatorBrush/SeparatorVisibility DPs,
// gripper hookup, ContainerTracking, OnClick → PerformSort, and mouse routing.
// This partial provides the HAS_UNO-specific render helpers.
public partial class DataGridColumnHeader : ButtonBase, IProvideDataGridColumn
{
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

}
