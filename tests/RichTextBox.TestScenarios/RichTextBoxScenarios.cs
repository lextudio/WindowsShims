using System.Windows.Controls;
using System.Windows.Documents;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;

namespace RichTextBox.TestScenarios;

public static class RichTextBoxScenarios
{
    public static WpfRichTextBox BuildPlainTextBox(string text)
    {
        var box = new WpfRichTextBox
        {
            Width = 640,
            Height = 240,
            AcceptsReturn = true,
        };

        box.AppendText(text);
        return box;
    }

    public static FlowDocument BuildSimpleDocument(string text)
    {
        var document = new FlowDocument();
        document.Blocks.Add(new Paragraph(new Run(text)));
        return document;
    }

    // Builds the List/ListItem tree directly via constructors, bypassing List.Apply
    // (TextRangeEditLists.ConvertParagraphsToListItems), which throws NotSupportedException
    // under HAS_UNO. This lets tests exercise indent/outdent and marker-removal on an
    // already-existing list without needing new-list creation to be implemented.
    public static FlowDocument BuildListDocument(params string[] itemTexts)
    {
        var document = new FlowDocument();
        var list = new List();
        foreach (var text in itemTexts)
        {
            list.ListItems.Add(new ListItem(new Paragraph(new Run(text))));
        }
        document.Blocks.Add(list);
        return document;
    }
}
