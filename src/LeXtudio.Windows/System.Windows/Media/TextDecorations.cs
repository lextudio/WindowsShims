namespace System.Windows.Media
{
    public static class TextDecorations
    {
        public static TextDecorationCollection Underline { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Underline) });
        public static TextDecorationCollection Strikethrough { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Strikethrough) });
        public static TextDecorationCollection Overline { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Overline) });
        public static TextDecorationCollection Baseline { get; } =
            new TextDecorationCollection(new[] { new TextDecoration(TextDecorationLocation.Baseline) });
    }
}
