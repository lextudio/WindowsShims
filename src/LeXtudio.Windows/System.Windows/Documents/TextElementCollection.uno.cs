using System.Collections.Specialized;

namespace System.Windows.Documents;

// Uno partial: bridges upstream WPF mutation paths to the
// INotifyCollectionChanged contract that RichTextBlock relies on. Upstream
// TextElementCollection invokes OnUnoCollectionChanged after every Add/Remove/
// Clear/Insert; this partial fans those out as CollectionChanged events.
public partial class TextElementCollection<TextElementType> : INotifyCollectionChanged
    where TextElementType : TextElement
{
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    partial void OnUnoCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, args);
    }

    public TextElementType this[int index] => (TextElementType)((System.Collections.IList)this)[index]!;
}
