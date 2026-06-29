using NUnit.Framework;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

// Session 119, Slice 1: dispatcher-free viewport math for the DataGrid
// virtualization core (VirtualizingRowsLayout.Compute).
[TestFixture]
public sealed class VirtualizingRowsLayoutTests
{
    [Test]
    public void EmptyListRealizesNothing()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 0, rowHeight: 20, viewportTop: 0, viewportHeight: 200);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.Count, Is.EqualTo(0));
        Assert.That(layout.ExtentHeight, Is.EqualTo(0));
        Assert.That(layout.FirstItemTop, Is.EqualTo(0));
    }

    [Test]
    public void ExtentIsItemCountTimesRowHeight()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200);

        Assert.That(layout.ExtentHeight, Is.EqualTo(20_000));
    }

    [Test]
    public void TopOfListRealizesOnlyViewportPlusCache()
    {
        // viewport [0,200) over 20px rows = rows 0..9 visible; cache 2 -> 0..11.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200, cacheRows: 2);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.EndIndex, Is.EqualTo(12));
        Assert.That(layout.Count, Is.EqualTo(12));
        Assert.That(layout.FirstItemTop, Is.EqualTo(0));
    }

    [Test]
    public void ScrolledMiddleRealizesSliceAroundViewport()
    {
        // viewportTop 1000 over 20px rows -> firstVisible 50; viewport 200 -> rows 50..59.
        // cache 1 -> realize 49..60 (inclusive) => first 49, end 61, count 12.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 1000, viewportHeight: 200, cacheRows: 1);

        Assert.That(layout.FirstIndex, Is.EqualTo(49));
        Assert.That(layout.EndIndex, Is.EqualTo(61));
        Assert.That(layout.Count, Is.EqualTo(12));
        Assert.That(layout.FirstItemTop, Is.EqualTo(49 * 20));
    }

    [Test]
    public void PartiallyScrolledRowStillRealized()
    {
        // viewportTop 1010 (mid-row 50) -> firstVisible 50 (floor), viewport 200 ->
        // bottom 1210 -> ceiling/rowHeight - 1 = ceil(60.5)-1 = 60. Visible 50..60.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 1010, viewportHeight: 200, cacheRows: 0);

        Assert.That(layout.FirstIndex, Is.EqualTo(50));
        Assert.That(layout.EndIndex, Is.EqualTo(61));
    }

    [Test]
    public void SliceIsClampedAtEndOfList()
    {
        // Near the bottom: viewportTop covers the last rows; end clamps to itemCount.
        var layout = VirtualizingRowsLayout.Compute(itemCount: 100, rowHeight: 20, viewportTop: 1820, viewportHeight: 200, cacheRows: 2);

        Assert.That(layout.EndIndex, Is.EqualTo(100));
        Assert.That(layout.FirstIndex, Is.LessThan(100));
        Assert.That(layout.FirstIndex + layout.Count, Is.EqualTo(100));
    }

    [Test]
    public void NegativeViewportTopIsClampedToZero()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 100, rowHeight: 20, viewportTop: -500, viewportHeight: 200, cacheRows: 0);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.FirstItemTop, Is.EqualTo(0));
    }

    [Test]
    public void NonPositiveRowHeightRealizesEverything()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 42, rowHeight: 0, viewportTop: 0, viewportHeight: 200);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.Count, Is.EqualTo(42));
        Assert.That(layout.ExtentHeight, Is.EqualTo(0));
    }

    [Test]
    public void ZeroHeightViewportStillRealizesAnchorRow()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 100, rowHeight: 20, viewportTop: 200, viewportHeight: 0, cacheRows: 0);

        // firstVisible = 10; zero-height viewport still realizes that one row.
        Assert.That(layout.FirstIndex, Is.EqualTo(10));
        Assert.That(layout.Count, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void CacheBandNeverProducesNegativeFirstIndex()
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200, cacheRows: 50);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.Count, Is.GreaterThan(0));
    }
}
