using System.Windows.Controls;
using TraceEventType = System.Diagnostics.TraceEventType;

namespace MS.Internal;

internal static class TraceData
{
    internal static void TraceAndNotify(TraceEventType type, object message, object? details = null) { }
    internal static object CannotSort(string propertyName) => propertyName;
}

// WPF NamedObject: a named sentinel used as a placeholder (e.g. NewItemPlaceholder).
internal sealed class NamedObject
{
    private readonly string _name;
    internal NamedObject(string name) => _name = name;
    public override string ToString() => _name;
}
