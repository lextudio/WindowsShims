using System.Collections;
using System.Windows.Controls;

namespace MS.Internal.Controls;

// WPF-internal enumerator that yields Header first, then the base ItemsControl logical children.
// Used by HeaderedItemsControl.LogicalChildren on HAS_UNO (the base Control shim already exposes
// LogicalChildren as a virtual property returning an empty enumerator).
internal sealed class HeaderedItemsModelTreeEnumerator : IEnumerator
{
    private readonly IEnumerator _baseEnumerator;
    private readonly object _header;
    private int _state; // 0=before header, 1=at header, 2=in base

    internal HeaderedItemsModelTreeEnumerator(System.Windows.Controls.ItemsControl owner, IEnumerator baseEnumerator, object header)
    {
        _baseEnumerator = baseEnumerator;
        _header = header;
        _state = 0;
    }

    public object? Current => _state == 1 ? _header : _baseEnumerator.Current;

    public bool MoveNext()
    {
        if (_state == 0) { _state = 1; return true; }
        if (_state == 1) { _state = 2; }
        if (_state == 2) return _baseEnumerator.MoveNext();
        return false;
    }

    public void Reset()
    {
        _state = 0;
        _baseEnumerator.Reset();
    }
}
