namespace System.Windows.Media
{
    public class TextDecorationCollection : Collections.Generic.IList<TextDecoration>
    {
        readonly Collections.Generic.List<TextDecoration> _items;
        public static readonly TextDecorationCollection Empty = new();

        public TextDecorationCollection() { _items = new Collections.Generic.List<TextDecoration>(); }

        public TextDecorationCollection(Collections.Generic.IEnumerable<TextDecoration> items)
        {
            _items = new Collections.Generic.List<TextDecoration>(items ?? Array.Empty<TextDecoration>());
        }

        public bool IsFrozen => true;
        public TextDecorationCollection Clone() => new TextDecorationCollection(_items);

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public TextDecoration this[int index] { get => _items[index]; set => _items[index] = value; }
        public void Add(TextDecoration item) => _items.Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(TextDecoration item) => _items.Contains(item);
        public void CopyTo(TextDecoration[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public Collections.Generic.IEnumerator<TextDecoration> GetEnumerator() => _items.GetEnumerator();
        Collections.IEnumerator Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(TextDecoration item) => _items.IndexOf(item);
        public void Insert(int index, TextDecoration item) => _items.Insert(index, item);
        public bool Remove(TextDecoration item) => _items.Remove(item);
        public void RemoveAt(int index) => _items.RemoveAt(index);

        // WPF TryRemove: removes decorations matching toRemove and returns the modified collection.
        // Returns false if nothing was removed (caller should then add instead).
        public bool TryRemove(TextDecorationCollection toRemove, out TextDecorationCollection result)
        {
            var locations = new Collections.Generic.List<TextDecorationLocation>();
            foreach (var dec in (toRemove ?? Empty))
                locations.Add(dec.Location);

            var items = new Collections.Generic.List<TextDecoration>(_items);
            foreach (var location in locations)
                items.RemoveAll(d => d.Location == location);
            result = new TextDecorationCollection(items);
            return items.Count < Count;
        }

        public bool ValueEquals(TextDecorationCollection? other)
        {
            if (other is null) return Count == 0;
            if (Count != other.Count) return false;
            for (int i = 0; i < Count; i++)
            {
                if (_items[i].Location != other._items[i].Location) return false;
            }
            return true;
        }
    }
}
