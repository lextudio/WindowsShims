using System.Windows.Documents;

namespace System.Windows.Controls;

/// <summary>
/// WPF-compatible TextBlock. On the Uno desktop target it derives directly from WinUI's TextBlock
/// (which is not sealed there) and shadows InlineCollection so WPF source can call
/// Inlines.Add(new Bold(...)) etc.
///
/// On the WinUI (WINDOWS_APP_SDK) target Microsoft.UI.Xaml.Controls.TextBlock is SEALED, so the shim
/// cannot derive from it. To keep the WinUI build compiling, it wraps an inner WinUI TextBlock inside a
/// (non-sealed) ContentControl and forwards the common text surface. The WPF Inlines collection is
/// exposed for source compatibility; rendering inlines on this target is a separate (deferred) concern.
/// </summary>
#if WINDOWS_APP_SDK
public partial class TextBlock : Microsoft.UI.Xaml.Controls.ContentControl
{
    private readonly Microsoft.UI.Xaml.Controls.TextBlock _inner = new();
    private InlineCollection? _inlines;

    public TextBlock()
    {
        Content = _inner;
    }

    // WPF exposes TextBlock.TextProperty as a bindable DependencyProperty (used e.g. by
    // DataGridTextColumn to bind cell text). Re-declare it here and forward to the inner TextBlock.
    public static readonly Microsoft.UI.Xaml.DependencyProperty TextProperty =
        Microsoft.UI.Xaml.DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TextBlock),
            new Microsoft.UI.Xaml.PropertyMetadata(string.Empty, OnTextPropertyChanged));

    private static void OnTextPropertyChanged(Microsoft.UI.Xaml.DependencyObject d, Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock tb)
            tb._inner.Text = e.NewValue as string ?? string.Empty;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Microsoft.UI.Xaml.TextWrapping TextWrapping
    {
        get => _inner.TextWrapping;
        set => _inner.TextWrapping = value;
    }

    public Microsoft.UI.Xaml.TextTrimming TextTrimming
    {
        get => _inner.TextTrimming;
        set => _inner.TextTrimming = value;
    }

    public Microsoft.UI.Xaml.TextAlignment TextAlignment
    {
        get => _inner.TextAlignment;
        set => _inner.TextAlignment = value;
    }

    public InlineCollection Inlines => _inlines ??= new InlineCollection(this, true);
}
#else
public partial class TextBlock : Microsoft.UI.Xaml.Controls.TextBlock
{
    private InlineCollection? _inlines;

    public new InlineCollection Inlines => _inlines ??= new InlineCollection(this, true);
}
#endif
