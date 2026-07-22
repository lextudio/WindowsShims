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
    // Session 122 (v7-toolkit-style adoption, slice 1): real WPF's own linked
    // DataGridColumnHeader.ChangeVisualState (ext/wpf's DataGridColumnHeader.cs,
    // unguarded — genuinely runs today via ButtonBase's PointerEntered/Exited ->
    // UpdateVisualState() wiring) already calls VisualStateManager.GoToState with
    // "Normal"/"MouseOver"/"Pressed" (CommonStates) — it was a silent no-op purely
    // because this template never declared matching VisualStateGroups. Adding
    // CommonStates here doesn't touch any C#; it just lets already-linked,
    // already-running upstream logic become visible. Hover/press tint (a
    // HoverRectangle overlay) is adapted from WindowsCommunityToolkit v7's
    // DataGrid.xaml (vendored for reference at ext/wct-v7) using a plain literal
    // color instead of its ThemeResource lookup, since that key isn't defined
    // anywhere in this shim's resource tree.
    //
    // No SortStates group here: this.SortDirection driving a VisualState was the
    // original plan (matching real WPF's own ChangeVisualState, which also has a
    // "SortStates" group with "Unsorted"/"SortAscending"/"SortDescending"), but
    // DataGrid.HeaderContent(column) (DataGrid.cs) already builds a real, working
    // sort-direction arrow (a Path glyph, above the header text) independently,
    // reading column.SortDirection directly at content-build time — every sort
    // already shows it correctly, with no VSM involved. Adding a second,
    // VSM-driven arrow here (as an initial attempt did) duplicated it — two
    // triangles, one legitimate (above text) and one redundant (beside text).
    // Removed; kept only the hover/press treatment, which had no pre-existing
    // equivalent.
    private const string HeaderTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
        "xmlns:primitives='using:System.Windows.Controls.Primitives'>" +
        "<Border x:Name='HeaderBorder' Background='{TemplateBinding Background}' " +
        "BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'>" +
        "<VisualStateManager.VisualStateGroups>" +
        "<VisualStateGroup x:Name='CommonStates'>" +
        "<VisualState x:Name='Normal' />" +
        "<VisualState x:Name='MouseOver'>" +
        "<VisualState.Setters><Setter Target='HoverRectangle.Fill' Value='{ThemeResource SubtleFillColorSecondaryBrush}' /></VisualState.Setters>" +
        "</VisualState>" +
        "<VisualState x:Name='Pressed'>" +
        "<VisualState.Setters><Setter Target='HoverRectangle.Fill' Value='{ThemeResource SubtleFillColorTertiaryBrush}' /></VisualState.Setters>" +
        "</VisualState>" +
        "</VisualStateGroup>" +
        "</VisualStateManager.VisualStateGroups>" +
        "<Grid>" +
        "<Grid.ColumnDefinitions>" +
        "<ColumnDefinition Width='8' />" +
        "<ColumnDefinition Width='*' />" +
        "<ColumnDefinition Width='8' />" +
        "</Grid.ColumnDefinitions>" +
        "<Rectangle x:Name='HoverRectangle' Grid.ColumnSpan='3' Fill='Transparent' />" +
        "<primitives:Thumb x:Name='PART_LeftHeaderGripper' Grid.Column='0' Background='Transparent' />" +
        "<ContentPresenter Grid.Column='1' VerticalAlignment='Center' />" +
        "<primitives:Thumb x:Name='PART_RightHeaderGripper' Grid.Column='2' Background='Transparent' />" +
        "</Grid>" +
        "</Border></ControlTemplate>";

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _headerTemplate;

    // Session 122: root cause of the sort glyph never appearing (see docs/session121.md).
    // Upstream DataGridColumnHeader.cs calls the unqualified `CoerceValue(SortDirectionProperty)`
    // to pull SortDirection from the owning column, expecting real WPF coercion semantics.
    // This shim's `CoerceValue(DependencyProperty)` is a no-op *everywhere* in the codebase
    // (Control.cs, ContentControl.cs, ButtonBase.cs, FrameworkElement.cs, and the shared
    // WinUIDependencyObjectExtensions.CoerceValue extension all declare it as an empty body) —
    // WinUI's native DependencyProperty has no coercion concept at all, so nothing was ever
    // wired to actually invoke a registered CoerceValueCallback.
    //
    // Fixed *here only* (hiding ButtonBase's no-op for this one control), rather than fixing
    // the shared no-op project-wide: there are ~25 other CoerceValueCallback registrations
    // across the linked DataGrid/DataGridColumn/DataGridCell/DataGridRow files that have never
    // actually run in this shim; flipping coercion on globally as a side effect of fixing one
    // sort glyph risks reactivating a couple dozen previously-inert code paths at once, with no
    // way to verify all of them are safe in one pass. A real fix, scoped to exactly the one
    // call site that needed it.
    //
    // FrameworkPropertyMetadata (System.Windows/FrameworkPropertyMetadata.cs) is a subclass of
    // native Microsoft.UI.Xaml.PropertyMetadata that additionally carries CoerceValueCallback —
    // since it's the literal object instance passed to DependencyProperty.RegisterReadOnly(...),
    // DependencyProperty.GetMetadata(Type) hands back that same instance, callback included.
    internal new void CoerceValue(DependencyProperty property)
    {
        if (property.GetMetadata(GetType()) is not FrameworkPropertyMetadata { CoerceValueCallback: { } coerce })
        {
            return;
        }

        var current = GetValue(property);
        var coerced = coerce(this, current);
        if (!Equals(coerced, current))
        {
            SetValue(property, coerced);
        }
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
        MinHeight = 32;
        var themeElement = owner is Microsoft.UI.Xaml.FrameworkElement ownerElement
            ? ownerElement
            : this;
        Background ??= DataGridFluentTheme.HeaderBackgroundFor(themeElement);
        Foreground = DataGridFluentTheme.SecondaryTextFor(themeElement);
        // Session 122: force the template to actually materialize now (VisualStateGroups
        // included) rather than relying on the framework's own lazy application timing.
        // PrepareColumnHeader (upstream, ext/wpf) calls CoerceValue(SortDirectionProperty)
        // right after this in the SAME method — that coercion's PropertyChangedCallback
        // (ButtonBase.OnVisualStatePropertyChanged -> UpdateVisualState -> ChangeVisualState
        // -> VisualStateManager.GoToState) needs a template that already has its
        // VisualStateGroups instantiated to find "SortAscending" etc.; without this call,
        // ApplyTemplate happens later (via the natural layout pass), by which point nothing
        // re-fires the state transition — the sort glyph silently never appears despite
        // SortDirection being coerced correctly. Idempotent for repeat calls.
        ApplyTemplate();
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
