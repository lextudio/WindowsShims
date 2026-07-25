### Session 70 - Drag/Drop Visual Feedback (Drop Caret)

Status: completed.

Scope:

- When dragging selected text, WPF shows a blinking caret at the drop
  position. The current drag/drop implementation (sessions 44, 52) raises
  `DragOver`/`DragDrop` events but has no visual feedback.
- `TextEditorDragDropUno.OnDragOver` calls `_host.SetDropCaretOffset`
  on `IRichTextDragDropHost`, which stores the offset but renders nothing.
- Add a drop caret `Rectangle` in `FlowDocumentView` positioned at the
  current drop target offset.

Implementation:

- Add `_dropCaret` `Rectangle` field in `FlowDocumentView`.
- Implement `IRichTextDragDropHost.SetDropCaretOffset(int offset)` to
  position the drop caret rectangle at the character rect for that offset.
- Add `ClearDropCaret()` to hide it when drag leaves or completes.
- Wire `DragLeave` and `Drop` events to clear the drop caret.

Files modified:

- `FlowDocumentView.uno.cs` — drop caret visual.
- `RichTextBox.DragDrop.uno.cs` — wire `SetDropCaretOffset`/`ClearDropCaret`.

Next session:

- Auto-scroll during typing.
