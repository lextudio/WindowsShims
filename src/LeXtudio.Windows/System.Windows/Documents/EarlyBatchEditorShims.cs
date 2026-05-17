#if WINUI_BRIDGE
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace System.Windows
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class AttachedPropertyBrowsableForTypeAttribute : Attribute
    {
        public AttachedPropertyBrowsableForTypeAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public Type TargetType { get; }
    }

    public abstract class ContentPosition
    {
    }

    public sealed class LocalValueEnumerator
    {
        public bool MoveNext() => false;

        public LocalValueEntry Current => default;
    }

    public readonly struct LocalValueEntry
    {
        public DependencyProperty Property { get; init; }

        public object? Value { get; init; }
    }

    public class QueryContinueDragEventArgs : RoutedEventArgs
    {
    }

    public class GiveFeedbackEventArgs : RoutedEventArgs
    {
    }
}

namespace System.Windows.Input
{
    public class RoutedUICommand : RoutedCommand
    {
        public string Text { get; }

        public RoutedUICommand(string text, string name, Type ownerType)
            : base(name, ownerType)
        {
            Text = text ?? string.Empty;
        }

        public RoutedUICommand(string text, string name, Type ownerType, InputGestureCollection inputGestures)
            : base(name, ownerType, inputGestures)
        {
            Text = text ?? string.Empty;
        }
    }

    public class KeyboardFocusChangedEventArgs : RoutedEventArgs
    {
    }

    public class Cursor
    {
    }
}

namespace System.Windows.Threading
{
    public struct DispatcherProcessingDisabled : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

namespace MS.Win32
{
    internal static class NativeMethods
    {
        internal const int LOCALE_FONTSIGNATURE = 0x0058;
    }

    internal static class SafeNativeMethods
    {
        internal const ushort C1_SPACE = 0x0008;
        internal const ushort C1_BLANK = 0x0040;
        internal const uint   CT_CTYPE1 = 0x00000001;
        internal const uint   CT_CTYPE3 = 0x00000004;
        internal const ushort C1_PUNCT      = 0x0010;
        internal const ushort C3_KATAKANA   = 0x0020;
        internal const ushort C3_HIRAGANA   = 0x0040;
        internal const ushort C3_IDEOGRAPH  = 0x0100;
        internal const ushort C3_HALFWIDTH  = 0x0400;
        internal const ushort C3_FULLWIDTH  = 0x0800;
        internal const ushort C3_DIACRITIC  = 0x0002;
        internal const ushort C3_NONSPACING = 0x0001;
        internal const ushort C3_VOWELMARK  = 0x0004;
        internal const ushort C3_KASHIDA    = 0x0040;

        internal static int GetKeyboardLayoutList(int nBuff, IntPtr[]? lpList) => 0;

        internal static bool GetStringTypeEx(uint locale, uint dwInfoType, ReadOnlySpan<char> lpSrcStr, Span<ushort> lpCharType) => false;
    }

    internal static class UnsafeNativeMethods
    {
        internal static int GetLocaleInfoW(int locale, int lcType, string lpLCData, int cchData) => 0;

        internal static int FindNLSString(int locale, uint dwFindNLSStringFlags, ReadOnlySpan<char> lpStringSource, ReadOnlySpan<char> lpStringValue, out int foundLength)
        {
            foundLength = 0;
            return -1;
        }
    }
}

namespace System.Windows.Media
{
    public class GlyphRun
    {
    }
}

namespace System.Windows
{
    // Stub for WPF binding expression type used in undo guards
    internal abstract class Expression
    {
    }
}

namespace System.Windows.Documents
{
    internal static class TextTreeUndo
    {
        internal static UndoManager? GetOrClearUndoManager(ITextContainer container) => null;
    }

    internal class TextEditor
    {
        internal static readonly TextEditorThreadLocalStore _ThreadLocalStore = new();

        internal TextEditor()
        {
        }

        internal ITextSelection Selection => null;
        internal TextContainer TextContainer { get; } = new();
        internal FrameworkElement? UiScope { get; set; }
        internal bool AcceptsRichContent { get; set; } = true;
        internal bool IsContextMenuOpen { get; set; }
        internal bool AutoWordSelection { get; set; }
        internal bool IsReadOnly { get; set; }
        internal bool IsReadOnlyCaretVisible { get; set; }
        internal ITextView? TextView { get; set; }
        internal TextStore? TextStore { get; set; }
        internal ImmComposition? ImmComposition { get; set; }
    }

