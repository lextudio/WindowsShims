namespace System.Windows.Documents;

internal static class TextSchema
{
    private static readonly DependencyProperty[] EmptyProperties = [];

    internal static void ValidateChild(TextElement parent, Inline child, bool throwIfIllegalChild, bool throwIfIllegalHyperlinkDescendent)
    {
    }

    internal static bool IsValidChildOfContainer(Type parentType, Type childType)
    {
        return true;
    }

    internal static bool IsValidChild(TextPointer position, Type childType) => true;

    internal static bool IsValidChild(ITextPointer position, Type childType) => true;

    internal static bool ValidateChild(TextPointer position, Type childType, bool throwIfIllegalChild, bool throwIfIllegalHyperlinkDescendent) => true;

    internal static bool IsParagraphOrBlockUIContainer(Type type)
        => typeof(Paragraph).IsAssignableFrom(type) || typeof(BlockUIContainer).IsAssignableFrom(type);

    internal static bool IsCharacterProperty(DependencyProperty property)
    {
        return property == TextElement.FontWeightProperty
            || property == TextElement.FontStyleProperty;
    }

    internal static bool IsParagraphProperty(DependencyProperty property)
    {
        return property == Paragraph.TextIndentProperty;
    }

    internal static bool IsPropertyIncremental(DependencyProperty property)
    {
        return false;
    }

    internal static bool IsBreak(Type? elementType)
    {
        return elementType == typeof(LineBreak);
    }

    internal static DependencyProperty[] GetInheritableProperties(Type elementType)
    {
        return EmptyProperties;
    }

    internal static DependencyProperty[] GetNoninheritableProperties(Type elementType)
    {
        return EmptyProperties;
    }

    internal static bool IsInTextContent(ITextPointer position) => true;

    internal static bool IsFormattingType(Type type) => typeof(Inline).IsAssignableFrom(type);

    internal static bool IsNonFormattingInline(Type type)
        => typeof(Inline).IsAssignableFrom(type) && !IsFormattingType(type);

    internal static bool IsMergeableInline(Type type) => IsFormattingType(type);

    internal static bool IsKnownType(Type type) => true;
}
