namespace System.Windows.Documents;

internal static class SR
{
    internal const string TextSchema_ChildTypeIsInvalid = "TextSchema_ChildTypeIsInvalid";
    internal const string InDifferentTextContainers = "InDifferentTextContainers";
    internal const string BadTextPositionOrder = "BadTextPositionOrder";
    internal const string InDifferentParagraphs = "InDifferentParagraphs";
    internal const string TextSchema_CannotSplitElement = "TextSchema_CannotSplitElement";
    internal const string NonWhiteSpaceInAddText = "NonWhiteSpaceInAddText";
    internal const string UnexpectedParameterType = "UnexpectedParameterType";
    internal const string TableInvalidParentNodeType = "TableInvalidParentNodeType";
    internal const string TableCollectionElementTypeExpected = "TableCollectionElementTypeExpected";
    internal const string EnumeratorVersionChanged = "EnumeratorVersionChanged";
    internal const string EnumeratorNotStarted = "EnumeratorNotStarted";
    internal const string EnumeratorReachedEnd = "EnumeratorReachedEnd";

    internal static string Format(string format, params object[] args)
    {
        return string.Format(format, args);
    }
}
