namespace System.Windows.Media
{
    public class TextEffectCollection : Collections.Generic.IList<TextEffect>
    {
        readonly Collections.Generic.List<TextEffect> _items = new Collections.Generic.List<TextEffect>();

        public bool IsFrozen => true;
        public TextEffectCollection Clone() => new TextEffectCollection();

        public int Count => _items.Count;
        public bool IsReadOnly => false;
        public TextEffect this[int index] { get => _items[index]; set => _items[index] = value; }
        public void Add(TextEffect item) => _items.Add(item);
        public void Clear() => _items.Clear();
        public bool Contains(TextEffect item) => _items.Contains(item);
        public void CopyTo(TextEffect[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public Collections.Generic.IEnumerator<TextEffect> GetEnumerator() => _items.GetEnumerator();
        Collections.IEnumerator Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
        public int IndexOf(TextEffect item) => _items.IndexOf(item);
        public void Insert(int index, TextEffect item) => _items.Insert(index, item);
        public bool Remove(TextEffect item) => _items.Remove(item);
        public void RemoveAt(int index) => _items.RemoveAt(index);
    }
}
