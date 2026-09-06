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

    /// <summary>
    /// Logical children of <paramref name="current"/>. WPF reads the element's own
    /// LogicalChildren enumerator; on HAS_UNO the closest equivalent is the WinUI
    /// panel/content child set, so a Panel's Children and a ContentControl's Content
    /// are surfaced and everything else enumerates empty.
    /// </summary>
    public static System.Collections.IEnumerable GetChildren(DependencyObject current)
    {
        switch (current)
        {
            case Microsoft.UI.Xaml.Controls.Panel panel:
                return panel.Children;
            case Microsoft.UI.Xaml.Controls.ContentControl { Content: { } content }:
                return new[] { content };
            case Microsoft.UI.Xaml.Controls.Border { Child: { } child }:
                return new object[] { child };
            default:
                return Array.Empty<object>();
        }
    }

    /// <summary>
    /// Depth-first search for a named element under <paramref name="current"/>,
    /// matching WPF's FindLogicalNode over the children GetChildren reports.
    /// </summary>
    public static DependencyObject? FindLogicalNode(DependencyObject current, string elementName)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(elementName);

        if (current is Microsoft.UI.Xaml.FrameworkElement element && element.Name == elementName)
        {
            return current;
        }

        foreach (var child in GetChildren(current))
        {
            if (child is DependencyObject childObject && FindLogicalNode(childObject, elementName) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
