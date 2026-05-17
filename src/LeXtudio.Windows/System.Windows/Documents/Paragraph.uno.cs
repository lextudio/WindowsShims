namespace System.Windows.Documents;

[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Inlines))]
public partial class Paragraph
{
    private InlineCollection? _inlines;
}
