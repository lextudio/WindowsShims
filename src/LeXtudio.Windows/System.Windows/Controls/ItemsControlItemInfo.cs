namespace System.Windows.Controls;

// Bridge subset of WPF ItemsControl.ItemInfo for linked DataGrid/selector
// sources. WPF creates its sentinel containers eagerly in a static
// constructor; on Uno a bare DependencyObject cannot be constructed (it is an
// interface), so the sentinels are generated partial instances created
// lazily, and equality checks the backing fields null-safely so plain CLR
// paths (and unit tests) never force construction. Generator Refresh is
// omitted until an item container generator exists.
public partial class ItemsControl
{
    internal static bool EqualsEx(object? o1, object? o2)
    {
        try
        {
            return Equals(o1, o2);
        }
        catch (InvalidCastException)
        {
            // A common programming error: the type of o1 overrides Equals(object o2)
            // but mistakenly assumes that o2 has the same type as o1. Rather than
            // crash, just return false - the objects are clearly unequal.
            return false;
        }
    }

    internal class ItemInfo
    {
        private static SentinelContainerObject? _sentinelContainer;
        private static SentinelContainerObject? _unresolvedContainer;
        private static SentinelContainerObject? _keyContainer;
        private static SentinelContainerObject? _removedContainer;

        public ItemInfo(object? item, DependencyObject? container = null, int index = -1)
        {
            Item = item;
            Container = container;
            Index = index;
        }

        internal static DependencyObject SentinelContainer => _sentinelContainer ??= new SentinelContainerObject();

        internal static DependencyObject UnresolvedContainer => _unresolvedContainer ??= new SentinelContainerObject();

        internal static DependencyObject KeyContainer => _keyContainer ??= new SentinelContainerObject();

        internal static DependencyObject RemovedContainer => _removedContainer ??= new SentinelContainerObject();

        internal object? Item { get; private set; }

        internal DependencyObject? Container { get; set; }

        internal int Index { get; set; }

        internal bool IsResolved => !IsUnresolvedContainer(Container);

        internal bool IsKey => IsKeyContainer(Container);

        internal bool IsRemoved => IsRemovedContainer(Container);

        internal ItemInfo Clone() => new(Item, Container, Index);

        internal static ItemInfo Key(ItemInfo info)
            => IsUnresolvedContainer(info.Container)
                ? new ItemInfo(info.Item, KeyContainer, -1)
                : info;

        internal void Reset(object? item)
        {
            Item = item;
        }

        public override int GetHashCode() => Item?.GetHashCode() ?? 314159;

        public override bool Equals(object? o)
        {
            if (ReferenceEquals(o, this))
            {
                return true;
            }

            if (o is not ItemInfo that)
            {
                return false;
            }

            return Equals(that, matchUnresolved: false);
        }

        internal bool Equals(ItemInfo that, bool matchUnresolved)
        {
            if (IsRemoved || that.IsRemoved)
            {
                return false;
            }

            if (!EqualsEx(Item, that.Item))
            {
                return false;
            }

            if (IsKeyContainer(Container))
            {
                return matchUnresolved || !IsUnresolvedContainer(that.Container);
            }

            if (IsKeyContainer(that.Container))
            {
                return matchUnresolved || !IsUnresolvedContainer(Container);
            }

            if (IsUnresolvedContainer(Container) || IsUnresolvedContainer(that.Container))
            {
                return false;
            }

            return Container == that.Container
                ? IsSentinelContainer(Container)
                    ? Index == that.Index
                    : Index < 0 || that.Index < 0 || Index == that.Index
                : IsSentinelContainer(Container) ||
                  IsSentinelContainer(that.Container) ||
                  ((Container is null || that.Container is null) &&
                   (Index < 0 || that.Index < 0 || Index == that.Index));
        }

        public static bool operator ==(ItemInfo? info1, ItemInfo? info2) => Equals(info1, info2);

        public static bool operator !=(ItemInfo? info1, ItemInfo? info2) => !Equals(info1, info2);

        private static bool IsSentinelContainer(DependencyObject? container)
            => container is not null && ReferenceEquals(container, _sentinelContainer);

        private static bool IsUnresolvedContainer(DependencyObject? container)
            => container is not null && ReferenceEquals(container, _unresolvedContainer);

        private static bool IsKeyContainer(DependencyObject? container)
            => container is not null && ReferenceEquals(container, _keyContainer);

        private static bool IsRemovedContainer(DependencyObject? container)
            => container is not null && ReferenceEquals(container, _removedContainer);
    }
}

// Marker dependency object for the ItemInfo sentinels. Partial so the Uno
// generator supplies the DependencyObject implementation.
internal sealed partial class SentinelContainerObject : DependencyObject
{
}
