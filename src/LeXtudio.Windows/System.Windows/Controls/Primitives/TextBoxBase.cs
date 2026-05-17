using System.Windows.Documents;

namespace System.Windows.Controls.Primitives;

/// <summary>
/// Thin WPF-shaped compatibility surface for upstream TextBoxBase promotion.
/// Runtime behavior continues to live in Uno-side hosts until more WPF source compiles.
/// </summary>
public abstract class TextBoxBase
{
    public bool IsReadOnly { get; set; }

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
