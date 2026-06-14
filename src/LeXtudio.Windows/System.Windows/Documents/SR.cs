// SR is intentionally NOT in any namespace so it is accessible from all namespaces
// in this assembly (System.Windows.Documents, MS.Internal.Documents, etc.).
internal static class SR
{
    internal const string FlowDocumentInvalidContnetChange = "FlowDocumentInvalidContnetChange";
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
    internal const string RichTextBox_PointerNotInSameDocument = "RichTextBox_PointerNotInSameDocument";
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
    internal const string DataGridLength_Infinity = "Value should not be infinity.";
    internal const string DataGridLength_InvalidType = "Invalid type.";
    internal const string DataGrid_InvalidColumnReuse = "The column '{0}' already belongs to another DataGrid.";
    internal const string SelectedCellsCollection_InvalidItem = "The DataGridCellInfo is not valid for this DataGrid.";
    internal const string SelectedCellsCollection_DuplicateItem = "The cell is already selected.";
    internal const string VirtualizedCellInfoCollection_DoesNotSupportIndexChanges = "This collection does not support changes at a specific index.";
    internal const string VirtualizedCellInfoCollection_IsReadOnly = "This collection is read-only.";
    internal const string ChangingTypeNotAllowed = "Changing the Type of a ComponentResourceKey is not allowed once it has been set.";
    internal const string ChangingIdNotAllowed = "Changing the ID of a ComponentResourceKey is not allowed once it has been set.";
    internal const string RangeActionsNotSupported = "Range actions are not supported.";
    internal const string UnexpectedCollectionChangeAction = "Unexpected collection change action '{0}'.";
    internal const string SelectionChangeNotActive = "SelectionChange is not active.";
    internal const string SelectionChangeActive = "SelectionChange is already active.";
    internal const string MultiSelectorSelectAll = "Can only call SelectAll when SelectionMode is Multiple or Extended.";
    internal const string ChangingCollectionNotSupported = "Cannot modify the items collection while a change is in progress.";
    internal const string CannotSelectNotSelectableItem = "The item cannot be selected because it is not selectable.";
    internal const string DataGridRow_CannotSelectRowWhenCells = "Cannot select a row when the DataGrid selection unit is Cell.";
    internal const string DeferSelectionActive = "Deferred selection is already active.";
    internal const string DeferSelectionNotActive = "Deferred selection is not active.";
    internal const string InsertInDeferSelectionActive = "Cannot insert while deferred selection is active.";
    internal const string MoveInDeferSelectionActive = "Cannot move while deferred selection is active.";
    internal const string SetInDeferSelectionActive = "Cannot set while deferred selection is active.";
    internal const string CannotChangeAfterSealed = "The '{0}' object cannot be modified after it is sealed.";

    // AdornerLayer error strings
    internal const string AdornedElementNotFound = "AdornedElementNotFound";
    internal const string AdornerNotFound = "AdornerNotFound";
    internal const string Visual_ArgumentOutOfRange = "Visual_ArgumentOutOfRange";

