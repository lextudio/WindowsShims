namespace System.Windows.Controls.Primitives;

// Session 120 (B1 slice 1, exploratory): the upstream file is fully linked (session 94)
// but was never live — nothing gave it an items-hosting panel of its own, so
// GetContainerForItemOverride/PrepareContainerForItemOverride never ran. Unlike the row
// virtualization work (DataGridRowsPresenter is a Panel that piggybacks on DataGrid's own,
// already-proven ItemsControl generation via GetItemsOwner), this presenter IS the
// ItemsControl and must generate its own containers over its own ItemsSource
// (DataGridColumnHeaderCollection) — a capability never previously exercised in this shim.
// This partial gives it a minimal template so it can try: a DataGridCellsPanel marked
// IsItemsHost, matching the pattern used for DataGridRowsPresenter/DataGridCellsPresenter.
public partial class DataGridColumnHeadersPresenter
{
    // DataGridCellsPanel.ParentPresenter used to rely on a two-hop TemplatedParent walk (real
    // WPF's ItemsPresenter indirection), but TemplatedParent is never populated for
    // ControlTemplates built at runtime via XamlReader.Load — confirmed live via
    // roma.probe.metadata-header-presenter (panelTemplatedParentType always null). Fixed at the
    // source (DataGridCellsPanel.cs, #elif HAS_UNO branch) to use the already-proven
    // ItemsControlSpine.GetItemsOwner visual-tree walk instead, so a single IsItemsHost panel
    // directly in this template (no ItemsPresenter stand-in needed) is enough.
    // DataGridCellsPanel lives in System.Windows.Controls (not .Primitives).
    private const string HeaderPresenterTemplateXaml =
        "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
        "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
        "xmlns:c='using:System.Windows.Controls'>" +
        "<c:DataGridCellsPanel x:Name='PART_HeaderCellsPanel' IsItemsHost='True' />" +
        "</ControlTemplate>";

    private static Microsoft.UI.Xaml.Controls.ControlTemplate? _presenterTemplate;

    // The shim ItemsControl derives from the real Microsoft.UI.Xaml.Controls.ItemsControl,
    // so there is no InitializeDefaultStyleKey virtual to hook (that's a shim-only Control
    // pattern used by e.g. DataGridRow); set the template directly in the constructor instead,
    // matching how DataGrid itself assigns its ControlTemplate.
    public DataGridColumnHeadersPresenter()
    {
        _presenterTemplate ??= (Microsoft.UI.Xaml.Controls.ControlTemplate)
            Microsoft.UI.Xaml.Markup.XamlReader.Load(HeaderPresenterTemplateXaml);
        Template = _presenterTemplate;
    }
}
