### Session 69 - Caret Visual Polish

Status: in progress.

Scope:

- The caret is a 1px black `Rectangle` in `FlowDocumentView`. WPF's caret:
  - Blinks at the system blink rate (already done via `DispatcherTimer`).
  - Uses `CaretBrush` for color/thickness.
  - Hides during active selection.
  - Adjusts thickness in `Overwrite` mode.
  - Respects `IsReadOnly`/`IsEnabled` states.
- This session fixes the caret to match WPF behavior more closely.

Implementation:

- Add `CaretBrush` support: read from `TextBoxBase.CaretBrush` or default to black.
- Hide caret when `Selection.IsEmpty == false` (selection active).
- Adjust caret width/thickness in overwrite mode if available.
- Grey out caret when `IsReadOnly` or `IsEnabled == false`.
- Add `caret-visual-state` probe to report caret properties.

Files modified:

- `FlowDocumentView.uno.cs` — caret visual properties.
- `MainPage.cs` — caret visual probe.
- `RichTextBoxIntegrationTests.cs` — new tests.

Next session:

- Drag/drop visual feedback (drop caret rendering).
