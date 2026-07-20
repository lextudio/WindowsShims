using System.Windows.Controls;

namespace System.Windows.Controls.Primitives;

// Session 77: DataGridColumnHeader now inherits ButtonBase (promoted from
// static class to abstract class). The upstream DataGridColumnHeader.cs is
// linked as a partial — it carries SeparatorBrush/SeparatorVisibility DPs,
// gripper hookup, ContainerTracking, OnClick → PerformSort, and mouse routing.
// This partial provides the HAS_UNO-specific render helpers.
public partial class DataGridColumnHeader : ButtonBase, IProvideDataGridColumn
{
    // Session 120 (header hit-test investigation): WinUI does not hit-test a Border whose
    // Background is null — only opaque child content (e.g. rendered glyphs) would receive
    // pointer input, and even that isn't reliable across the header's full clickable area.
    // Real WPF's default header theme style always paints a Background, giving the whole
    // cell a hit-test surface; this shim's minimal template had none. The two gripper Thumbs
    // already work around this locally (explicit Background='Transparent'); the same needs
    // to apply to the header's own Border so PointerPressed/Click reach it at all. Falls back
    // to Transparent only when the caller hasn't set an explicit Background (TemplateBinding
    // still wins once Background is actually set, e.g. via ColumnHeaderStyle).
    private const string HeaderTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
        "xmlns:primitives='using:System.Windows.Controls.Primitives'>" +
        "<Border Background='{TemplateBinding Background}' " +
        "BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>" +
        "<Grid>" +
        "<Grid.ColumnDefinitions>" +
        "<ColumnDefinition Width='8' />" +
        "<ColumnDefinition Width='*' />" +
        "<ColumnDefinition Width='8' />" +
        "</Grid.ColumnDefinitions>" +
        "<primitives:Thumb x:Name='PART_LeftHeaderGripper' Grid.Column='0' Background='Transparent' />" +
        "<ContentPresenter Grid.Column='1' />" +
        "<primitives:Thumb x:Name='PART_RightHeaderGripper' Grid.Column='2' Background='Transparent' />" +
        "</Grid>" +
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

    // Session 120 (header hit-test investigation): this was the only place that assigned
    // Template, and it was called exclusively from the manual BuildHeaderRow() path.
    // Presenter-generated headers (new DataGridColumnHeader() from
    // DataGridColumnHeadersPresenter.GetContainerForItemOverride) never got a Template at
    // all — no Border, no gripper Thumbs, no hit-test surface beyond whatever WinUI's own
    // default Control chrome happens to provide — which is why clicks/drags on
    // presenter-hosted headers weren't reaching OnClick/OnPointerPressed. Now also called
    // from PrepareColumnHeader (shared by both paths — see the HAS_UNO addition there) so
    // every header gets the template applied regardless of which path constructs it.
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
        // WinUI does not hit-test a Border/Control whose Background is null; the two
        // gripper Thumbs in the template already work around this locally
        // (Background='Transparent'). TemplateBinding still wins once a real Background
        // is set later (e.g. via ColumnHeaderStyle), since this only fills in when unset.
        Background ??= new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        BorderThickness = horizontal || vertical
            ? new Microsoft.UI.Xaml.Thickness(0, 0, vertical ? 1 : 0, horizontal ? 1 : 0)
            : new Microsoft.UI.Xaml.Thickness(0);
        BorderBrush = (horizontal || vertical)
            ? (vertical ? owner?.VerticalGridLinesBrush : owner?.HorizontalGridLinesBrush)
            : null;
    }

}
