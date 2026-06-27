using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

// Session 77: DataGridColumnHeader now inherits ButtonBase (promoted from
// static class to abstract class). The upstream DataGridColumnHeader.cs is
// linked as a partial — it carries SeparatorBrush/SeparatorVisibility DPs,
// gripper hookup, ContainerTracking, OnClick → PerformSort, and mouse routing.
// This partial provides the HAS_UNO-specific render helpers.
public partial class DataGridColumnHeader : ButtonBase, IProvideDataGridColumn
{
    private const string HeaderTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>" +
        "<Border Background='{TemplateBinding Background}' " +
        "BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>" +
        "<ContentPresenter />" +
        "</Border></ControlTemplate>";

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _headerTemplate;

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

    internal void SetShimCursor()
    {
        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);
    }

    internal void ClearShimCursor()
    {
        ProtectedCursor = null;
    }

    internal void ApplyShimGridLines()
    {
        var owner = Column?.DataGridOwner;
        var visibility = owner?.GridLinesVisibility ?? DataGridGridLinesVisibility.None;
        var horizontal = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Horizontal;
        var vertical = visibility is DataGridGridLinesVisibility.All or DataGridGridLinesVisibility.Vertical;

        if (_headerTemplate == null)
        {
            _headerTemplate = (Microsoft.UI.Xaml.Controls.ControlTemplate)
                Microsoft.UI.Xaml.Markup.XamlReader.Load(HeaderTemplateXaml);
        }

        Template = _headerTemplate;

        BorderThickness = horizontal || vertical
            ? new Microsoft.UI.Xaml.Thickness(0, 0, vertical ? 1 : 0, horizontal ? 1 : 0)
            : new Microsoft.UI.Xaml.Thickness(0);
        BorderBrush = (horizontal || vertical)
            ? (vertical ? owner?.VerticalGridLinesBrush : owner?.HorizontalGridLinesBrush)
            : null;
    }

}
