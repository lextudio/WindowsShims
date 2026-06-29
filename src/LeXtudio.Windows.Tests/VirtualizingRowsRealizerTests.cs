using NUnit.Framework;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LeXtudio.Windows.Tests;

// Session 119, Slice 2: the windowed realize/recycle state machine
// (VirtualizingRowsRealizer<T>). Plain object containers exercise the algorithm
// without a live visual tree (UIElements cannot be created off the Uno UI thread).
[TestFixture]
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

    [Test]
    public void InitialRealizeCreatesOnlyWindowPlusCache()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        var layout = r.Realize(itemCount: 1000, rowHeight: 20, viewportTop: 0, viewportHeight: 200, cacheRows: 2);

        // rows 0..9 visible + 2 cache below => 0..11 (12 containers).
        Assert.That(layout.Count, Is.EqualTo(12));
        Assert.That(r.Realized.Count, Is.EqualTo(12));
        Assert.That(h.Created, Is.EqualTo(12));
        Assert.That(h.Prepared, Is.EqualTo(12));
        Assert.That(h.Cleared, Is.EqualTo(0));
        Assert.That(r.RecyclePoolCount, Is.EqualTo(0));
    }

    [Test]
    public void ReRealizingSameViewportDoesNoWork()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, 0, 200, cacheRows: 2);
        var createdAfterFirst = h.Created;
        var preparedAfterFirst = h.Prepared;

        r.Realize(1000, 20, 0, 200, cacheRows: 2);

        Assert.That(h.Created, Is.EqualTo(createdAfterFirst), "no new containers");
        Assert.That(h.Prepared, Is.EqualTo(preparedAfterFirst), "no redundant prepare");
        Assert.That(h.Cleared, Is.EqualTo(0));
    }

    [Test]
    public void OverlappingScrollKeepsSharedIndexContainerInstance()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 1);   // ~0..10
        var shared = r.ContainerFromIndex(9);
        Assert.That(shared, Is.Not.Null);

        r.Realize(1000, 20, viewportTop: 100, viewportHeight: 200, cacheRows: 1); // ~4..15

        Assert.That(r.ContainerFromIndex(9), Is.SameAs(shared), "index 9 stays in window -> same instance");
    }

    [Test]
    public void RecyclingReusesContainerInstancesAcrossDisjointWindows()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 0);     // 0..9
        var createdAfterTop = h.Created;
        Assert.That(createdAfterTop, Is.EqualTo(10));

        // Jump far away — fully disjoint window. The 10 cleared containers should
        // be recycled to serve the 10 new indices, so no new creation.
        r.Realize(1000, 20, viewportTop: 5000, viewportHeight: 200, cacheRows: 0);  // 250..259

        Assert.That(h.Cleared, Is.EqualTo(10));
        Assert.That(h.Created, Is.EqualTo(createdAfterTop), "recycled instances reused, no new creates");
        Assert.That(r.Realized.Count, Is.EqualTo(10));
        Assert.That(r.RecyclePoolCount, Is.EqualTo(0), "pool drained back into the window");
    }

    [Test]
    public void StandardModeDoesNotReuseAndDoesNotPool()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: false);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 0);    // 0..9
        r.Realize(1000, 20, viewportTop: 5000, viewportHeight: 200, cacheRows: 0); // 250..259

        Assert.That(h.Cleared, Is.EqualTo(10));
        Assert.That(r.RecyclePoolCount, Is.EqualTo(0), "standard mode never pools");
        Assert.That(h.Created, Is.EqualTo(20), "standard mode re-creates for the new window");
    }

    [Test]
    public void RealizedCountNeverExceedsWindowSize()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        for (double top = 0; top <= 4000; top += 137)
        {
            var layout = r.Realize(1000, 20, viewportTop: top, viewportHeight: 200, cacheRows: 2);
            Assert.That(r.Realized.Count, Is.EqualTo(layout.Count));
            Assert.That(r.Realized.Count, Is.LessThanOrEqualTo(16));
        }
    }

    [Test]
    public void ClearRecyclesAllRealizedContainers()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, 0, 200, cacheRows: 0); // 10 realized
        r.Clear();

        Assert.That(r.Realized.Count, Is.EqualTo(0));
        Assert.That(h.Cleared, Is.EqualTo(10));
        Assert.That(r.RecyclePoolCount, Is.EqualTo(10));
    }

    [Test]
    public void PreparePassesCorrectIndexOnRecycledReuse()
    {
        var h = new Harness();
        var r = h.Make(1000, recycling: true);

        r.Realize(1000, 20, viewportTop: 0, viewportHeight: 200, cacheRows: 0);     // 0..9
        h.PrepareLog.Clear();
        r.Realize(1000, 20, viewportTop: 5000, viewportHeight: 200, cacheRows: 0);  // 250..259

        Assert.That(h.PrepareLog.Count, Is.EqualTo(10));
        foreach (var (_, index) in h.PrepareLog)
        {
            Assert.That(index, Is.InRange(250, 259));
        }
    }
}
