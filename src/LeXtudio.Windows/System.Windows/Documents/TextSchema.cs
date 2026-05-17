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

    internal static bool IsValidChild(TextPointer position, Type childType)
    {
        return true;
    }

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
}
