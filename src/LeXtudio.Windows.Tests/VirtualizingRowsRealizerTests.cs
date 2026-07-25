using Xunit;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

// Session 119, Slice 2: the windowed realize/recycle state machine
// (VirtualizingRowsRealizer<T>). Plain object containers exercise the algorithm
// without a live visual tree (UIElements cannot be created off the Uno UI thread).
public sealed class VirtualizingRowsRealizerTests
{
    private sealed class Harness
    {
        public int Created;
        public int Prepared;
        public int Cleared;
        public readonly List<(object container, int index)> PrepareLog = new();

        public VirtualizingRowsRealizer<object> Make(int itemCount, bool recycling)
        {
            return new VirtualizingRowsRealizer<object>(
                itemAt: i => $"item{i}",
                create: _ => { Created++; return new object(); },
                prepare: (c, _, idx) => { Prepared++; PrepareLog.Add((c, idx)); },
                clear: (_, _) => Cleared++,
                recycling: recycling);
        }
    }

    [Fact]
    public void InitialRealizeCreatesOnlyWindowPlusCache()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        var layout = r.Realize(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200, cacheRows: 2);

        // rows 0..9 visible + 2 cache below => 0..11 (12 containers).
        Assert.Equal(12, layout.Count);
        Assert.Equal(12, r.Realized.Count);
        Assert.Equal(12, h.Created);
        Assert.Equal(12, h.Prepared);
        Assert.Equal(0, h.Cleared);
        Assert.Equal(0, r.RecyclePoolCount);
    }

    [Fact]
    public void ReRealizingSameViewportDoesNoWork()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, 0, 200, cacheRows: 2);
        var createdAfterFirst = h.Created;
        var preparedAfterFirst = h.Prepared;

        r.Realize(1000, 20, 0, 200, cacheRows: 2);

        Assert.Equal(createdAfterFirst, h.Created);
        Assert.Equal(preparedAfterFirst, h.Prepared);
        Assert.Equal(0, h.Cleared);
    }

    [Fact]
    public void OverlappingScrollKeepsSharedIndexContainerInstance()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 1);   // ~0..10
        var shared = r.ContainerFromIndex(9);
        Assert.NotNull(shared);

        r.Realize(1000, 20, viewportTop: 100, viewportHeight: 200, cacheRows: 1); // ~4..15

        Assert.Same(shared, r.ContainerFromIndex(9));
    }

    [Fact]
    public void RecyclingReusesContainerInstancesAcrossDisjointWindows()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 0);     // 0..9
        var createdAfterTop = h.Created;
        Assert.Equal(10, createdAfterTop);

        // Jump far away — fully disjoint window. The 10 cleared containers should
        // be recycled to serve the 10 new indices, so no new creation.
        r.Realize(1000, 20, viewportTop: 5000, viewportHeight: 200, cacheRows: 0);  // 250..259

        Assert.Equal(10, h.Cleared);
        Assert.Equal(createdAfterTop, h.Created);
        Assert.Equal(10, r.Realized.Count);
        Assert.Equal(0, r.RecyclePoolCount);
    }

    [Fact]
    public void StandardModeDoesNotReuseAndDoesNotPool()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: false);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 0);    // 0..9
        r.Realize(1000, 20, viewportTop: 5000, viewportHeight: 200, cacheRows: 0); // 250..259

        Assert.Equal(10, h.Cleared);
        Assert.Equal(0, r.RecyclePoolCount);
        Assert.Equal(20, h.Created);
    }

    [Fact]
    public void RealizedCountNeverExceedsWindowSize()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        for (double top = 0; top <= 4000; top += 137)
        {
            var layout = r.Realize(1000, 20, viewportTop: top, viewportHeight: 200, cacheRows: 2);
            Assert.Equal(layout.Count, r.Realized.Count);
            Assert.True(r.Realized.Count <= 16);
        }
    }

    [Fact]
    public void ClearRecyclesAllRealizedContainers()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, 0, 200, cacheRows: 0); // 10 realized
        r.Clear();

        Assert.Empty(r.Realized);
        Assert.Equal(10, h.Cleared);
        Assert.Equal(10, r.RecyclePoolCount);
    }

    [Fact]
    public void PreparePassesCorrectIndexOnRecycledReuse()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 0);     // 0..9
        h.PrepareLog.Clear();
        r.Realize(1000, 20, viewportTop: 5000, viewportHeight: 200, cacheRows: 0);  // 250..259

        Assert.Equal(10, h.PrepareLog.Count);
        foreach (var (_, index) in h.PrepareLog)
        {
            Assert.InRange(index, 250, 259);
        }
    }
}