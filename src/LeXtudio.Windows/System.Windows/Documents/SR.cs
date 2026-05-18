// SR is intentionally NOT in any namespace so it is accessible from all namespaces
// in this assembly (System.Windows.Documents, MS.Internal.Documents, etc.).
internal static class SR
{
    internal const string TextSchema_ChildTypeIsInvalid = "TextSchema_ChildTypeIsInvalid";
    internal const string InDifferentTextContainers = "InDifferentTextContainers";
    internal const string BadTextPositionOrder = "BadTextPositionOrder";
    internal const string NotInAssociatedTree = "NotInAssociatedTree: {0}";
    internal const string InDifferentParagraphs = "InDifferentParagraphs";
    internal const string TextSchema_CannotSplitElement = "TextSchema_CannotSplitElement";
    internal const string TextSchema_TheChildElementBelongsToAnotherTreeAlready = "TextSchema_TheChildElementBelongsToAnotherTreeAlready: {0}";
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
    // Undo-related keys used by ParentUndoUnit and UndoManager
    internal const string UndoUnitCantBeOpenedTwice      = nameof(UndoUnitCantBeOpenedTwice);
    internal const string UndoNoOpenUnit                 = nameof(UndoNoOpenUnit);
    internal const string UndoUnitNotFound               = nameof(UndoUnitNotFound);
    internal const string UndoUnitCantBeAddedTwice       = nameof(UndoUnitCantBeAddedTwice);
    internal const string UndoUnitLocked                 = nameof(UndoUnitLocked);
    internal const string UndoContainerTypeMismatch      = nameof(UndoContainerTypeMismatch);
    internal const string UndoManagerAlreadyAttached     = nameof(UndoManagerAlreadyAttached);
    internal const string UndoServiceDisabled            = nameof(UndoServiceDisabled);
    internal const string UndoUnitAlreadyOpen            = nameof(UndoUnitAlreadyOpen);
    internal const string UndoUnitNotOnTopOfStack        = nameof(UndoUnitNotOnTopOfStack);
    internal const string UndoNoOpenParentUnit           = nameof(UndoNoOpenParentUnit);
    internal const string UndoNotInNormalState           = nameof(UndoNotInNormalState);
    internal const string UndoUnitOpen                   = nameof(UndoUnitOpen);
    internal const string InputScopeAttribute_E_OUTOFMEMORY = "Insufficient memory to complete the operation.";

    internal const string TextContainer_UndoManagerCreatedMoreThanOnce = "TextContainer_UndoManagerCreatedMoreThanOnce";
    internal const string BadDistance = "BadDistance";
    internal const string NotInThisTree = "NotInThisTree";
    internal const string NoElement = "NoElement";
    internal const string TextPointer_CannotInsertTextElementBecauseItBelongsToAnotherTree = "TextPointer_CannotInsertTextElementBecauseItBelongsToAnotherTree";
    internal const string TextSchema_IllegalElement = "TextSchema_IllegalElement";
    internal const string NegativeValue = "NegativeValue";
    internal const string StartIndexExceedsBufferSize = "StartIndexExceedsBufferSize";
    internal const string MaxLengthExceedsBufferSize = "MaxLengthExceedsBufferSize";
    internal const string TextSchema_UIElementNotAllowedInThisPosition = "TextSchema_UIElementNotAllowedInThisPosition";
    internal const string NoScopingElement = "NoScopingElement";
    internal const string InDifferentScope = "InDifferentScope";
    internal const string TextSchema_CannotInsertContentInThisPosition = "TextSchema_CannotInsertContentInThisPosition";
    internal const string TextPositionIsFrozen = "TextPositionIsFrozen";

    internal const string TextElement_UnmatchedEndPointer = "TextElement_UnmatchedEndPointer";
    internal const string TextSchema_ThisInlineUIContainerHasAChildUIElementAlready = "TextSchema_ThisInlineUIContainerHasAChildUIElementAlready";
    internal const string TextSchema_ThisBlockUIContainerHasAChildUIElementAlready = "TextSchema_ThisBlockUIContainerHasAChildUIElementAlready";
    internal const string TextSchema_TextIsNotAllowed = "TextSchema_TextIsNotAllowed";
    internal const string TextElement_ChildTypeIsInvalid = "TextElement_ChildTypeIsInvalid";

    internal static string Format(string format, params object[] args)
    {
        return string.Format(format, args);
    }
}