    internal const string TextContainer_UndoManagerCreatedMoreThanOnce = "TextContainer_UndoManagerCreatedMoreThanOnce";
    internal const string BadDistance = "BadDistance";
    internal const string NotInThisTree = "NotInThisTree";
    internal const string NoElement = "NoElement";
    internal const string TextPointer_CannotInsertTextElementBecauseItBelongsToAnotherTree = "TextPointer_CannotInsertTextElementBecauseItBelongsToAnotherTree";
    internal const string TextSchema_IllegalElement = "TextSchema_IllegalElement";
    internal const string TextSchema_IllegalHyperlinkChild = "TextSchema_IllegalHyperlinkChild: {0}";
    internal const string TextElementCollection_CannotCopyToArrayNotSufficientMemory = "TextElementCollection_CannotCopyToArrayNotSufficientMemory: {0} {1} {2}";
    internal const string TextElementCollection_IndexOutOfRange = "TextElementCollection_IndexOutOfRange";
    internal const string TextElementCollection_ItemHasUnexpectedType = "TextElementCollection_ItemHasUnexpectedType: {0} {1} {2}";
    internal const string TextElementCollection_NextSiblingDoesNotBelongToThisCollection = "TextElementCollection_NextSiblingDoesNotBelongToThisCollection: {0}";
    internal const string TextElementCollection_NoEnumerator = "TextElementCollection_NoEnumerator";
    internal const string TextElementCollection_PreviousSiblingDoesNotBelongToThisCollection = "TextElementCollection_PreviousSiblingDoesNotBelongToThisCollection: {0}";
    internal const string TextElementCollection_TextElementTypeExpected = "TextElementCollection_TextElementTypeExpected: {0}";
    internal const string TextRange_UnsupportedDataFormat = "TextRange_UnsupportedDataFormat: {0}";
    internal const string TextRange_UnrecognizedStructureInDataFormat = "TextRange_UnrecognizedStructureInDataFormat: {0}";
    internal const string TextRangeEdit_InvalidStructuralPropertyApply = "TextRangeEdit_InvalidStructuralPropertyApply: {0} {1}";
    internal const string TextEditorCanNotRegisterCommandHandler = "TextEditorCanNotRegisterCommandHandler: {0} {1}";
    internal const string KeyAltUndoDisplayString = "KeyAltUndoDisplayString";
    internal const string KeyRedoDisplayString = "KeyRedoDisplayString";
    internal const string KeyUndoDisplayString = "KeyUndoDisplayString";
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

    // TextEditorLists display strings
    internal const string KeyRemoveListMarkersDisplayString = "KeyRemoveListMarkersDisplayString";
    internal const string KeyToggleBulletsDisplayString = "KeyToggleBulletsDisplayString";
    internal const string KeyToggleNumberingDisplayString = "KeyToggleNumberingDisplayString";
    internal const string KeyIncreaseIndentationDisplayString = "KeyIncreaseIndentationDisplayString";
    internal const string KeyDecreaseIndentationDisplayString = "KeyDecreaseIndentationDisplayString";

    // TextEditorContextMenu display strings
    internal const string TextBox_ContextMenu_Cut = "Cut";
    internal const string TextBox_ContextMenu_Copy = "Copy";
    internal const string TextBox_ContextMenu_Paste = "Paste";
    internal const string TextBox_ContextMenu_NoSpellingSuggestions = "No spelling suggestions";
    internal const string TextBox_ContextMenu_IgnoreAll = "Ignore All";
    internal const string TextBox_ContextMenu_Description_SBCSSpace = "SBCS Space";
    internal const string TextBox_ContextMenu_Description_DBCSSpace = "DBCS Space";
    internal const string TextBox_ContextMenu_More = "More...";

    // TextEditorTables display strings
    internal const string KeyInsertTableDisplayString = "KeyInsertTableDisplayString";
    internal const string KeyInsertRowsDisplayString = "KeyInsertRowsDisplayString";
    internal const string KeyInsertColumnsDisplayString = "KeyInsertColumnsDisplayString";
    internal const string KeyDeleteRows = "KeyDeleteRows";
    internal const string KeyDeleteRowsDisplayString = "KeyDeleteRowsDisplayString";
    internal const string KeyDeleteColumnsDisplayString = "KeyDeleteColumnsDisplayString";
    internal const string KeyMergeCellsDisplayString = "KeyMergeCellsDisplayString";
    internal const string KeySplitCellDisplayString = "KeySplitCellDisplayString";

    // TextEditorParagraphs display strings
    internal const string KeyAlignLeftDisplayString = "KeyAlignLeftDisplayString";
    internal const string KeyAlignCenterDisplayString = "KeyAlignCenterDisplayString";
    internal const string KeyAlignRightDisplayString = "KeyAlignRightDisplayString";
    internal const string KeyAlignJustifyDisplayString = "KeyAlignJustifyDisplayString";
    internal const string KeyApplySingleSpaceDisplayString = "KeyApplySingleSpaceDisplayString";
    internal const string KeyApplyOneAndAHalfSpaceDisplayString = "KeyApplyOneAndAHalfSpaceDisplayString";
    internal const string KeyApplyDoubleSpaceDisplayString = "KeyApplyDoubleSpaceDisplayString";

