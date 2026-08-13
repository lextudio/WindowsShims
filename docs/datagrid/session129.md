# Session 129 — Column resize at the frozen/non-frozen boundary

Date: 2026-08-12. Status: done, DataGrid suite 67/67 green.

## What was wrong

todo.md item 13: resize at the frozen/non-frozen boundary with
`FrozenColumnCount > 0` was "not specifically tested". The existing
`FrozenColumns_BoundaryResizeKeepsFrozenCellTracked` exercised exactly that
scenario, but its assertions only checked the resize was *accepted*
(`resizedFrozen`/`resizedNonFrozen` booleans) and the frozen cell's screen X
was preserved — a silent no-op (width unchanged, still "accepted" per the
shim's contract) would have passed.

## Change

- `datagrid.probe.frozen-edit-readback` now reports `frozenWidthBefore/After`
  and `nonFrozenWidthBefore/After` (`DataGridColumn.Width.Value` before and
  after `ShimTryResizeColumn(+40)`).
- `FrozenColumns_BoundaryResizeKeepsFrozenCellTracked` asserts both columns'
  widths actually grow, in addition to the existing accepted + frozen-X
  assertions.

Result: with `FrozenColumnCount = 1`, resizing the last frozen column and the
first non-frozen column each grows 220 → 260 and the frozen cell's screen X
stays put (1px tolerance) — the boundary-cell arrange/clip math holds.

## Notes

- No source fix needed this session — the gap was test-only.
