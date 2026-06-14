namespace System.Windows.Controls.Primitives;

// Uno-specific partial for the linked upstream DataGridDetailsPresenter. The
// upstream file supplies the full WPF behavior (content/template coercion,
// click-to-select, notification propagation); this partial only adds the
// owner-row fallback the visual-tree lookup needs under WinUI.
public partial class DataGridDetailsPresenter
{
    // Upstream DataGridRowOwner walks the visual tree (DataGridHelper.FindParent),
    // which may not resolve when DataGridRow builds and syncs the presenter before
    // it is live in the tree (WinUI raises no OnVisualParentChanged). DataGridRow
    // records itself here as a reliable fallback.
    private DataGridRow? _shimOwnerRow;

    internal void SetShimOwnerRow(DataGridRow row) => _shimOwnerRow = row;

    internal DataGridRow? EffectiveRow => DataGridHelper.FindParent<DataGridRow>(this) ?? _shimOwnerRow;
}
