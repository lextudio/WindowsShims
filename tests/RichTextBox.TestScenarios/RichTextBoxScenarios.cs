using System.Windows;
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
    public static FlowDocument BuildListDocument(params string[] itemTexts) =>
        BuildListDocument(TextMarkerStyle.Disc, itemTexts);

    public static FlowDocument BuildListDocument(TextMarkerStyle markerStyle, params string[] itemTexts)
    {
        var document = new FlowDocument();
        var list = new List { MarkerStyle = markerStyle };
        foreach (var text in itemTexts)
        {
            list.ListItems.Add(new ListItem(new Paragraph(new Run(text))));
        }
        document.Blocks.Add(list);
        return document;
    }

    public static FlowDocument BuildHyperlinkDocument(string beforeText, string linkText, string afterText, Uri? navigateUri = null)
    {
        var document = new FlowDocument();
        var hyperlink = new Hyperlink(new Run(linkText)) { NavigateUri = navigateUri };
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new Run(beforeText));
        paragraph.Inlines.Add(hyperlink);
        paragraph.Inlines.Add(new Run(afterText));
        document.Blocks.Add(paragraph);
        return document;
    }

    // Builds a 2x2 Table/TableRowGroup/TableRow/TableCell tree directly via
    // constructors, mirroring BuildListDocument's approach for lists. Tables have
    // no visual rendering support in this shim's Florence layout engine (it only
    // walks document.Blocks.OfType<Paragraph>()), so this exists purely to probe
    // whether the document-model/editing layer (TextRangeEditTables, Parent
    // chains) construct and read back correctly.
    public static FlowDocument BuildTableDocument(string cell00, string cell01, string cell10, string cell11)
    {
        var document = new FlowDocument();
        var table = new Table();
        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        var row0 = new TableRow();
        row0.Cells.Add(new TableCell(new Paragraph(new Run(cell00))));
        row0.Cells.Add(new TableCell(new Paragraph(new Run(cell01))));
        rowGroup.Rows.Add(row0);

        var row1 = new TableRow();
        row1.Cells.Add(new TableCell(new Paragraph(new Run(cell10))));
        row1.Cells.Add(new TableCell(new Paragraph(new Run(cell11))));
        rowGroup.Rows.Add(row1);

        document.Blocks.Add(table);
        return document;
    }
}
