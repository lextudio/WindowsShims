using NUnit.Framework;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

// Gap survey item 8: row-details variable-height rows under virtualization.
// VirtualizingRowsVariableLayout.Compute is VirtualizingRowsLayout.Compute's
// variable-height counterpart — same result shape and cache-band semantics,
// but driven by a cumulative-offset lookup instead of index * rowHeight, so it
// still gives correct answers once some rows (e.g. an expanded RowDetails row)
// are taller than others.
[TestFixture]
public sealed class VirtualizingRowsVariableLayoutTests
{
    private static double[] UniformOffsets(int itemCount, double rowHeight)
    {
        var offsets = new double[itemCount + 1];
        for (var i = 0; i < itemCount; i++)
        {
            offsets[i + 1] = offsets[i] + rowHeight;
        }

        return offsets;
    }

    [Test]
    public void EmptyListRealizesNothing()
    {
        var layout = VirtualizingRowsVariableLayout.Compute(0, _ => 0, viewportTop: 0, viewportHeight: 200);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.Count, Is.EqualTo(0));
        Assert.That(layout.ExtentHeight, Is.EqualTo(0));
        Assert.That(layout.FirstItemTop, Is.EqualTo(0));
    }

    [Test]
    public void UniformHeightsMatchTheUniformLayoutExactly()
    {
        var offsets = UniformOffsets(1000, 20);
        var uniform = VirtualizingRowsLayout.Compute(1000, 20, viewportTop: 1000, viewportHeight: 200, cacheRows: 1);
        var variable = VirtualizingRowsVariableLayout.Compute(1000, i => offsets[i], viewportTop: 1000, viewportHeight: 200, cacheRows: 1);

        Assert.That(variable.FirstIndex, Is.EqualTo(uniform.FirstIndex));
        Assert.That(variable.EndIndex, Is.EqualTo(uniform.EndIndex));
        Assert.That(variable.ExtentHeight, Is.EqualTo(uniform.ExtentHeight));
        Assert.That(variable.FirstItemTop, Is.EqualTo(uniform.FirstItemTop));
    }

    [Test]
    public void ExpandedRowDetailsPushesLaterRowsDownByTheRealHeightDifference()
    {
        // 10 rows: row 0 is a 200px-tall expanded-details row, the rest are 20px.
        // A uniform model would place row 1's top at 20; the real cumulative top is 200.
        var heights = new double[10];
        heights[0] = 200;
        for (var i = 1; i < 10; i++) heights[i] = 20;

        double OffsetOf(int index)
        {
            var sum = 0d;
            for (var i = 0; i < index; i++) sum += heights[i];
            return sum;
        }

        var layout = VirtualizingRowsVariableLayout.Compute(10, OffsetOf, viewportTop: 0, viewportHeight: 100, cacheRows: 0);

        // Viewport [0,100) only reaches partway into row 0 (0..200) — row 0 alone covers it.
        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.EndIndex, Is.EqualTo(1));
        Assert.That(layout.ExtentHeight, Is.EqualTo(200 + 9 * 20));
    }

    [Test]
    public void ScrollingPastTheExpandedRowRealizesTheCorrectLaterIndex()
    {
        var heights = new double[10];
        heights[0] = 200;
        for (var i = 1; i < 10; i++) heights[i] = 20;

        double OffsetOf(int index)
        {
            var sum = 0d;
            for (var i = 0; i < index; i++) sum += heights[i];
            return sum;
        }

        // A uniform model (rowHeight ~20) would compute firstVisible = floor(210/20) = 10
        // (past the end!). The real cumulative offsets put index 1's top at 200, so
        // viewportTop 210 is 10px into row 1, not row 10.
        var layout = VirtualizingRowsVariableLayout.Compute(10, OffsetOf, viewportTop: 210, viewportHeight: 20, cacheRows: 0);

        Assert.That(layout.FirstIndex, Is.EqualTo(1));
        Assert.That(layout.FirstItemTop, Is.EqualTo(200));
    }

    [Test]
    public void CacheBandNeverProducesNegativeFirstIndex()
    {
        var offsets = UniformOffsets(1000, 20);
        var layout = VirtualizingRowsVariableLayout.Compute(1000, i => offsets[i], viewportTop: 0, viewportHeight: 200, cacheRows: 50);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.Count, Is.GreaterThan(0));
    }

    [Test]
    public void SliceIsClampedAtEndOfList()
    {
        var offsets = UniformOffsets(100, 20);
        var layout = VirtualizingRowsVariableLayout.Compute(100, i => offsets[i], viewportTop: 1820, viewportHeight: 200, cacheRows: 2);

        Assert.That(layout.EndIndex, Is.EqualTo(100));
        Assert.That(layout.FirstIndex, Is.LessThan(100));
        Assert.That(layout.FirstIndex + layout.Count, Is.EqualTo(100));
    }

    [Test]
    public void NegativeViewportTopIsClampedToZero()
    {
        var offsets = UniformOffsets(100, 20);
        var layout = VirtualizingRowsVariableLayout.Compute(100, i => offsets[i], viewportTop: -500, viewportHeight: 200, cacheRows: 0);

        Assert.That(layout.FirstIndex, Is.EqualTo(0));
        Assert.That(layout.FirstItemTop, Is.EqualTo(0));
    }

    [Test]
    public void ZeroHeightViewportStillRealizesAnchorRow()
    {
        var offsets = UniformOffsets(100, 20);
        var layout = VirtualizingRowsVariableLayout.Compute(100, i => offsets[i], viewportTop: 200, viewportHeight: 0, cacheRows: 0);

        Assert.That(layout.FirstIndex, Is.EqualTo(10));
        Assert.That(layout.Count, Is.GreaterThanOrEqualTo(1));
    }
}
