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
            if (!string.IsNullOrEmpty(new TextRange(run.ContentStart, run.ContentEnd).Text))
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
