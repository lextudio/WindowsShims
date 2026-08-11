### Session 125 - Row-Span Text Vertical Centering

Status: completed.

Scope:

- Session 102 made row-spanned cell boxes extend over the spanned rows, but
  documented that the cell's text stays top-aligned in its first row's band
  (WPF vertically centers it over the spanned height). This session closes
  that approximation.

Findings:

- The flat line model lays out every line on one bottomless page (`Format`)
  and `FormatPages` re-bases lines per page afterwards, so shifting lines
  vertically during `FormatTable` composes safely with pagination (a shifted
  line simply flows to the page its new Y lands on).
- The row-spanned box extension used `box.Y + box.Height` as its base, where
  `box.Height` was the running row height at cell-processing time — a later
  taller cell in the same row would leave the box shorter than the full
  spanned band. Re-based it on the final row band
  (`rowBounds[rowIndex].y + rowBounds[rowIndex].height`).

Changes:

- `FlorenceEngine.cs`:
  - `FlorenceLine` gains a settable `Y`/`Baseline` and a `ShiftY(delta)`
    helper (offsets and width are untouched).
  - `FormatTable` records each row-spanned cell's line range (before/after
    formatting its blocks) in `rowSpannedText`, separately from the visual
    boxes (`rowSpannedBoxes`), so centering applies even when the cell has no
    background/border.
  - After all rows are laid out, each row-spanned cell's lines are shifted
    down by `(spannedHeight - firstRowBand) / 2`.
- `FlowDocumentView.uno.cs`: new `LineYLayout` (comma-joined line top Ys) for
  tests; wired into the DevFlow snapshot.

Tests:

- Integration (extended, 238/238 total):
  - `SaveLoad_Rtf_RoundTripsTableCellRowSpan` now asserts the row-spanning
    cell's line sits halfway down the extra spanned height
    (`alphaY == (boxHeight - gammaY) / 2`), below the same-row cell's line,
    while the next-row cell stays in its band.
- Model tests: 234/234; DataGrid: 62/62 (no regressions).

Result:

- 238/238 RichTextBox integration tests pass; 234/234 model tests pass.
- Row-spanned cell text is now vertically centered over the spanned rows,
  matching WPF.
