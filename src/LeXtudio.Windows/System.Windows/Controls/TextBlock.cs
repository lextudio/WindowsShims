using System.Windows.Documents;

namespace System.Windows.Controls;

/// <summary>
/// WPF-compatible TextBlock backed by WinUI TextBlock for rendering.
/// Shadows WinUI's InlineCollection with the WPF InlineCollection so
/// WPF source files can call Inlines.Add(new Bold(...)) etc.
/// </summary>
public partial class TextBlock : Microsoft.UI.Xaml.Controls.TextBlock
{
    private InlineCollection? _inlines;

    public new InlineCollection Inlines => _inlines ??= new InlineCollection(this, true);
}
