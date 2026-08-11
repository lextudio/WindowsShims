### Session 101 - Side-by-Side Table Cell Layout (True Grid)

Status: completed.

Scope:

- Session 99 rendered table columns/cells but cells flowed vertically (each
  cell below the previous). This session lays out cells in a row side by
  side, sharing one vertical band, and makes hit-testing pick the correct
  cell when multiple lines share a band.

Findings:

- The Florence line model is flat (one line per text row, ordered by offset),
  which is why the stacked layout was chosen originally. A true grid is
  possible because the offset-based mapping (caret, GetRect, IME, spell
  check) only requires that line offset ranges are disjoint and
  monotonically ordered — which holds when each cell's lines are emitted at
  the same row band but with per-cell X offsets.
- The only Y-based lookup was `ITextView.GetTextPositionFromPoint`, which
  picked the first line in the point's Y band. With side-by-side cells that
  returns the wrong cell; the fix prefers the line whose run X-range contains
  the point.
- Table cell offsets must reserve one slot per cell paragraph (the invisible
  paragraph boundary position), matching the `TextPointer` offset space used
  by caret navigation — otherwise caret/hit-test mapping drifts inside
  tables. (An earlier container-offset experiment broke caret round-trips and
  was reverted; the session-99 accumulation model is preserved.)

Changes:

- `FlorenceEngine.cs` (`FormatTable`): cells format with a local `y` starting
  at the row's top; the row height is the tallest cell and the next row
  starts after it; cell boxes span the full row band. One offset slot is
  reserved per cell paragraph (and one for empty cells).
- `UnoFlowDocumentTextView.cs` (`GetTextPositionFromPoint`): among the lines
  in the point's Y band, prefer the one whose run X-range contains the
  point; fall back to the first line in the band.
- `MainPage.cs`: new `hit-test-first-inline` probe (finds the first Run
  containing text anywhere in the document, hit-tests its first character's
  rect, and reports the hit position's rect X).

Tests:

- Integration (1 new, 233/233 total):
  - `SaveLoad_Rtf_RoundTripsTableCellHitTest` — after the RTF round-trip,
    clicking the first character of the second cell's text round-trips to a
    position whose rect is in the same column (|ΔX| < 5), not the left cell.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 233/233 RichTextBox integration tests pass; 234/234 model tests pass.
- Table cells now render side by side in shared row bands with correct
  hit-testing; the previous "cells flow vertically" limitation is resolved.
  `ColumnSpan`/`RowSpan` are still not honored by the layout.
