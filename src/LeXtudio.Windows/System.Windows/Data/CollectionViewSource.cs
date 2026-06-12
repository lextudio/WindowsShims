namespace System.Windows.Data;

// Stub: the shim ItemCollection is always its own default (degenerate) view,
// so every view it hands out is "the default view".
public class CollectionViewSource
{
    internal static bool IsDefaultView(object? view) => true;
}
