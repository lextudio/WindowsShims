using System.Collections.ObjectModel;

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
}

namespace MS.Internal.Data
{
    // Grouping-navigation stub: reachable code is gated on IsGrouping, which
    // the shim ItemsControl reports as false.
    internal class CollectionViewGroupInternal
    {
        public ReadOnlyObservableCollection<object?> Items { get; } = new([]);

        public int ItemCount => Items.Count;

        internal int LeafIndexFromItem(object? item, int index) => -1;
    }
}