    // TextEditorTyping display strings
    internal const string KeyCorrectionList               = nameof(KeyCorrectionList);
    internal const string KeyCorrectionListDisplayString  = nameof(KeyCorrectionListDisplayString);
    internal const string KeyToggleInsertDisplayString    = nameof(KeyToggleInsertDisplayString);
    internal const string KeyDeleteDisplayString          = nameof(KeyDeleteDisplayString);
    internal const string KeyDeleteNextWordDisplayString  = nameof(KeyDeleteNextWordDisplayString);
    internal const string KeyDeletePreviousWordDisplayString = nameof(KeyDeletePreviousWordDisplayString);
    internal const string KeyEnterParagraphBreakDisplayString = nameof(KeyEnterParagraphBreakDisplayString);
    internal const string KeyEnterLineBreakDisplayString  = nameof(KeyEnterLineBreakDisplayString);
    internal const string KeyTabForwardDisplayString      = nameof(KeyTabForwardDisplayString);
    internal const string KeyTabBackwardDisplayString     = nameof(KeyTabBackwardDisplayString);
    internal const string KeySpaceDisplayString           = nameof(KeySpaceDisplayString);
    internal const string KeyShiftSpaceDisplayString      = nameof(KeyShiftSpaceDisplayString);
    internal const string KeyBackspaceDisplayString       = nameof(KeyBackspaceDisplayString);
    internal const string KeyShiftBackspaceDisplayString  = nameof(KeyShiftBackspaceDisplayString);

    // TextEditorSelection display strings
    internal const string KeyMoveLeftByCharacterDisplayString  = nameof(KeyMoveLeftByCharacterDisplayString);
    internal const string KeyMoveRightByCharacterDisplayString = nameof(KeyMoveRightByCharacterDisplayString);
    internal const string KeyMoveLeftByWordDisplayString       = nameof(KeyMoveLeftByWordDisplayString);
    internal const string KeyMoveRightByWordDisplayString      = nameof(KeyMoveRightByWordDisplayString);
    internal const string KeyMoveToLineStartDisplayString      = nameof(KeyMoveToLineStartDisplayString);
    internal const string KeyMoveToLineEndDisplayString        = nameof(KeyMoveToLineEndDisplayString);
    internal const string KeyMoveUpByLineDisplayString         = nameof(KeyMoveUpByLineDisplayString);
    internal const string KeyMoveDownByLineDisplayString       = nameof(KeyMoveDownByLineDisplayString);
    internal const string KeyMoveUpByPageDisplayString         = nameof(KeyMoveUpByPageDisplayString);
    internal const string KeyMoveDownByPageDisplayString       = nameof(KeyMoveDownByPageDisplayString);
    internal const string KeyMoveToDocumentStartDisplayString  = nameof(KeyMoveToDocumentStartDisplayString);
    internal const string KeyMoveToDocumentEndDisplayString    = nameof(KeyMoveToDocumentEndDisplayString);
    internal const string KeyMoveUpByParagraphDisplayString    = nameof(KeyMoveUpByParagraphDisplayString);
    internal const string KeyMoveDownByParagraphDisplayString  = nameof(KeyMoveDownByParagraphDisplayString);
    internal const string KeyMoveToColumnStartDisplayString    = nameof(KeyMoveToColumnStartDisplayString);
    internal const string KeyMoveToColumnEndDisplayString      = nameof(KeyMoveToColumnEndDisplayString);
    internal const string KeyMoveToWindowTopDisplayString      = nameof(KeyMoveToWindowTopDisplayString);
    internal const string KeyMoveToWindowBottomDisplayString   = nameof(KeyMoveToWindowBottomDisplayString);
    internal const string KeySelectLeftByCharacterDisplayString  = nameof(KeySelectLeftByCharacterDisplayString);
    internal const string KeySelectRightByCharacterDisplayString = nameof(KeySelectRightByCharacterDisplayString);
    internal const string KeySelectLeftByWordDisplayString       = nameof(KeySelectLeftByWordDisplayString);
    internal const string KeySelectRightByWordDisplayString      = nameof(KeySelectRightByWordDisplayString);
    internal const string KeySelectToLineStartDisplayString      = nameof(KeySelectToLineStartDisplayString);
    internal const string KeySelectToLineEndDisplayString        = nameof(KeySelectToLineEndDisplayString);
    internal const string KeySelectUpByLineDisplayString         = nameof(KeySelectUpByLineDisplayString);
    internal const string KeySelectDownByLineDisplayString       = nameof(KeySelectDownByLineDisplayString);
    internal const string KeySelectUpByPageDisplayString         = nameof(KeySelectUpByPageDisplayString);
    internal const string KeySelectDownByPageDisplayString       = nameof(KeySelectDownByPageDisplayString);
    internal const string KeySelectToDocumentStartDisplayString  = nameof(KeySelectToDocumentStartDisplayString);
    internal const string KeySelectToDocumentEndDisplayString    = nameof(KeySelectToDocumentEndDisplayString);
    internal const string KeySelectUpByParagraphDisplayString    = nameof(KeySelectUpByParagraphDisplayString);
    internal const string KeySelectDownByParagraphDisplayString  = nameof(KeySelectDownByParagraphDisplayString);
    internal const string KeySelectToColumnStartDisplayString    = nameof(KeySelectToColumnStartDisplayString);
    internal const string KeySelectToColumnEndDisplayString      = nameof(KeySelectToColumnEndDisplayString);
    internal const string KeySelectToWindowTopDisplayString      = nameof(KeySelectToWindowTopDisplayString);
    internal const string KeySelectToWindowBottomDisplayString   = nameof(KeySelectToWindowBottomDisplayString);
    internal const string KeySelectAllDisplayString              = nameof(KeySelectAllDisplayString);

