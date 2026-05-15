// Shims for WPF markup attributes used in PresentationFramework source files.
namespace System.Windows.Markup
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ContentPropertyAttribute : Attribute
    {
        public ContentPropertyAttribute() { }
        public ContentPropertyAttribute(string name) { Name = name; }
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ContentWrapperAttribute : Attribute
    {
        public ContentWrapperAttribute(Type contentWrapper) { ContentWrapper = contentWrapper; }
        public Type ContentWrapper { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class WhitespaceSignificantCollectionAttribute : Attribute
    {
        public WhitespaceSignificantCollectionAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TrimSurroundingWhitespaceAttribute : Attribute
    {
        public TrimSurroundingWhitespaceAttribute() { }
    }
}