    internal sealed partial class FormattingDependencyObject : DependencyObject
    {
    }

    internal enum CaretScrollMethod
    {
        Unset,
        None,
        Simple,
        Navigation,
    }

    internal sealed class CaretElement
    {
        internal const double c_endOfParaMagicMultiplier = 1.0;

        internal CaretElement(TextEditor textEditor, bool isBlinkEnabled)
        {
        }

        internal bool IsSelectionActive { get; set; }

        internal Geometry SelectionGeometry { get; set; }

        internal static FrameworkElement GetOwnerElement(DependencyObject uiScope)
        {
            return uiScope as FrameworkElement;
        }

        internal void SetBlinking(bool isBlinkEnabled) { }
        internal void OnTextViewUpdated() { }
        internal void Hide() { }
        internal void RefreshCaret(bool italic) { }

        internal void Update(bool visible, Rect caretRect, Brush caretBrush, double opacity, bool italic, CaretScrollMethod scrollMethod, double scrollToOriginPosition)
        {
        }

        internal void UpdateSelection() { }
        internal void DetachFromView() { }
    }

    internal sealed class TextStore
    {
        internal TextStore(TextEditor textEditor)
        {
        }

        internal bool IsComposing => false;
        internal bool IsInterimSelection => false;

        internal void OnAttach() { }
        internal void OnDetach(bool finalizer) { }
        internal void OnLayoutUpdated() { }
        internal void OnGotFocus() { }
        internal void OnLostFocus() { }
        internal void OnSelectionChange() { }
        internal void OnSelectionChanged() { }
        internal void CompleteComposition() { }
        internal void CompleteCompositionAsync() { }

        internal bool QueryRangeOrReconvertSelection(bool fDoReconvert)
        {
            return false;
        }

        internal void UpdateCompositionText(object composition) { }

        internal object GetReconversionCandidateList()
        {
            return null;
        }
    }

    internal sealed class ImmComposition
    {
        internal static ImmComposition GetImmComposition(DependencyObject uiScope)
        {
            return null;
        }

        internal bool IsComposition => false;

        internal void OnGotFocus(TextEditor editor) { }
        internal void OnLostFocus() { }
        internal void OnDetach(TextEditor editor) { }
        internal void OnLayoutUpdated() { }
        internal void OnSelectionChange() { }
        internal void OnSelectionChanged() { }
        internal void UpdateCompositionText(object composition) { }
        internal void CompleteComposition() { }
    }

    internal enum SpellingReform
    {
        PreAndPostreform,
    }

    internal sealed class SpellingError
    {
    }

    internal sealed class Speller
    {
        internal Speller(TextEditor editor)
        {
        }

        internal void Detach() { }
        internal void SetCustomDictionaries(object dictionarySources, bool add) { }
        internal void SetSpellingReform(SpellingReform spellingReform) { }
    }

    internal abstract class ShutDownListener
    {
        protected ShutDownListener(object target, ShutDownEvents events)
        {
        }

        internal abstract void OnShutDown(object target, object sender, EventArgs e);
    }

    [Flags]
    internal enum ShutDownEvents
    {
        DomainUnload = 1,
        DispatcherShutdown = 2,
    }

    internal sealed class TextEditorThreadLocalStore
    {
        internal object? Bidi { get; set; }

        internal ITextSelection? FocusedTextSelection { get; set; }
    }

    public sealed class UndoManager
    {
        internal bool IsEnabled => false;
        internal int UndoCount => 0;
        internal int RedoCount => 0;
        internal int MinUndoStackCount => 0;
        internal MS.Internal.Documents.IParentUndoUnit OpenedUnit => null;

        internal void Clear()
        {
        }

        internal void Add(object undoUnit)
        {
        }

        internal void Open(MS.Internal.Documents.IParentUndoUnit parentUndoUnit)
        {
        }

        internal void Close(MS.Internal.Documents.IParentUndoUnit parentUndoUnit, MS.Internal.Documents.UndoCloseAction closeAction)
        {
        }
    }

