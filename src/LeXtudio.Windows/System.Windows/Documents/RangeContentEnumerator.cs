using System.Collections;

namespace System.Windows.Documents;

/// <summary>
/// WPF range enumerator over content between two TextPointers.
/// Shim implementation - returns empty enumeration; sufficient for compilation.
/// </summary>
public class RangeContentEnumerator : IEnumerator
{
    public RangeContentEnumerator(TextPointer? start, TextPointer? end)
    {
    }

    public object? Current => null;

    public bool MoveNext() => false;

    public void Reset() { }
}
