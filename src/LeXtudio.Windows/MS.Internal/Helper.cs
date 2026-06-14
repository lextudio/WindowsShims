namespace MS.Internal;

internal static class Helper
{
    internal static void InvalidateMeasureOnPath(
        DependencyObject pathStartElement,
        DependencyObject pathEndElement,
        bool duringMeasure,
        bool includePathEnd)
    {
        UIElement? start = pathStartElement as UIElement;
        if (start is not null)
        {
            start.InvalidateMeasure();
        }

        if (includePathEnd && pathEndElement is UIElement end && !ReferenceEquals(start, end))
        {
            end.InvalidateMeasure();
        }
    }
}
