using Xunit;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

// Session 119, Slice 1: dispatcher-free viewport math for the DataGrid
// virtualization core (VirtualizingRowsLayout.Compute).
public sealed class VirtualizingRowsLayoutTests
{
    [Fact]
    public void EmptyListRealizesNothing()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 0, rowHeight: 20, viewportTop: 0, viewportHeight: 200);

        Assert.Equal(0, layout.FirstIndex);
        Assert.Equal(0, layout.Count);
        Assert.Equal(0, layout.ExtentHeight);
        Assert.Equal(0, layout.FirstItemTop);
    }

    [Fact]
    public void ExtentIsItemCountTimesRowHeight()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200);

        Assert.Equal(20_000, layout.ExtentHeight);
    }

    [Fact]
    public void TopOfListRealizesOnlyViewportPlusCache()
    {
        // viewport [0,200) over 20px rows = rows 0..9 visible; cache 2 -> 0..11.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200, cacheRows: 2);

        Assert.Equal(0, layout.FirstIndex);
        Assert.Equal(12, layout.EndIndex);
        Assert.Equal(12, layout.Count);
        Assert.Equal(0, layout.FirstItemTop);
    }

    [Fact]
    public void ScrolledMiddleRealizesSliceAroundViewport()
    {
        // viewportTop 1000 over 20px rows -> firstVisible 50; viewport 200 -> rows 50..59.
        // cache 1 -> realize 49..60 (inclusive) => first 49, end 61, count 12.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 1000, viewportHeight: 200, cacheRows: 1);

        Assert.Equal(49, layout.FirstIndex);
        Assert.Equal(61, layout.EndIndex);
        Assert.Equal(12, layout.Count);
        Assert.Equal(49 * 20, layout.FirstItemTop);
    }

    [Fact]
    public void PartiallyScrolledRowStillRealized()
    {
        // viewportTop 1010 (mid-row 50) -> firstVisible 50 (floor), viewport 200 ->
        // bottom 1210 -> ceiling/rowHeight - 1 = ceil(60.5)-1 = 60. Visible 50..60.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 1010, viewportHeight: 200, cacheRows: 0);

        Assert.Equal(50, layout.FirstIndex);
        Assert.Equal(61, layout.EndIndex);
    }

    [Fact]
    public void SliceIsClampedAtEndOfList()
    {
        // Near the bottom: viewportTop covers the last rows; end clamps to itemCount.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 100, rowHeight: 20, viewportTop: 1820, viewportHeight: 200, cacheRows: 2);

        Assert.Equal(100, layout.EndIndex);
        Assert.True(layout.FirstIndex < 100);
        Assert.Equal(100, layout.FirstIndex + layout.Count);
    }

    [Fact]
    public void NegativeViewportTopIsClampedToZero()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 100, rowHeight: 20, viewportTop: -500, viewportHeight: 200, cacheRows: 0);

        Assert.Equal(0, layout.FirstIndex);
        Assert.Equal(0, layout.FirstItemTop);
    }

    [Fact]
    public void NonPositiveRowHeightRealizesEverything()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 42, rowHeight: 0, viewportTop: 0, viewportHeight: 200);

        Assert.Equal(0, layout.FirstIndex);
        Assert.Equal(42, layout.Count);
        Assert.Equal(0, layout.ExtentHeight);
    }

    [Fact]
    public void ZeroHeightViewportStillRealizesAnchorRow()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 100, rowHeight: 20, viewportTop: 200, viewportHeight: 0, cacheRows: 0);

        // firstVisible = 10; zero-height viewport still realizes that one row.
        Assert.Equal(10, layout.FirstIndex);
        Assert.True(layout.Count >= 1);
    }

    [Fact]
    public void CacheBandNeverProducesNegativeFirstIndex()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200, cacheRows: 50);

        Assert.Equal(0, layout.FirstIndex);
        Assert.True(layout.Count > 0);
    }
}
