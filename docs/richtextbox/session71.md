### Session 71 - Auto-Scroll During Typing

Status: completed.

Scope:

- When text extends beyond the visible area, the RichTextBox should
  auto-scroll to keep the caret visible. Currently the caret moves off-
  screen during rapid typing, Enter at page bottom, or paste of large
  content, with no automatic scrolling.
- WPF's `RichTextBox` scrolls via `ScrollViewer` that is part of the
  `RichTextBox` control template. This shim's `FlowDocumentView` is a
  bare `Panel` embedded in a `ScrollViewer` (`RichTextBox.uno.cs` line
  ~110: `_root` is a `ScrollViewer`).

Implementation:

- In `RichTextBox.uno.cs`, after `UpdateCaretFromSelection()`, call
  `BringIntoView()` on the caret's bounding rect.
- Alternatively, in `FlowDocumentView.SetCaretAt`, call
  `this.StartBringIntoView(rect)` on the `FlowDocumentView` itself.
- Test with long text that overflows the viewport: type past the visible
  area and verify the `ScrollViewer` scrolls to keep the caret visible.

Files modified:

- `FlowDocumentView.uno.cs` — `BringIntoView` in `SetCaretAt`.
- `RichTextBox.IntegrationTests.cs` — scrolling test.

Next session:

- TBD.
