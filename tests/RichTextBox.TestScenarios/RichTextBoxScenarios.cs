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
}
