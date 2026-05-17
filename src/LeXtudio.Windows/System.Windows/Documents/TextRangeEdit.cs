namespace System.Windows.Documents;

internal static class TextRangeEdit
{
    internal static TextPointer SplitElement(TextPointer position) => position;

    internal static TextAlignment GetTextAlignmentFromHorizontalAlignment(HorizontalAlignment alignment)
        => alignment switch
        {
            HorizontalAlignment.Center => TextAlignment.Center,
            HorizontalAlignment.Right => TextAlignment.Right,
            HorizontalAlignment.Stretch => TextAlignment.Justify,
            _ => TextAlignment.Left,
        };

    internal static TextPointer SplitFormattingElements(TextPointer position, bool keepEmptyFormatting)
        => position;

    internal static void SetParagraphProperty(TextPointer start, TextPointer end, DependencyProperty property, object? value)
    {
    }

    internal static void SetParagraphProperty(TextPointer start, TextPointer end, DependencyProperty property, object? value, PropertyValueAction propertyValueAction)
    {
    }

    internal static void SetInlineProperty(TextPointer start, TextPointer end, DependencyProperty property, object? value)
    {
    }

    internal static void SetInlineProperty(TextPointer start, TextPointer end, DependencyProperty property, object? value, PropertyValueAction propertyValueAction)
    {
    }

    internal static void CharacterResetFormatting(TextRange range, bool includeParagraphProperties)
    {
    }

    internal static void CharacterResetFormatting(TextPointer start, TextPointer end)
    {
    }
}
