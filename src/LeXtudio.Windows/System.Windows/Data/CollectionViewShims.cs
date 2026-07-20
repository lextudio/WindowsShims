using System.Collections.ObjectModel;
using System.Globalization;

namespace System.Windows.Data
{
    // Minimal stand-in for WPF's CollectionView: the control root only
    // references the new-item placeholder sentinel. The shim ItemCollection
    // never produces the placeholder, so identity checks are simply false.
    public class CollectionView
    {
        public static object NewItemPlaceholder { get; } = new NamedSentinel("NewItemPlaceholder");

        private sealed class NamedSentinel(string name)
        {
            public override string ToString() => $"{{{name}}}";
        }
    }

    // Session 121 (DataGrid grouping, Slice 1): subset of WPF's abstract
    // CollectionViewGroup — a named node holding either leaf items (bottom
    // level) or nested CollectionViewGroup subgroups (higher levels). GroupItem
    // (the container ItemsControl's group-aware generation produces) sets its
    // Content directly to one of these instances.
    public abstract class CollectionViewGroup
    {
        private readonly ObservableCollection<object?> _items = new();
        private readonly ReadOnlyObservableCollection<object?> _readOnlyItems;

        protected CollectionViewGroup(object? name)
        {
            Name = name;
            _readOnlyItems = new ReadOnlyObservableCollection<object?>(_items);
        }

        public object? Name { get; }

        public ReadOnlyObservableCollection<object?> Items => _readOnlyItems;

        public virtual int ItemCount => _items.Count;

        public abstract bool IsBottomLevel { get; }

        protected ObservableCollection<object?> ProtectedItems => _items;
    }
}

namespace MS.Internal.Data
{
    // Session 121 (DataGrid grouping, Slice 1): a real (not stubbed) group node.
    // Built by CollectionViewGroupBuilder from a flat, already sorted/filtered
    // item sequence plus the ordered GroupDescriptions. Bottom-level groups hold
    // leaf items directly in Items; higher levels hold nested
    // CollectionViewGroupInternal instances (still exposed via the same Items
    // property, matching upstream WPF's shape) so DataGrid's IsGrouping-gated
    // code (recursively summing ItemCount, walking subgroups) works unchanged.
    internal sealed class CollectionViewGroupInternal : System.Windows.Data.CollectionViewGroup
    {
        private readonly bool _isBottomLevel;

        internal CollectionViewGroupInternal(object? name, System.ComponentModel.GroupDescription? groupBy, bool isBottomLevel)
            : base(name)
        {
            GroupBy = groupBy;
            _isBottomLevel = isBottomLevel;
        }

        internal System.ComponentModel.GroupDescription? GroupBy { get; }

        public override bool IsBottomLevel => _isBottomLevel;

        // Session 121 (DataGrid grouping, Slice 4): expand/collapse state. Shim-only
        // — real WPF has no engine-level collapse API (see docs/session121.md); it's
        // purely a GroupStyle.HeaderTemplate/Expander template convention there. This
        // shim needs *some* signal so CollectionViewGroupBuilder.FlattenWithHeaders
        // can exclude a collapsed group's children from the realized slot sequence.
        // Defaults true (fully expanded), matching every group's appearance before
        // this slice existed.
        internal bool IsExpanded { get; set; } = true;

        public override int ItemCount
        {
            get
            {
                if (_isBottomLevel)
                {
                    return base.ItemCount;
                }

                var count = 0;
                foreach (var sub in Items)
                {
                    count += ((CollectionViewGroupInternal)sub!).ItemCount;
                }

                return count;
            }
        }

        internal void AddLeaf(object? item) => ProtectedItems.Add(item);

        internal void AddSubgroup(CollectionViewGroupInternal subgroup) => ProtectedItems.Add(subgroup);

        // WPF's CollectionViewGroupInternal.LeafIndexFromItem: the flat (leaf) index
        // of `item` within this group's subtree, or -1 if not found. Called by linked
        // upstream DataGrid.cs's grouped keyboard-navigation code (OnKeyDown's
        // group-boundary check) now that IsGrouping can be true (session 121).
        internal int LeafIndexFromItem(object? item, int startLeafIndex)
        {
            if (_isBottomLevel)
            {
                var index = 0;
                foreach (var leaf in Items)
                {
                    if (Equals(leaf, item))
                    {
                        return startLeafIndex + index;
                    }

                    index++;
                }

                return -1;
            }

            var offset = startLeafIndex;
            foreach (var sub in Items)
            {
                var group = (CollectionViewGroupInternal)sub!;
                var found = group.LeafIndexFromItem(item, offset);
                if (found >= 0)
                {
                    return found;
                }

                offset += group.ItemCount;
            }

            return -1;
        }

        // Session 121 (DataGrid grouping, Slice 4): the index of `item` in the same
        // "visual slot" index space CollectionViewGroupBuilder.FlattenWithHeaders
        // produces — i.e. counting one extra header slot per group (at any depth)
        // ahead of that group's own leaves/subgroups. Used to translate a flat data
        // item into VirtualizingStackPanel's grouped-realizer slot index for
        // scroll-into-view under grouping, without rebuilding the flattened list
        // just to find one index.
        internal int SlotIndexFromItem(object? item, int startSlotIndex)
        {
            var index = startSlotIndex + 1; // this group's own header slot
            if (!IsExpanded)
            {
                return -1; // collapsed: nothing below the header slot is realized
            }

            if (_isBottomLevel)
            {
                foreach (var leaf in Items)
                {
                    if (Equals(leaf, item))
                    {
                        return index;
                    }

                    index++;
                }

                return -1;
            }

            foreach (var sub in Items)
            {
                var group = (CollectionViewGroupInternal)sub!;
                var found = group.SlotIndexFromItem(item, index);
                if (found >= 0)
                {
                    return found;
                }

                index += group.SlotCount;
            }

            return -1;
        }

