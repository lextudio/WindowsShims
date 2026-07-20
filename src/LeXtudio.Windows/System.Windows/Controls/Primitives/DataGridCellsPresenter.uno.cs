namespace System.Windows.Controls.Primitives;

// Session 121 (frozen columns, Slice 1): the upstream file is fully linked but was never
// live — nothing gave it an items-hosting panel of its own, so GetContainerForItemOverride/
// PrepareContainerForItemOverride never ran; DataGridRow instead built DataGridCell
// containers by hand (DataGridRow.BuildCells()). Same shape of gap, same fix, as
// DataGridColumnHeadersPresenter (session 120, B1 slice 1): a minimal template giving this
// presenter a DataGridCellsPanel marked IsItemsHost.
//
// DataGridRowOwner (upstream, this class) resolves via DataGridHelper.FindParent<DataGridRow>
// — a visual-tree walk, not TemplatedParent — so it works correctly even though this
// presenter's own Template is built via XamlReader.Load at runtime (TemplatedParent is never
// populated for such templates — the same fact that drove DataGridCellsPanel's
// ParentPresenter fix from GetItemsOwner's TemplatedParent walk to a visual-tree walk).
public partial class DataGridCellsPresenter
{
    private const string CellsPresenterTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
        "xmlns:c='using:System.Windows.Controls'>" +
        "<c:DataGridCellsPanel x:Name='PART_RowCellsPanel' IsItemsHost='True' />" +
        "</ControlTemplate>";

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _presenterTemplate;

    // Unlike DataGridColumnHeadersPresenter (session 120), upstream DataGridCellsPresenter
    // already declares both a parameterless instance constructor and a static constructor
    // (ItemsPanelProperty default-metadata override) — this partial can't add either without
    // a duplicate-member error. Instead, DataGridRow.OnApplyTemplate (the only place this
    // presenter gets instantiated, declaratively via CellsPresenterRowTemplateXaml) sets
    // Template on it explicitly via this helper, right after retrieving it via
    // GetTemplateChild, then calls ApplyTemplate() — same net effect, different hook point.
    internal static void ShimEnsureTemplate(DataGridCellsPresenter presenter)
    {
        _presenterTemplate ??= (Microsoft.UI.Xaml.Controls.ControlTemplate)
            Microsoft.UI.Xaml.Markup.XamlReader.Load(CellsPresenterTemplateXaml);
        presenter.Template = _presenterTemplate;
    }
}
