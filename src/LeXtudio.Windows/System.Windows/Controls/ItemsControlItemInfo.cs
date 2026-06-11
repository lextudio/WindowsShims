namespace System.Windows.Controls;

// Bridge subset of WPF ItemsControl.ItemInfo for linked DataGrid sources.
// The WPF sentinel containers (Sentinel/Unresolved/Key/Removed) are omitted
// because they require constructing bare DependencyObject instances (dispatcher
// bound on Uno) and only matter for the virtualization/resolution paths that
// are not linked yet; equality keeps the item/container/index subset.
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
        public ItemInfo(object? item, DependencyObject? container = null, int index = -1)
        {
            Item = item;
            Container = container;
            Index = index;
        }

        internal object? Item { get; private set; }

        internal DependencyObject? Container { get; set; }

        internal int Index { get; set; }

        internal ItemInfo Clone() => new(Item, Container, Index);

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

            if (!EqualsEx(Item, that.Item))
            {
                return false;
            }

            return Container == that.Container
                ? Index < 0 || that.Index < 0 || Index == that.Index
                : (Container is null || that.Container is null) &&
                  (Index < 0 || that.Index < 0 || Index == that.Index);
        }

        public static bool operator ==(ItemInfo? info1, ItemInfo? info2) => Equals(info1, info2);

        public static bool operator !=(ItemInfo? info1, ItemInfo? info2) => !Equals(info1, info2);
    }
}
