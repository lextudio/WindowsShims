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

    internal string? EffectiveSortMemberPath
        => string.IsNullOrEmpty(SortMemberPath) ? BindingPath : SortMemberPath;
}
