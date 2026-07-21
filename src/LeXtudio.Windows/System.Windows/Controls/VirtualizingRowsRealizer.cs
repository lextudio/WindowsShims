namespace System.Windows.Controls;

// Session 119 (DataGrid virtualization, Slice 2): the windowed realize/recycle
// state machine — the heart of UI virtualization.
//
// Given the viewport slice computed by VirtualizingRowsLayout, this keeps exactly
// the in-window item containers realized: containers scrolled out of the window are
// cleared (and, in Recycling mode, returned to a pool that newly-scrolled-in items
// reuse), while containers already realized at an in-window index are kept as-is
// (no redundant prepare). It is decoupled from the live visual tree and from the
// existing ItemContainerGenerator via injected callbacks, so the algorithm is unit-
// testable now and can be wired to the real generator + host panel in later slices.
//
// Mirrors WPF semantics: VirtualizationMode.Recycling reuses container instances
// (PrepareContainerForItem on reuse), VirtualizationMode.Standard discards them.
//
// Generic over the container type so the algorithm is independent of the WinUI
// visual tree (UIElement instances cannot be created off the Uno UI thread, which
// would block headless unit tests). Live wiring uses DataGridRow as TContainer.
internal sealed class VirtualizingRowsRealizer<TContainer>
    where TContainer : class
{
    private readonly Func<int, object?> _itemAt;
    private readonly Func<object?, TContainer> _create;
    private readonly Action<TContainer, object?, int> _prepare;
    private readonly Action<TContainer, object?> _clear;
    private readonly bool _recycling;

    // Realized containers keyed by item index (the moving window), plus the item
    // each container currently presents (for the clear callback).
    private readonly Dictionary<int, TContainer> _realized = new();
    private readonly Dictionary<TContainer, object?> _itemOf = new();
    private readonly Queue<TContainer> _recyclePool = new();

    public VirtualizingRowsRealizer(
        Func<int, object?> itemAt,
        Func<object?, TContainer> create,
        Action<TContainer, object?, int> prepare,
        Action<TContainer, object?> clear,
        bool recycling)
    {
        _itemAt = itemAt ?? throw new ArgumentNullException(nameof(itemAt));
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
        _clear = clear ?? throw new ArgumentNullException(nameof(clear));
        _recycling = recycling;
    }

    /// <summary>Containers currently realized, keyed by item index.</summary>
    public IReadOnlyDictionary<int, TContainer> Realized => _realized;

    /// <summary>Container instances available for reuse (Recycling mode only).</summary>
    public int RecyclePoolCount => _recyclePool.Count;

    /// <summary>Container realized at <paramref name="index"/>, or null if not realized.</summary>
    public TContainer? ContainerFromIndex(int index)
        => _realized.TryGetValue(index, out var container) ? container : null;

    /// <summary>
    /// Realizes exactly the in-window slice for the given viewport, recycling everything else.
    /// Returns the computed layout (extent/offset) so the host can size and position itself.
    /// </summary>
    public VirtualizingRowsLayout Realize(
        int itemCount,
        double rowHeight,
        double viewportTop,
        double viewportHeight,
        int cacheRows = 1)
    {
        var layout = VirtualizingRowsLayout.Compute(itemCount, rowHeight, viewportTop, viewportHeight, cacheRows);
        RealizeWindow(layout.FirstIndex, layout.EndIndex);
        return layout;
    }

    /// <summary>
    /// The realize/recycle mechanics alone, for a caller that has already computed the
    /// window bounds itself (e.g. variable-row-height layout, which cannot use the
    /// uniform-height <see cref="VirtualizingRowsLayout.Compute"/> this <see cref="Realize"/>
    /// overload relies on). Recycles/removes containers outside [firstIndex, endIndex) and
    /// realizes any in-range index not already realized, exactly like <see cref="Realize"/>'s
    /// own loop — kept as a single implementation so both callers get identical
    /// keep-what's-still-in-window behavior.
    /// </summary>
    public void RealizeWindow(int firstIndex, int endIndex)
    {
        // Recycle/remove containers now outside the window. Snapshot keys first
        // because we mutate _realized while iterating.
        if (_realized.Count > 0)
        {
            var stale = new List<int>();
            foreach (var index in _realized.Keys)
            {
                if (index < firstIndex || index >= endIndex)
                {
                    stale.Add(index);
                }
            }

            foreach (var index in stale)
            {
                var container = _realized[index];
                _realized.Remove(index);
                _itemOf.Remove(container, out var item);
                _clear(container, item);
                if (_recycling)
                {
                    _recyclePool.Enqueue(container);
                }
            }
        }

        // Realize the window left-to-right, reusing containers already at their index.
        for (var index = firstIndex; index < endIndex; index++)
        {
            if (_realized.ContainsKey(index))
            {
                continue; // already realized at this index — keep as-is
            }

            var item = _itemAt(index);
            var container = _recycling && _recyclePool.Count > 0
                ? _recyclePool.Dequeue()
                : _create(item);

            _realized[index] = container;
            _itemOf[container] = item;
            _prepare(container, item, index);
        }
    }

    /// <summary>Clears all realized containers (e.g. on items reset). Recycling keeps the pool.</summary>
    public void Clear()
    {
        foreach (var pair in _realized)
        {
            _itemOf.Remove(pair.Value, out var item);
            _clear(pair.Value, item);
            if (_recycling)
            {
                _recyclePool.Enqueue(pair.Value);
            }
        }

        _realized.Clear();
        _itemOf.Clear();
    }
}
