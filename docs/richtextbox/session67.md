### Session 67 - InlineUIContainer / BlockUIContainer Embedding

Status: completed.

Scope:

- WPF allows embedding arbitrary `UIElement` objects in flow content via
  `InlineUIContainer` (inline-level) and `BlockUIContainer` (block-level).
  The upstream types are linked and compiled but untested.
- FlorenceLayoutEngine currently skips these during formatting.
  Embedded elements are invisible on screen.

Implementation:

- In `FlorenceLayoutEngine.CollectSpans`, handle `InlineUIContainer` with
  a placeholder `SpanInfo` (non-breaking space or element size).
- In `FlorenceLayoutEngine.FormatBlock`, handle `BlockUIContainer` as a
  single-line block.
- In `FlowDocumentView.BuildLineVisual`, proxy embedded elements into the
  visual tree.
- Tests verify rendering and save/load round-trip.

Files modified:

- `FlorenceEngine.cs`, `FlowDocumentView.uno.cs`, tests.

Next session:

- Performance stress testing.
