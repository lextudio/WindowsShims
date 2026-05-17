namespace System.Windows.Documents;

[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Text))]
public partial class Run
{
    internal bool IsEmpty => string.IsNullOrEmpty(Text);

    internal static Run CreateImplicitRun(object parent)
    {
        return new Run();
    }

    internal static InlineUIContainer CreateImplicitInlineUIContainer(object parent)
    {
        return new InlineUIContainer();
    }
}
