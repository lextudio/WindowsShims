namespace System.Windows.Documents;

internal static class FrameworkAppContextSwitches
{
    internal static bool UseAdornerForTextboxSelectionRendering => false;
}

internal sealed class PasswordTextContainer : TextContainer
{
    internal PasswordTextContainer()
        : base(null)
    {
    }
}
