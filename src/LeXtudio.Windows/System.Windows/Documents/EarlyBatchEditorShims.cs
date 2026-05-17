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
    }
}

namespace System.Windows.Media
{
    public class GlyphRun
    {
    }
}

namespace System.Windows.Documents
{
    public struct TextSegment
    {
        public TextSegment(ITextPointer start, ITextPointer end)
        {
            Start = start;
            End = end;
        }

        public ITextPointer Start { get; }

        public ITextPointer End { get; }

        public bool IsNull => Start is null || End is null;
    }

    internal class TextEditor
    {
        internal TextEditor()
        {
        }

        internal ITextSelection Selection => null;
    }

    public class TextContainerChangeEventArgs : EventArgs
    {
    }

    internal sealed class ChangeBlockUndoRecord
    {
    }

    public class TextRange
    {
        public TextRange(TextPointer start, TextPointer end)
        {
            Start = start;
            End = end;
        }

        public virtual TextPointer Start { get; }

        public virtual TextPointer End { get; }

        public virtual void ApplyPropertyValue(DependencyProperty formattingProperty, object value)
        {
        }
    }

    public class TextSelection : TextRange
    {
        public TextSelection(TextPointer start, TextPointer end)
            : base(start, end)
        {
        }

        public virtual void Select(TextPointer start, TextPointer end)
        {
        }

        public virtual void ApplySpringloadFormatting()
        {
        }
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
    }

    public sealed class UndoManager
    {
        internal bool IsEnabled => false;
        internal int UndoCount => 0;
        internal int RedoCount => 0;
        internal int MinUndoStackCount => 0;
        internal object OpenedUnit => null;

        internal void Clear()
        {
        }

        internal void Add(object undoUnit)
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

    internal class TextTreeTextElementNode
    {
        internal int IMELeftEdgeCharCount { get; set; }
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
