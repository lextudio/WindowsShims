namespace System.Windows.Documents;

public partial class Run
{
    // Note: do NOT use the Text property here. Uno's DP system stores a
    // DeferredRunTextReference (set via SetCurrentDeferredValue in OnTextUpdated)
    // and doesn't auto-resolve it on GetValue, so the (string) cast in
    // Run.get_Text throws InvalidCastException. Compute emptiness from the
    // Run's own content boundaries instead.
    internal new bool IsEmpty => ContentStart.CompareTo(ContentEnd) == 0;

    internal static Run CreateImplicitRun(object parent)
    {
        return new Run();
    }

    internal static InlineUIContainer CreateImplicitInlineUIContainer(object parent)
    {
        return new InlineUIContainer();
    }
}
