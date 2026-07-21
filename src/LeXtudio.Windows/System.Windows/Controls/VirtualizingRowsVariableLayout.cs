namespace System.Windows.Controls;

// Gap survey item 8 (docs/session121.md): row-details variable-height rows under
// virtualization. VirtualizingRowsLayout.Compute assumes one uniform row height,
// which breaks the moment RowDetailsVisibilityMode makes some realized rows taller
// than others (an expanded details row vs. a collapsed one) — the uniform math's
// `index * rowHeight` offset is simply wrong for any row after the first one whose
// actual height differs from the estimate.
//
// This mirrors VirtualizingRowsLayout.Compute's shape (same result type, same
// cache-band semantics) but replaces "index * rowHeight" arithmetic with a caller-
// supplied cumulative-offset lookup, so it works whether every row is the same
// height (degenerates to the uniform case) or not. The caller (VirtualizingStackPanel)
// backs offsetOfIndex with a prefix-sum array built from a per-index height cache
// (actual measured heights where known, an average estimate elsewhere) — the same
// "exact where realized, estimated where not" approach real WPF's own variable-
// height virtualization uses, without needing this shim to replicate WPF's full
// IHierarchicalVirtualizationAndScrollInfo machinery.
internal static class VirtualizingRowsVariableLayout
{
    /// <summary>
    /// Computes the realized slice for the given viewport, using cumulative offsets
    /// instead of a uniform row height.
    /// </summary>
    /// <param name="itemCount">Total item count (values &lt; 0 are treated as 0).</param>
    /// <param name="offsetOfIndex">
    /// Cumulative height of every item before <c>index</c> (i.e. item <c>index</c>'s
    /// top, in the same coordinate space as <paramref name="viewportTop"/>). Must be
    /// non-decreasing over <c>0..itemCount</c> inclusive; <c>offsetOfIndex(itemCount)</c>
    /// is taken as the total extent.
    /// </param>
    /// <param name="viewportTop">Vertical scroll offset of the viewport top, in pixels (clamped &gt;= 0).</param>
    /// <param name="viewportHeight">Visible height in pixels (values &lt; 0 are treated as 0).</param>
    /// <param name="cacheRows">Extra rows realized above and below the viewport (clamped &gt;= 0).</param>
    public static VirtualizingRowsLayout Compute(
        int itemCount,
        Func<int, double> offsetOfIndex,
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
            return VirtualizingRowsLayout.Create(0, 0, 0d, 0d);
        }

        var extentHeight = offsetOfIndex(itemCount);

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

        // First item intersecting the viewport top (the item whose top is <= viewportTop).
        var firstVisible = Math.Min(IndexOfOffset(itemCount, offsetOfIndex, viewportTop), itemCount - 1);

        // Last item intersecting the viewport bottom, even partially — mirrors the
        // uniform version's ceil(bottom/height) - 1: back off one unless the bottom
        // offset lands strictly inside (not exactly at the top of) that item.
        var viewportBottom = viewportTop + viewportHeight;
        var lastVisible = Math.Min(IndexOfOffset(itemCount, offsetOfIndex, viewportBottom), itemCount - 1);
        if (lastVisible > firstVisible && offsetOfIndex(lastVisible) >= viewportBottom)
        {
            lastVisible--;
        }

        if (lastVisible < firstVisible)
        {
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

        return VirtualizingRowsLayout.Create(firstIndex, count, extentHeight, offsetOfIndex(firstIndex));
    }

    // Largest index i in [0, itemCount] with offsetOfIndex(i) <= offset. Binary search
    // relies on offsetOfIndex being non-decreasing (true for any real height sequence).
    private static int IndexOfOffset(int itemCount, Func<int, double> offsetOfIndex, double offset)
    {
        var lo = 0;
        var hi = itemCount;
        while (lo < hi)
        {
            var mid = lo + (hi - lo + 1) / 2;
            if (offsetOfIndex(mid) <= offset)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }
}
