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

    partial void OnUnoItemAdded(TextElementType item)
    {
        HydrateRunText(item);
    }

    private static void HydrateRunText(TextElement item)
    {
        if (item is Run run)
        {
            // "Already hydrated?" must not go through TextRange's plain-text getter: that
            // walks the full document plain-text serializer (list marker computation
            // included), which null-derefs on ListItem.SiblingListItems when this Run's
            // enclosing ListItem hasn't been added to a List yet — a normal, transient
            // state while building a ListItem via its constructor. A raw symbol-count
            // check has nothing to do with list/marker formatting and is always safe.
            if (run.ContentStart.GetOffsetToPosition(run.ContentEnd) > 0)
                return;

            string text = run.GetValue(Run.TextProperty) as string ?? string.Empty;
            if (!string.IsNullOrEmpty(text))
                run.ContentStart.InsertTextInRun(text);

            return;
        }

        if (item is Span span)
        {
            foreach (Inline inline in span.Inlines)
                HydrateRunText(inline);
        }
        else if (item is Paragraph paragraph)
        {
            foreach (Inline inline in paragraph.Inlines)
                HydrateRunText(inline);
        }
    }

    public TextElementType this[int index] => (TextElementType)((System.Collections.IList)this)[index]!;
}
