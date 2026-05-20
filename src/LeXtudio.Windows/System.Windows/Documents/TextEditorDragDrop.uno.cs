#if HAS_UNO
using System.Windows.Input;

namespace System.Windows.Documents;

internal static partial class TextEditorDragDrop
{
    /// <summary>
    /// Uno-path stub for _DragDropProcess. Source-side drag is handled by TextEditorDragDropUno
    /// wired directly on the renderer (RichTextBlock.CanDrag / DragStarting). Target-side drag
    /// is also handled there. This stub keeps all call sites in TextEditor, TextEditorMouse, and
    /// TextBoxBase unchanged — they call the same methods and get no-ops on the Uno path.
    /// </summary>
    internal sealed class _DragDropProcessUno : IDragDropProcess
    {
        internal _DragDropProcessUno(TextEditor textEditor) { }

        // Source side — Uno drag is platform-initiated via CanDrag/DragStarting on the renderer.
        public bool SourceOnMouseLeftButtonDown(Point mouseDownPoint) => false;
        public void DoMouseLeftButtonUp(MouseButtonEventArgs e) { }
        public bool SourceOnMouseMove(Point mouseMovePoint) => false;

        // Target side — handled by TextEditorDragDropUno at the renderer level.
        public void TargetEnsureDropCaret() { }
        public void TargetOnDragEnter(DragEventArgs e) { }
        public void TargetOnDragOver(DragEventArgs e) { }
        public void TargetOnDrop(DragEventArgs e) { }
        public void DeleteCaret() { }
    }
}
#endif
