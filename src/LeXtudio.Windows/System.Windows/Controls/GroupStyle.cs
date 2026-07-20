namespace System.Windows.Controls;

// Session 121 (DataGrid grouping, Slice 4): minimal subset of WPF's GroupStyle —
// upstream's is not linked (drags ItemsPanelTemplate/StyleSelector plumbing this
// shim doesn't have). Only the two properties GroupItem.ShimPrepareGroupHeader
// actually consumes: HeaderTemplate (native WinUI DataTemplate, since GroupItem's
// base ContentControl is the real Microsoft.UI.Xaml.Controls.ContentControl — no
// WPF DataTemplate shim needed) and ContainerStyle. Real WPF's Panel/selectors/
// HidesIfEmpty are not shimmed; a caller supplying only HeaderTemplate/ContainerStyle
// gets a real, working per-level header customization, which covers the common case.
public class GroupStyle
{
    public Microsoft.UI.Xaml.DataTemplate? HeaderTemplate { get; set; }

    public Microsoft.UI.Xaml.Style? ContainerStyle { get; set; }
}
