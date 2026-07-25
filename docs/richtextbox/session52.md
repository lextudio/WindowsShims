### Session 52 - Drag/Drop End-to-End via Pointer Synthesis

Status: planned.

Scope:

- Session 44 wired `IRichTextDragDropHost` into `RichTextBox` and added
  `TextEditorDragDropUno` (the Uno-native drag-drop handler), plus 5 unit-level
  probes that test the host interface methods directly
  (`GetSelectionRange`, `GetTextRange`, `InsertTextAt`, `HitTest`). These
  verify the contract but **do not exercise the pointer-driven drag flow**
  (`DragStarting`, `DragOver`, `Drop` event handlers in
  `TextEditorDragDropUno`).

- The remaining gap is end-to-end pointer synthesis: simulate a true
  pointer-press → drag → drop sequence that drives the actual Uno
  `DragStarting`/`DragOver`/`Drop` event pipeline on the `RichTextBox` element.
  This validates that `OnPointerPressed` enables `CanDrag`, that
  `OnDragStarting` populates the `DataPackage`, and that `OnDrop` inserts text
  at the correct offset.

Implementation:

- Add probe(s) to `MainPage.cs` that synthesize pointer events on the
  `RichTextBox` element. Uno's `UIElement` supports `PointerPressed`,
  `PointerMoved`, `PointerReleased` via `InjectPointerInput` (if available) or
  a simpler approach: directly raise `DragStarting`/`DragOver`/`Drop` routed
  events on the target to exercise `TextEditorDragDropUno`'s handlers without
  requiring OS-level drag participation.

- The direct-event approach is preferred for CI reliability (OS-level drag
  requires real pointer hardware and mouse-move injection, which is fragile in
  headless or remote-test scenarios). `TextEditorDragDropUno` subscribes to
  `DragStarting`, `DragEnter`, `DragOver`, `DragLeave`, and `Drop` — raising
  these events programmatically on the `RichTextBox` UIElement exercises the
  same code paths the OS would invoke.

- Probe flow:
  1. `create-plain("hello world")` + `select-run-range(0, 5)` to select "hello".
  2. Raise `DragStarting` on the RichTextBox → verify `DataPackage` contains
     "hello".
  3. Raise `DragOver` at a character offset past the selection — verify drop
     caret moves.
  4. Raise `Drop` at the target offset — verify "hello" is inserted at that
     position and the document text reads correctly.

Tests:

- `DragDrop_EndToEnd_DragWithinDocument_InsertsAtDropTarget`: create-plain
  "hello world", select "hello", raise DragStarting → DataPackage has "hello",
  raise DragOver past "world" → drop caret shown, raise Drop → document reads
  "hello worldhello".

- `DragDrop_EndToEnd_EmptySelection_DoesNotStartDrag`: no selection, raise
  DragStarting → Cancel=true, no data in package.

- `DragDrop_EndToEnd_ReadOnly_DoesNotAcceptDrop`: set IsReadOnly=true, raise
  DragOver → AcceptedOperation=null.

- `DragDrop_EndToEnd_DropCaret_ShownOnDragOver_HiddenOnDragLeave`:
  raise DragOver at offset → SetDropCaretOffset called, raise DragLeave →
  SetDropCaretOffset(-1).

Files modified:

- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` — pointer synthesis
  probes.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Next session:

- List creation (`List.Apply`).
