using System.Windows.Data;

namespace System.Windows.Controls;

// Session 58: the WPF DataGridBoundColumn body is now reused from the linked
// upstream file (Binding, ElementStyle/EditingElementStyle, ApplyBinding,
// ApplyStyle, RefreshCellContent, ClipboardContentBinding, and the real
// OnCoerceSortMemberPath coercion). This partial carries only the Uno-specific
// BindingPath helper the render/sort/edit paths use.
public abstract partial class DataGridBoundColumn
{
    // Binding path used by the shim render/sort/edit paths (session 39).
    internal string? BindingPath
        => Binding is Binding { Path: { } pp } ? pp.Path : null;

    // The upstream OnCoerceSortMemberPath callback can't run in the shim DP
    // system, so derive SortMemberPath from the binding here (mirrors WPF: an
    // empty SortMemberPath falls back to the binding path).
    protected internal override void CoerceValue(DependencyProperty dp)
    {
        base.CoerceValue(dp);
        if (dp == SortMemberPathProperty
            && string.IsNullOrEmpty(SortMemberPath)
            && BindingPath is { Length: > 0 } path)
        {
            SortMemberPath = path;
        }
    }
}
