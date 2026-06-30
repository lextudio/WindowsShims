using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;

namespace System.Windows.Controls.Primitives;

/// <summary>
/// Horizontal items host for the Uno ToolBar. Lays children out left-to-right and,
/// when they do not all fit, leaves the trailing ones unarranged (zero rect) and
/// records them in <see cref="OverflowItems"/>. The ToolBar surfaces those through
/// an overflow chevron + popup. This is the Uno replacement for WPF's
/// ToolBarPanel/ToolBarOverflowPanel pair (whose layout is #if !HAS_UNO).
/// </summary>
public sealed class ToolBarOverflowAwarePanel : Panel
{
    public List<UIElement> OverflowItems { get; } = new();

    public event EventHandler? OverflowChanged;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        if (double.IsInfinity(availableSize.Width))
            return new Size(width, height);

        return new Size(Math.Min(width, availableSize.Width), height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var previous = OverflowItems.ToList();
        OverflowItems.Clear();

        double x = 0;
        foreach (var child in Children)
        {
            var w = child.DesiredSize.Width;
            if (x + w <= finalSize.Width + 0.5)
            {
                child.Arrange(new Rect(x, 0, w, finalSize.Height));
                x += w;
            }
            else
            {
                // Park overflowed children off-layout; the ToolBar reparents them
                // into the overflow popup when the chevron is opened.
                child.Arrange(new Rect(0, 0, 0, 0));
                OverflowItems.Add(child);
            }
        }

        if (!previous.SequenceEqual(OverflowItems))
            OverflowChanged?.Invoke(this, EventArgs.Empty);

        return finalSize;
    }
}
