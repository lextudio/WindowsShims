using System.Collections;
using System.Windows.Controls;

namespace MS.Internal.Controls;

// WPF-internal logical-tree enumerator for HeaderedContentControl.
// Yields Header first, then Content (if not treated as non-logical).
internal sealed class HeaderedContentModelTreeEnumerator : IEnumerator
{
    private readonly object? _content;
    private readonly object _header;
    private int _state; // 0=before header, 1=at header, 2=at content, 3=done

    internal HeaderedContentModelTreeEnumerator(System.Windows.Controls.ContentControl owner, object? content, object header)
    {
        _content = content;
        _header  = header;
        _state   = 0;
    }

    public object? Current => _state == 1 ? _header : _content;

    public bool MoveNext()
    {
        if (_state == 0) { _state = 1; return true; }
        if (_state == 1)
        {
            if (_content != null) { _state = 2; return true; }
            _state = 3;
        }
        return false;
    }

    public void Reset() => _state = 0;
}