    internal enum UndoState
    {
        Normal,
        Undo,
        Redo,
    }

    internal static class TextEditorMouse
    {
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners) { }
        internal static void OnMouseDown(object scope, MouseButtonEventArgs e) { }
        internal static void OnMouseMove(object scope, MouseEventArgs e) { }
        internal static void OnMouseUp(object scope, MouseButtonEventArgs e) { }
        internal static void OnQueryCursor(object scope, QueryCursorEventArgs e) { }
    }

    internal static class TextEditorTyping
    {
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners) { }
        internal static void OnPreviewKeyDown(object scope, KeyEventArgs e) { }
        internal static void OnKeyDown(object scope, KeyEventArgs e) { }
        internal static void OnKeyUp(object scope, KeyEventArgs e) { }
        internal static void OnTextInput(object scope, TextCompositionEventArgs e) { }
    }

    internal static class TextEditorCopyPaste
    {
        internal static void _RegisterClassHandlers(Type controlType, bool acceptsRichContent, bool readOnly, bool registerEventListeners) { }
    }

    internal static class TextEditorContextMenu
    {
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners) { }
        internal static void OnContextMenuOpening(object scope, ContextMenuEventArgs e) { }
    }

    internal static class TextEditorSpelling
    {
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners) { }
        internal static SpellingError GetSpellingErrorAtPosition(TextEditor editor, ITextPointer position, LogicalDirection direction) => null;
        internal static SpellingError GetSpellingErrorAtSelection(TextEditor editor) => null;
        internal static ITextPointer GetNextSpellingErrorPosition(TextEditor editor, ITextPointer position, LogicalDirection direction) => null;
    }

    internal sealed class TextEditorDragDrop
    {
        internal sealed class _DragDropProcess
        {
        }

        internal static void _RegisterClassHandlers(Type controlType, bool readOnly, bool registerEventListeners) { }
        internal static void OnQueryContinueDrag(object scope, QueryContinueDragEventArgs e) { }
        internal static void OnGiveFeedback(object scope, GiveFeedbackEventArgs e) { }
        internal static void OnDragEnter(object scope, DragEventArgs e) { }
        internal static void OnDragOver(object scope, DragEventArgs e) { }
        internal static void OnDragLeave(object scope, DragEventArgs e) { }
        internal static void OnDrop(object scope, DragEventArgs e) { }
    }

    internal static class TextEditorTables
    {
        internal static void _RegisterClassHandlers(Type controlType, bool registerEventListeners) { }
    }

    internal sealed class TextRangeEditTables
    {
        internal static TextPointer EnsureInsertionPosition(ITextPointer position)
        {
            return position as TextPointer;
        }

        internal static bool GetColumnRange(ITextRange range, Table table, out int firstColumnIndex, out int lastColumnIndex)
        {
            firstColumnIndex = 0;
            lastColumnIndex = 0;
            return false;
        }

        internal static Table GetTableFromPosition(TextPointer position)
        {
            return null;
        }

        internal static TableCell GetTableCellFromPosition(TextPointer position)
        {
            return null;
        }

        internal static bool IsTableStructureCrossed(ITextPointer anchorPosition, ITextPointer movingPosition)
        {
            return false;
        }

        internal static bool IsTableCellRange(TextPointer anchorPosition, TextPointer movingPosition, bool includeCellAtMovingPosition, out TableCell anchorCell, out TableCell movingCell)
        {
            anchorCell = null;
            movingCell = null;
            return false;
        }

        internal static List<TextSegment> BuildTableRange(TextPointer start, TextPointer end)
        {
            return [new TextSegment(start, end)];
        }

        internal static void IdentifyValidBoundaries(ITextRange range, out ITextPointer start, out ITextPointer end)
        {
            start = range.Start;
            end = range.End;
        }

        internal static TextPointer GetNextTableCellRangeInsertionPosition(TextSelection selection, LogicalDirection direction)
        {
            return ((ITextSelection)selection).MovingPosition as TextPointer;
        }

        internal static TextPointer GetNextRowEndMovingPosition(TextSelection selection, LogicalDirection direction)
        {
            return ((ITextSelection)selection).MovingPosition as TextPointer;
        }

        internal static bool MovingPositionCrossesCellBoundary(TextSelection selection)
        {
            return false;
        }

        internal static TextPointer GetNextRowStartMovingPosition(TextSelection selection, LogicalDirection direction)
        {
            return ((ITextSelection)selection).MovingPosition as TextPointer;
        }

        internal static Table InsertTable(TextPointer insertionPosition, int rowCount, int columnCount)
        {
            var table = new Table();
            var rowGroup = table.RowGroups[0];
            rowCount = Math.Max(1, rowCount);
            columnCount = Math.Max(1, columnCount);

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var row = new TableRow
                {
                    RowGroup = rowGroup,
                    Index = rowIndex,
                };

                for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    var cell = new TableCell
                    {
                        Row = row,
                        ColumnIndex = columnIndex,
                    };
                    row.Cells.Add(cell);
                }

                rowGroup.Rows.Add(row);
            }

            return table;
        }

        internal static TextPointer GetAdjustedRowEndPosition(Table currentTable, TextPointer rowEndPosition)
        {
            return rowEndPosition;
        }

        internal static void DeleteContent(TextPointer start, TextPointer end)
        {
        }

        internal static TextRange InsertRows(TextRange textRange, int rowCount)
        {
            return textRange;
        }

        internal static bool DeleteRows(TextRange textRange)
        {
            return false;
        }

        internal static TextRange InsertColumns(TextRange textRange, int columnCount)
        {
            return textRange;
        }

        internal static bool DeleteColumns(TextRange textRange)
        {
            return false;
        }

        internal static TextRange MergeCells(TextRange textRange)
        {
            return textRange;
        }

        internal static TextRange SplitCell(TextRange textRange, int splitCountHorizontal, int splitCountVertical)
        {
            return textRange;
        }

        internal static bool TableBorderHitTest(ITextView textView, Point pt)
        {
            return false;
        }

        internal static TableColumnResizeInfo StartColumnResize(ITextView textView, Point pt)
        {
            return null;
        }

        internal static void EnsureTableColumnsAreFixedSize(Table table, double[] columnWidths)
        {
        }

        internal sealed class TableColumnResizeInfo
        {
        }
    }

    internal sealed class TextTreeDeleteContentUndoUnit
    {
        internal TextTreeDeleteContentUndoUnit(params object[] args)
        {
        }
    }

    internal sealed class TextTreeExtractElementUndoUnit
    {
        internal TextTreeExtractElementUndoUnit(params object[] args)
        {
        }
    }

    internal class TextTreeTextElementNode : SplayTreeNode
    {
        internal int IMELeftEdgeCharCount { get; set; }

        internal override SplayTreeNode ParentNode { get; set; }
        internal override SplayTreeNode ContainedNode { get; set; }
        internal override SplayTreeNode LeftChildNode { get; set; }
        internal override SplayTreeNode RightChildNode { get; set; }
        internal override int SymbolCount { get; set; }
        internal override int IMECharCount { get; set; }
        internal override int LeftSymbolCount { get; set; }
        internal override int LeftCharCount { get; set; }
        internal override uint Generation { get; set; }
        internal override int SymbolOffsetCache { get; set; }
    }
}

namespace MS.Internal.Documents
{
    internal class ParentUndoUnit : IParentUndoUnit
    {
        internal ParentUndoUnit(string description)
        {
            Description = description;
        }

        public IUndoUnit LastUnit => null;

        public IParentUndoUnit OpenedUnit => null;

        public string Description { get; set; }

        public bool Locked => false;

        public object Container { get; set; }

        public virtual void Do()
        {
        }

        public virtual bool Merge(IUndoUnit unit)
        {
            return false;
        }

        public void Clear()
        {
        }

        public void Open(IParentUndoUnit newUnit)
        {
        }

        public void Close(UndoCloseAction closeAction)
        {
        }

        public void Close(IParentUndoUnit closingUnit, UndoCloseAction closeAction)
        {
        }

        public void Add(IUndoUnit newUnit)
        {
        }

        public void OnNextAdd()
        {
        }

        public void OnNextDiscard()
        {
        }

        protected virtual IParentUndoUnit CreateParentUndoUnitForSelf()
        {
            return null;
        }
    }
}
#endif
