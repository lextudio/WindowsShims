namespace System.Windows;

/// <summary>
/// WPF logical tree helper. Upstream WPF's TextContainer (element insertion,
/// ReparentLogicalChildren, etc.) calls these to keep each TextElement's
/// FrameworkContentElement.Parent in sync with its containing scope. This used
/// to be a no-op stub, which left TextElement.Parent (e.g. Run.Parent) always
/// null/stale, breaking any code that walks TextElement.Parent chains —
/// notably TextPointer.ParentBlock (used by ParagraphOrBlockUIContainer, which
/// drives paragraph-merge-on-delete) and TextRangeEditTables.FindTableElements'
/// common-ancestor walk. Real WPF's version does considerably more (visual
/// parent pointers, logical tree change notifications); this keeps only the
/// Parent bookkeeping our linked WPF document/editing code depends on.
/// </summary>
public static class LogicalTreeHelper
{
    public static void AddLogicalChild(DependencyObject parent, object child)
    {
        if (child is FrameworkContentElement childElement)
            childElement.Parent = parent;
    }

    public static void RemoveLogicalChild(DependencyObject parent, object child)
    {
        if (child is FrameworkContentElement childElement && ReferenceEquals(childElement.Parent, parent))
            childElement.Parent = null;
    }

    public static DependencyObject? GetParent(DependencyObject current) =>
        current is FrameworkContentElement element ? element.Parent : null;
}
