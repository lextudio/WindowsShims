namespace System.Windows.Controls.Primitives;

// Uno-specific partial for the linked upstream DataGridRowHeader. The upstream
// file supplies the full WPF behavior (content/width/selection coercion, click
// selection, visual-state machine); this partial only adds the shim grid-line
// border the Uno render path draws in place of WPF's template chrome.
public partial class DataGridRowHeader
{
    public bool HasShimGridLine { get; private set; }

    // The upstream ParentRow walks the visual tree, which may not yet resolve to
    // the owning DataGridRow when the header is built or when grid-line/content
    // notifications fire. DataGridRow records itself here as a reliable fallback.
    private DataGridRow? _shimOwnerRow;

    internal void SetShimOwnerRow(DataGridRow row) => _shimOwnerRow = row;

    internal DataGridRow? EffectiveRow => ParentRow ?? _shimOwnerRow;

    internal void ApplyShimGridLines()
    {
        var owner = EffectiveRow?.DataGridOwner;
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
