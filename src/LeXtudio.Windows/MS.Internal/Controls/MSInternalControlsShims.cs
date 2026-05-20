using System.Collections;

namespace MS.Internal.Controls;

internal sealed class EmptyEnumerator : IEnumerator
{
    private EmptyEnumerator() { }
    public static readonly IEnumerator Instance = new EmptyEnumerator();
    public object? Current => null;
    public bool MoveNext() => false;
    public void Reset() { }
}