        // Total visual slots (this group's own header + all leaves/subgroup headers
        // recursively) this group's subtree occupies in FlattenWithHeaders' sequence.
        // Collapsed groups occupy just their own header slot.
        internal int SlotCount
            => 1 + (IsExpanded
                ? (_isBottomLevel ? ItemCount : Items.Sum(sub => ((CollectionViewGroupInternal)sub!).SlotCount))
                : 0);
    }

    // Builds the nested CollectionViewGroupInternal tree for a flat item
    // sequence given an ordered list of GroupDescriptions, one level per
    // description. Groups are ordered by first encounter in the source
    // sequence (matching WPF, which does not alphabetize group names), so
    // grouping composes with a prior sort: sort first, then group, and the
    // group order follows the sorted sequence.
    internal static class CollectionViewGroupBuilder
    {
        internal static List<CollectionViewGroupInternal> BuildGroups(
            IEnumerable<object?> items,
            IReadOnlyList<System.ComponentModel.GroupDescription> groupDescriptions)
        {
            return BuildLevel(items, groupDescriptions, 0);
        }

        private static List<CollectionViewGroupInternal> BuildLevel(
            IEnumerable<object?> items,
            IReadOnlyList<System.ComponentModel.GroupDescription> descriptions,
            int level)
        {
            var description = descriptions[level];
            var isBottomLevel = level == descriptions.Count - 1;

            var order = new List<object?>();
            var buckets = new Dictionary<object?, List<object?>>(EqualityComparer<object?>.Default);
            foreach (var item in items)
            {
                var name = description.GroupNameFromItem(item, level, CultureInfo.CurrentCulture);
                if (!buckets.TryGetValue(name, out var bucket))
                {
                    bucket = new List<object?>();
                    buckets.Add(name, bucket);
                    order.Add(name);
                }

                bucket.Add(item);
            }

            var groups = new List<CollectionViewGroupInternal>(order.Count);
            foreach (var name in order)
            {
                var group = new CollectionViewGroupInternal(name, description, isBottomLevel);
                var bucketItems = buckets[name];
                if (isBottomLevel)
                {
                    foreach (var item in bucketItems)
                    {
                        group.AddLeaf(item);
                    }
                }
                else
                {
                    foreach (var subgroup in BuildLevel(bucketItems, descriptions, level + 1))
                    {
                        group.AddSubgroup(subgroup);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        // Flattens a group tree back to a leaf-item sequence, in group order —
        // the order ItemCollection's flat backing list adopts once grouping is
        // active (items sharing a group become contiguous even if the prior sort
        // interleaved them).
        internal static IEnumerable<object?> FlattenLeaves(IEnumerable<CollectionViewGroupInternal> groups)
        {
            foreach (var group in groups)
            {
                if (group.IsBottomLevel)
                {
                    foreach (var leaf in group.Items)
                    {
                        yield return leaf;
                    }
                }
                else
                {
                    foreach (var leaf in FlattenLeaves(group.Items.Cast<CollectionViewGroupInternal>()))
                    {
                        yield return leaf;
                    }
                }
            }
        }

        // Session 121 (DataGrid grouping, Slice 3): like FlattenLeaves, but keeps
        // a GroupHeaderSlot marker in the sequence ahead of each group's own
        // leaves/subgroups, at the nesting depth of that group — the "visual
        // slot" sequence a virtualizing panel realizes one container per index
        // over (a GroupItem for a header slot, whatever the leaf item's own
        // container is for a data slot), mirroring how real WPF's
        // ItemContainerGenerator interleaves CollectionViewGroup objects with
        // data items when IsGrouping is true.
        internal static List<object?> FlattenWithHeaders(IReadOnlyList<CollectionViewGroupInternal> groups)
        {
            var slots = new List<object?>();
            AppendWithHeaders(slots, groups, depth: 0);
            return slots;
        }

        private static void AppendWithHeaders(List<object?> slots, IReadOnlyList<CollectionViewGroupInternal> groups, int depth)
        {
            foreach (var group in groups)
            {
                slots.Add(new GroupHeaderSlot(group, depth));
                if (!group.IsExpanded)
                {
                    // Collapsed: only the header slot itself is realized — its
                    // leaves/subgroups are excluded from the sequence entirely.
                    continue;
                }

                if (group.IsBottomLevel)
                {
                    foreach (var leaf in group.Items)
                    {
                        slots.Add(leaf);
                    }
                }
                else
                {
                    AppendWithHeaders(slots, group.Items.Cast<CollectionViewGroupInternal>().ToList(), depth + 1);
                }
            }
        }
    }

    // Marker wrapping a group in the flattened "visual slot" sequence
    // FlattenWithHeaders produces, so a slot consumer (VirtualizingStackPanel's
    // grouped realizer callbacks) can tell a group-header slot apart from a
    // real data item without the two ever being confusable (a real item could
    // coincidentally be a CollectionViewGroupInternal-shaped object otherwise).
    internal sealed class GroupHeaderSlot(CollectionViewGroupInternal group, int depth)
    {
        internal CollectionViewGroupInternal Group { get; } = group;

        internal int Depth { get; } = depth;
    }
}