    // TextEditorCharacters display strings
    internal const string KeyResetFormat                  = nameof(KeyResetFormat);
    internal const string KeyResetFormatDisplayString     = nameof(KeyResetFormatDisplayString);
    internal const string KeyToggleBold                   = nameof(KeyToggleBold);
    internal const string KeyToggleBoldDisplayString      = nameof(KeyToggleBoldDisplayString);
    internal const string KeyToggleItalic                 = nameof(KeyToggleItalic);
    internal const string KeyToggleItalicDisplayString    = nameof(KeyToggleItalicDisplayString);
    internal const string KeyToggleUnderline              = nameof(KeyToggleUnderline);
    internal const string KeyToggleUnderlineDisplayString = nameof(KeyToggleUnderlineDisplayString);
    internal const string KeyToggleSubscript              = nameof(KeyToggleSubscript);
    internal const string KeyToggleSubscriptDisplayString = nameof(KeyToggleSubscriptDisplayString);
    internal const string KeyToggleSuperscript            = nameof(KeyToggleSuperscript);
    internal const string KeyToggleSuperscriptDisplayString = nameof(KeyToggleSuperscriptDisplayString);
    internal const string KeyIncreaseFontSize             = nameof(KeyIncreaseFontSize);
    internal const string KeyIncreaseFontSizeDisplayString = nameof(KeyIncreaseFontSizeDisplayString);
    internal const string KeyDecreaseFontSize             = nameof(KeyDecreaseFontSize);
    internal const string KeyDecreaseFontSizeDisplayString = nameof(KeyDecreaseFontSizeDisplayString);
    internal const string KeyApplyFontSize                = nameof(KeyApplyFontSize);
    internal const string KeyApplyFontSizeDisplayString   = nameof(KeyApplyFontSizeDisplayString);
    internal const string KeyApplyFontFamily              = nameof(KeyApplyFontFamily);
    internal const string KeyApplyFontFamilyDisplayString = nameof(KeyApplyFontFamilyDisplayString);
    internal const string KeyApplyForeground              = nameof(KeyApplyForeground);
    internal const string KeyApplyForegroundDisplayString = nameof(KeyApplyForegroundDisplayString);
    internal const string KeyApplyBackground              = nameof(KeyApplyBackground);
    internal const string KeyApplyBackgroundDisplayString = nameof(KeyApplyBackgroundDisplayString);
    internal const string KeyToggleSpellCheck             = nameof(KeyToggleSpellCheck);
    internal const string KeyToggleSpellCheckDisplayString = nameof(KeyToggleSpellCheckDisplayString);

