using System.Windows.Documents;

namespace System.Windows.Controls.Primitives;

/// <summary>
/// Thin WPF-shaped compatibility surface for upstream TextBoxBase promotion.
/// Runtime behavior continues to live in Uno-side hosts until more WPF source compiles.
/// </summary>
public abstract class TextBoxBase : FrameworkElement
{
    public static readonly DependencyProperty IsInactiveSelectionHighlightEnabledProperty =
        DependencyProperty.Register(
            nameof(IsInactiveSelectionHighlightEnabledProperty),
            typeof(bool),
            typeof(TextBoxBase),
            new System.Windows.FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty IsSelectionActivePropertyKey =
        DependencyProperty.Register(
            nameof(IsSelectionActivePropertyKey),
            typeof(bool),
            typeof(TextBoxBase),
            new System.Windows.FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty CaretBrushProperty =
        DependencyProperty.Register(
            nameof(CaretBrushProperty),
            typeof(Brush),
            typeof(TextBoxBase),
            new System.Windows.FrameworkPropertyMetadata(null));

    public bool IsReadOnly { get; set; }

    public double ViewportWidth { get; protected set; }

    public double ExtentWidth { get; protected set; }

    public double HorizontalOffset { get; protected set; }

    public TextSelection? Selection { get; protected set; }

    public event EventHandler? SelectionChanged;

    public virtual void SelectAll()
    {
    }

    public virtual void Copy()
    {
    }

    public virtual void Cut()
    {
    }

    public virtual void Paste()
    {
    }

    public virtual void AppendText(string textData)
    {
    }

    internal void NotifySelectionChanged()
        => RaiseSelectionChanged();

    protected void RaiseSelectionChanged()
        => SelectionChanged?.Invoke(this, EventArgs.Empty);
}
