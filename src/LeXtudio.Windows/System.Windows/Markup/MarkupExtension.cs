namespace System.Windows.Markup
{
    /// <summary>Compiler shim for the WPF markup extension base class.</summary>
    public abstract class MarkupExtension
    {
        public abstract object ProvideValue(IServiceProvider serviceProvider);
    }
}
