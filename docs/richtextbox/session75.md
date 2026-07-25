### Session 75 - InlineUIContainer Visual Rendering

Status: completed.

Scope:

- Session 67 added `InlineUIContainer`/`BlockUIContainer` support at the
  layout engine level (placeholder `SpanInfo`, single-line block). But
  embedded `UIElement` objects (buttons, images) are **invisible** — the
  layout engine emits a non-breaking space placeholder, and
  `FlowDocumentView.BuildLineVisual` renders it as text.
- This session actually parents the embedded `UIElement` into the
  `FlowDocumentView` visual tree at the correct position.

Implementation:

- In `CollectSpans`, when encountering `InlineUIContainer`, store a
  reference to the child `UIElement` in the `SpanInfo` (or a parallel
  list).
- In `BuildLineVisual`, when processing a run that has an associated
  `UIElement`, add that element as a child of the line `Canvas` at the
  correct X/Y position (from `FlorenceRun.X` and line Y).
- Size the element to its `DesiredSize` or `Width`/`Height`.
- Remove the placeholder text (non-breaking space) from the run so it
  doesn't render as a visible character.
- Handle `BlockUIContainer` similarly: a block-level slot that parents
  the element at the block's Y position.

Tests:

- `InlineUIContainer_Button_AppearsInVisualTree`: create document with
  `InlineUIContainer(Button)`, verify the `FlowDocumentView`'s `Children`
  collection contains a `Button`.
- `BlockUIContainer_Button_AppearsInVisualTree`: same for `BlockUIContainer`.
- `InlineUIContainer_SurvivesEditRoundTrip`: type text around the button,
  verify text is preserved and button still in visual tree.

Files modified:

- `FlorenceEngine.cs` — store UIElement refs in formatting output.
- `FlowDocumentView.uno.cs` — parent embedded elements into visual tree.
- `RichTextBoxIntegrationTests.cs` — visual tree probes + tests.