    // TextEditorCopyPaste display strings
    internal const string KeyCutDisplayString         = nameof(KeyCutDisplayString);
    internal const string KeyCopyDisplayString        = nameof(KeyCopyDisplayString);
    internal const string KeyPasteDisplayString       = nameof(KeyPasteDisplayString);
    internal const string KeyCopyFormatDisplayString  = nameof(KeyCopyFormatDisplayString);
    internal const string KeyPasteFormatDisplayString = nameof(KeyPasteFormatDisplayString);
    internal const string KeyCtrlInsertDisplayString  = nameof(KeyCtrlInsertDisplayString);
    internal const string KeyShiftDeleteDisplayString = nameof(KeyShiftDeleteDisplayString);
    internal const string KeyShiftInsertDisplayString = nameof(KeyShiftInsertDisplayString);

    // TextBoxBase strings
    internal const string TextBoxBase_CantSetIsUndoEnabledInsideChangeBlock = nameof(TextBoxBase_CantSetIsUndoEnabledInsideChangeBlock);
    internal const string TextBoxBase_UnmatchedEndChange                    = nameof(TextBoxBase_UnmatchedEndChange);
    internal const string TextBoxScrollViewerMarkedAsTextBoxContentMustHaveNoContent = nameof(TextBoxScrollViewerMarkedAsTextBoxContentMustHaveNoContent);
    internal const string TextBoxDecoratorMarkedAsTextBoxContentMustHaveNoContent    = nameof(TextBoxDecoratorMarkedAsTextBoxContentMustHaveNoContent);
    internal const string TextBoxInvalidTextContainer                       = nameof(TextBoxInvalidTextContainer);
    internal static string Format(string format, params object[] args)
    {
        return string.Format(format, args);
    }

    // DataGrid resource strings (new entries — earlier entries are at lines 52-62)
    internal const string DataGrid_DisplayIndexOutOfRange = "DataGrid_DisplayIndexOutOfRange";
    internal const string DataGrid_CannotSelectCell = "DataGrid_CannotSelectCell";
    internal const string DataGrid_ProbableInvalidSortDescription = "DataGrid_ProbableInvalidSortDescription";
    internal const string ClipboardCopyMode_Disabled = "ClipboardCopyMode_Disabled";
    internal const string DataGrid_HeaderNotVisible = "DataGrid_HeaderNotVisible";
    internal const string DataGrid_ColumnIndexOutOfRange = "DataGrid_ColumnIndexOutOfRange";
    internal const string DataGrid_ColumnDisplayIndexOutOfRange = "DataGrid_ColumnDisplayIndexOutOfRange";
    internal const string DataGrid_DuplicateDisplayIndex = "DataGrid_DuplicateDisplayIndex";
    internal const string DataGrid_InvalidDataGridFrozenColumnCount = "DataGrid_InvalidDataGridFrozenColumnCount";
    internal const string DataGrid_NullColumn = "DataGrid_NullColumn";
    internal const string DataGrid_InvalidCurrentCellSet = "DataGrid_InvalidCurrentCellSet";
    internal const string DataGrid_EditingCellTemplateIsNotSupported = "DataGrid_EditingCellTemplateIsNotSupported";
    internal const string DataGridLength_Auto = "DataGridLength_Auto";
    internal const string DataGridLength_SizeToCells = "DataGridLength_SizeToCells";
    internal const string DataGridLength_SizeToHeader = "DataGridLength_SizeToHeader";
    internal const string DataGrid_ReadOnlyCellsCannotBeEdited = "DataGrid_ReadOnlyCellsCannotBeEdited";
}
