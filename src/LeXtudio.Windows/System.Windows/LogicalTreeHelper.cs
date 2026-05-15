namespace System.Windows;

/// <summary>
/// WPF logical tree helper stub. The shim does not implement actual logical tree
/// management; methods are no-ops or return null sufficient to satisfy compilation.
/// </summary>
internal static class LogicalTreeHelper
{
    public static void AddLogicalChild(DependencyObject parent, object child) { }
    public static void RemoveLogicalChild(DependencyObject parent, object child) { }
    public static DependencyObject? GetParent(DependencyObject current) => null;
}
