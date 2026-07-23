### Session 46 - Double/Triple-Click Word/Paragraph Selection Implemented

Status: complete.

Scope:

- Per session 45's candidate list: found `RichTextBox.uno.cs`'s
  `OnPointerPressed` hardcodes `clickCount = 1` in every call to
  `TextEditorMouse.SetCaretPositionOnMouseEvent`. That method's own logic
  already fully supports `clickCount == 2` (select word, only when the
  selection is currently empty and Shift isn't held) and `clickCount == 3`
  (select paragraph, when `AcceptsRichContent` and Shift isn't held) — this
  is real, linked, upstream WPF behavior that was simply unreachable through
  the actual pointer path.

Implementation:

- Uno/WinUI pointer events have no built-in click-count concept (unlike
  WPF's `MouseButtonEventArgs.ClickCount`), so added
  `RichTextBox.ComputeClickCount(timestamp, point)`: standard desktop
  double/triple-click heuristic — consecutive presses within 500ms and
  within 4px of each other increment a running count; anything else (too
  slow, moved too far) restarts at 1. A 4th rapid click at the same spot
  wraps back to 1 (so a 5th behaves like a double-click again, matching
  common desktop UX rather than getting stuck unable to select anything
  once past 3).
- Uses `PointerPoint.Timestamp` (microseconds, from the actual pointer
  event) rather than wall-clock `DateTime`, matching the platform's own
  event timing.
- `OnPointerPressed` now calls `ComputeClickCount` and passes the real
  result into `SetCaretPositionOnMouseEvent` instead of the hardcoded `1`.

Verification methodology: same pattern as sessions 43-45 — a real pointer
press→move→release sequence can't be synthesized headlessly (no existing
`PointerRoutedEventArgs`/`PointerPoint` construction pattern anywhere in this
repo, and building one is much heavier than the `KeyRoutedEventArgs`/
`CharacterReceivedRoutedEventArgs` reflection already in use), so this splits
verification into the two pieces that together prove the feature works
end-to-end:

- `richtextbox.probe.set-caret-on-mouse-event-at-offset(offset, clickCount)` —
  calls `TextEditorMouse.SetCaretPositionOnMouseEvent` directly (reflection)
  with an explicit `clickCount`, proving the WPF word/paragraph-selection
  logic itself works correctly once reachable.
- `richtextbox.probe.compute-click-count(timestampMicroseconds, x, y)` —
  calls `RichTextBox.ComputeClickCount` directly (reflection), proving the
  new click-counting heuristic itself is correct.
- Together these cover both halves of "does a real double/triple-click
  work" without needing to fake the OS pointer gesture that would normally
  connect them.

Tests added:

- `DoubleClick_SelectsWordUnderCaret` — clicking (clickCount=2) inside the
  word "two" in `"one two three"` selects `"two "` (offsets 4-8, matching
  WPF's `SelectWord` which includes trailing whitespace).
- `TripleClick_SelectsWholeParagraph` — clickCount=3 selects the entire
  paragraph text.
- `ComputeClickCount_DetectsDoubleClickHeuristics` (3 cases) — same spot
  within the time window counts as click 2; moved far away or too slow both
  restart the count at 1.
- `ComputeClickCount_ThreeQuickClicksAtSameSpot_CountsUpToThreeThenWraps` —
  four rapid same-spot clicks produce 1, 2, 3, 1 (the wrap case).

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet test tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet run --project src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop -- --labels=Before
```

Result:

```text
LeXtudio.Windows: 0 errors
RichTextBox.IntegrationTests: 112/112
DataGrid.IntegrationTests: 52/53 (1 pre-existing skip)
LeXtudio.Windows.Tests (NUnitLite): 233/233
```

Note: `System.Windows.Point` (used by `TextEditorMouse.SetCaretPositionOnMouseEvent`'s
signature and `RichTextBox.uno.cs` internally) resolves to `Windows.Foundation.Point`
under `HAS_UNO` — confirmed by reflecting the method's actual parameter type at
runtime. Test host code should construct `Windows.Foundation.Point` directly
(as it already does everywhere else in that file) rather than trying to
resolve `System.Windows.Point` by name, which doesn't exist as a distinct
type in this build.

Next session:

- Remaining candidates from session 45's list: context menu
  (`TextEditorContextMenu.cs` is linked but untested), `AcceptsTab`/Tab-key
  behavior.
- If real `PointerRoutedEventArgs`/`PointerPoint` synthesis is ever built
  (e.g. to test the drag gesture end-to-end), it would also give this
  click-count feature and the session 45 click-collapse fix direct
  end-to-end automated coverage instead of the split-verification approach
  used here.
