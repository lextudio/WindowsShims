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
    internal const string TableCollectionOutOfRange = "TableCollectionOutOfRange";
    internal const string TableCollectionWrongProxyParent = "TableCollectionWrongProxyParent";
    internal const string TableCollectionInOtherCollection = "TableCollectionInOtherCollection";
    internal const string TableCollectionRankMultiDimNotSupported = "TableCollectionRankMultiDimNotSupported";
    internal const string TableCollectionOutOfRangeNeedNonNegNum = "TableCollectionOutOfRangeNeedNonNegNum";
    internal const string TableCollectionInvalidOffLen = "TableCollectionInvalidOffLen";
    internal const string TableCollectionCountNeedNonNegNum = "TableCollectionCountNeedNonNegNum";
    internal const string TableCollectionRangeOutOfRange = "TableCollectionRangeOutOfRange";
    internal const string TableCollectionNotEnoughCapacity = "TableCollectionNotEnoughCapacity";
    internal const string CanOnlyHaveOneChild = "CanOnlyHaveOneChild";
    internal const string RichTextBox_DocumentBelongsToAnotherRichTextBoxAlready = "RichTextBox_DocumentBelongsToAnotherRichTextBoxAlready";
    internal const string RichTextBox_CantSetDocumentInsideChangeBlock = "RichTextBox_CantSetDocumentInsideChangeBlock";
    internal const string TextEditorPropertyIsNotApplicableForTextFormatting = "TextEditorPropertyIsNotApplicableForTextFormatting";
    internal const string TextEditorTypeOfParameterIsNotAppropriateForFormattingProperty = "TextEditorTypeOfParameterIsNotAppropriateForFormattingProperty";
    internal const string TextRange_InvalidParameterValue = "TextRange_InvalidParameterValue";
    internal const string TextRange_PropertyCannotBeIncrementedOrDecremented = "TextRange_PropertyCannotBeIncrementedOrDecremented";

    internal static string Format(string format, params object[] args)
    {
        return string.Format(format, args);
    }
}
