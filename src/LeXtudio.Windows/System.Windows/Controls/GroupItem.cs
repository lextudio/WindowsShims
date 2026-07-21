namespace System.Windows.Controls;

// Session 121 (DataGrid grouping, Slice 2/4, extended Slice 5): WPF's generic
// ItemsControl group container. There is no DataGrid-specific row-group-header
// class upstream to link (see docs/session121.md) — real WPF reuses this same
// GroupItem, with GroupStyle supplying ContainerStyle/HeaderTemplate/Panel.
// Panel (custom items layout per group) is deliberately not shimmed — see
// GroupStyle.cs's class comment. ContainerStyle/ContainerStyleSelector,
// HeaderTemplate/HeaderTemplateSelector, and HeaderStringFormat are (Slice 5);
// when none of these resolve anything, this falls back to Slice 2's original
// fixed header (group name + item count, indented per nesting level).
public partial class GroupItem : ContentControl
{
    internal MS.Internal.Data.CollectionViewGroupInternal? ShimGroup { get; private set; }

    // Session 121 (DataGrid grouping, Slice 4): set by whoever prepares this header
    // (DataGrid.BuildGroupedRows for the manual path, VirtualizingStackPanel's
    // grouped realizer for the virtualized path) to that path's existing
    // "re-derive the realized view" entry point — BuildShimVisualTree() or
    // ShimResetRealization(), respectively — so a tap toggling ShimGroup.IsExpanded
    // takes effect without either path needing new invalidation machinery.
    internal Action? ShimToggleGroupExpansion { get; set; }

    private bool _shimToggleHooked;

    internal void ShimPrepareGroupHeader(MS.Internal.Data.CollectionViewGroupInternal group, int depth, ItemsControl? owner = null)
    {
        ShimGroup = group;

        if (!_shimToggleHooked)
        {
            Tapped += OnShimHeaderTapped;
            _shimToggleHooked = true;
        }

        var style = ResolveGroupStyle(owner, group, depth);

        var containerStyle = style?.ContainerStyleSelector?.SelectStyle(group, this) ?? style?.ContainerStyle;
        if (containerStyle is not null)
        {
            Style = containerStyle;
        }

        var headerTemplate = style?.HeaderTemplateSelector?.SelectTemplate(group, this) ?? style?.HeaderTemplate;
        if (headerTemplate is not null)
        {
            // A real template defines its own visuals — don't fight it with the
            // fixed-header fallback's hardcoded background/padding/font below.
            // Expand/collapse indication for a templated header is the template's
            // own responsibility (matching real WPF's Expander-in-template
            // convention) — only the fixed fallback below renders one itself.
            ContentTemplate = headerTemplate;
            Content = group;
            return;
        }

        var disclosure = group.IsExpanded ? "▾" : "▸"; // ▾ expanded / ▸ collapsed
        // HeaderStringFormat "arises only when no template is available" (matching
        // upstream's own doc comment) — {0} is Name, {1} ItemCount. The disclosure
        // marker is this shim's own addition (not part of upstream's fixed header),
        // so it's still prefixed even with a custom format.
        var body = style?.HeaderStringFormat is { } format
            ? string.Format(format, group.Name, group.ItemCount)
            : $"{group.Name} ({group.ItemCount})";
        Content = $"{disclosure} {body}";
        Padding = new Microsoft.UI.Xaml.Thickness(8 + depth * 16, 4, 8, 4);
        Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(0xFF, 0xE5, 0xE5, 0xE5));
        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
    }

    private void OnShimHeaderTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (ShimGroup is null)
        {
            return;
        }

        ShimGroup.IsExpanded = !ShimGroup.IsExpanded;
        ShimToggleGroupExpansion?.Invoke();
    }

    // Internal (not private) so CollectionViewGroupBuilder can resolve the same
    // per-depth style to honor HidesIfEmpty at build/flatten time — the one other
    // place in this shim that needs to know a group's effective GroupStyle.
    internal static GroupStyle? ResolveGroupStyle(ItemsControl? owner, System.Windows.Data.CollectionViewGroup? group, int depth)
    {
        if (owner is null)
        {
            return null;
        }

        // Real WPF's own fallback order: GroupStyleSelector wins when it returns
        // a non-null style; otherwise fall back to the indexed GroupStyle collection.
        if (owner.GroupStyleSelector is { } selector && group is not null)
        {
            if (selector(group, depth) is { } selected)
            {
                return selected;
            }
        }

        var styles = owner.GroupStyle;
        return styles.Count == 0 ? null : styles[Math.Min(depth, styles.Count - 1)];
    }
}
