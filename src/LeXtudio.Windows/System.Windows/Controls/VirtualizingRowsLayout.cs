namespace System.Windows.Controls;

// Session 119 (DataGrid virtualization, Slice 1): dispatcher-free viewport math
// for the functional VirtualizingStackPanel core landing in Slice 2.
//
// Given a uniform/estimated row height and the current vertical viewport, this
// computes which item slice the panel must realize (plus a symmetric cache band
// for smooth scrolling), the total scroll extent, and the pixel offset of the
// first realized row. Keeping it pure mirrors the established dispatcher-free
// DataGridColumnResizeShim.ComputeWidth pattern so it is unit-testable without a
// live visual tree.
//
// Pixel-based (ScrollUnit.Pixel) to match the native Uno ScrollViewer; WPF
// item-based scrolling + variable heights arrive in later slices.
internal readonly struct VirtualizingRowsLayout
{
    /// <summary>Index of the first item to realize (inclusive, clamped to [0, itemCount]).</summary>
    public int FirstIndex { get; }

    /// <summary>Number of items to realize starting at <see cref="FirstIndex"/>.</summary>
    public int Count { get; }

    /// <summary>Total vertical extent of all items (itemCount * rowHeight).</summary>
    public double ExtentHeight { get; }

    /// <summary>Pixel offset (from the top of the extent) of the first realized row.</summary>
    public double FirstItemTop { get; }

    /// <summary>Index just past the last realized item (FirstIndex + Count).</summary>
    public int EndIndex => FirstIndex + Count;

    private VirtualizingRowsLayout(int firstIndex, int count, double extentHeight, double firstItemTop)
    {
        FirstIndex = firstIndex;
        Count = count;
        ExtentHeight = extentHeight;
        FirstItemTop = firstItemTop;
    }

    // Shared constructor access for VirtualizingRowsVariableLayout, which computes the
    // same four values via cumulative (non-uniform) offsets instead of pure arithmetic.
    internal static VirtualizingRowsLayout Create(int firstIndex, int count, double extentHeight, double firstItemTop)
        => new(firstIndex, count, extentHeight, firstItemTop);

    /// <summary>
    /// Computes the realized slice for the given viewport.
    /// </summary>
    /// <param name="itemCount">Total item count (values &lt; 0 are treated as 0).</param>
    /// <param name="rowHeight">Uniform/estimated row height in pixels. Must be &gt; 0; non-positive
    /// disables virtualization and realizes everything.</param>
    /// <param name="viewportTop">Vertical scroll offset of the viewport top, in pixels (clamped &gt;= 0).</param>
    /// <param name="viewportHeight">Visible height in pixels (values &lt; 0 are treated as 0).</param>
    /// <param name="cacheRows">Extra rows realized above and below the viewport (clamped &gt;= 0).</param>
    public static VirtualizingRowsLayout Compute(
        int itemCount,
        double rowHeight,
        double viewportTop,
        double viewportHeight,
        int cacheRows = 1)
    {
        if (itemCount < 0)
        {
            itemCount = 0;
        }

        if (itemCount == 0)
        {
            return new VirtualizingRowsLayout(0, 0, 0d, 0d);
        }

        // Non-positive / non-finite row height: cannot virtualize meaningfully, so
        // realize the whole list. Extent is unknown without a height, report 0.
        if (!(rowHeight > 0d) || double.IsInfinity(rowHeight))
        {
            return new VirtualizingRowsLayout(0, itemCount, 0d, 0d);
        }

        var extentHeight = itemCount * rowHeight;

        if (viewportTop < 0d || double.IsNaN(viewportTop))
        {
            viewportTop = 0d;
        }

        if (viewportHeight < 0d || double.IsNaN(viewportHeight))
        {
            viewportHeight = 0d;
        }

        if (cacheRows < 0)
        {
            cacheRows = 0;
        }

        // First/last item intersecting the viewport (before cache band).
        var firstVisible = (int)Math.Floor(viewportTop / rowHeight);

        // Last item whose top is above the viewport bottom. Use a row count that
        // always covers a partially visible trailing row.
        var viewportBottom = viewportTop + viewportHeight;
        var lastVisible = (int)Math.Ceiling(viewportBottom / rowHeight) - 1;
        if (lastVisible < firstVisible)
        {
            // Zero-height viewport: still realize the single row at the offset.
            lastVisible = firstVisible;
        }

        var firstIndex = firstVisible - cacheRows;
        var endIndex = lastVisible + cacheRows + 1; // exclusive

        if (firstIndex < 0)
        {
            firstIndex = 0;
        }

        if (endIndex > itemCount)
        {
            endIndex = itemCount;
        }

        if (firstIndex > itemCount)
        {
            firstIndex = itemCount;
        }

        var count = endIndex - firstIndex;
        if (count < 0)
        {
            count = 0;
        }

        return new VirtualizingRowsLayout(firstIndex, count, extentHeight, firstIndex * rowHeight);
    }
}
