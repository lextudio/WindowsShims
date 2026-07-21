namespace System.Windows.Controls;

// Session 121 (DataGrid grouping, Slice 4, extended Slice 5): subset of WPF's
// GroupStyle covering everything meaningful for this shim's flattened,
// single-list-host row virtualization. HeaderTemplate/ContainerStyle are native
// WinUI DataTemplate/Style (GroupItem's base ContentControl already *is*
// Microsoft.UI.Xaml.Controls.ContentControl — no WPF DataTemplate/Style shim
// needed); ContainerStyleSelector/HeaderTemplateSelector reuse the same real,
// linked StyleSelector/DataTemplateSelector types RowDetailsTemplateSelector
// already proves work. HeaderStringFormat/HidesIfEmpty are plain shim-only logic
// (GroupItem.ShimPrepareGroupHeader / CollectionViewGroupBuilder respectively).
//
// Not shimmed: Panel (ItemsPanelTemplate) and AlternationCount. Real WPF's Panel
// lets each group's children render through a custom nested items panel; this
// shim's DataGrid grouping instead flattens groups into the single row-host list
// DataGridRowsPresenter/the manual StackPanel already virtualizes (see Slice 3),
// which every other feature in this arc (frozen columns, cell editing, column
// virtualization) depends on. Hosting a distinct nested ItemsControl+Panel per
// group would mean rows for different groups living in different visual
// subtrees — a genuine architectural rewrite of row hosting, not a GroupStyle
// gap — so it's a deliberate, documented scope cut rather than an oversight.
public class GroupStyle
{
    public Microsoft.UI.Xaml.DataTemplate? HeaderTemplate { get; set; }

    public DataTemplateSelector? HeaderTemplateSelector { get; set; }

    public Microsoft.UI.Xaml.Style? ContainerStyle { get; set; }

    public StyleSelector? ContainerStyleSelector { get; set; }

    // Applied only when neither HeaderTemplate nor HeaderTemplateSelector
    // produces a template (i.e. the fixed "{name} ({count})" fallback header) —
    // matches upstream's "This arises only when no template is available."
    // {0} is the group's Name, {1} its ItemCount.
    public string? HeaderStringFormat { get; set; }

    // When true, a group with ItemCount == 0 (all its items filtered out, but
    // the group itself still produced by GroupDescriptions) is omitted entirely
    // — no header, no slot — instead of rendering an empty header.
    public bool HidesIfEmpty { get; set; }
}

// WPF's ItemsControl.GroupStyleSelector: chosen ahead of the GroupStyle
// collection when non-null (real WPF's own fallback order — see
// GroupItem.ResolveGroupStyle).
public delegate GroupStyle? GroupStyleSelector(System.Windows.Data.CollectionViewGroup group, int level);
