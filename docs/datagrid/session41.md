# DataGrid Port - Session 41

Date: 2026-06-12

## Goal

`Auto` column width — size non-absolute columns to their content (deferred
from sessions 29/35/39 due to the async cell-realization timing).

## What Changed

- `ShimColumnWidth` now returns `NaN` (auto-size) for non-absolute columns
  instead of the flat 120 fallback; absolute widths are still honored.
- `BuildShimVisualTree` records the visible columns (`_visibleColumns`) and
  header cells (`_headerCells`); if any visible column is non-absolute it
  arms `_autoWidthPending` and subscribes `LayoutUpdated` once.
- `OnAutoWidthLayoutUpdated` (one-shot per build): for each Auto column it
  takes the max realized `DesiredSize.Width` across the header + all row cells
  and applies it uniformly to the header and every cell in that column, so the
  column sizes to content and header/cells stay aligned. Setting the width
  flips `_autoWidthPending` off, so the re-layout it triggers is a no-op.
- Probe: a `Width=60` Age column stays fixed; the Auto Name column reports a
  finite, content-based width with header and cell aligned (49 == 49). Test
  `AutoWidthSurfaceExists`. 119 tests; 22 probe steps; failures=0.

## Verification

Build succeeded; 119 passed/0 failed; probe `DONE failures=0` —
"Name header width=49, cell width=49".

## Notes / honest limits

- `Star` and `SizeToCells`/`SizeToHeader` are treated as plain Auto
  (content-sized); true star distribution across available width and the
  size-to-* semantics are not implemented.
- The Auto pass uses `DesiredSize.Width` (includes cell margins), so columns
  are a few px wider than strict content; it is stable and aligned.
- One measure pass per build; no continuous re-fit as content changes without
  a rebuild, and no min/max width clamping (`MinWidth`/`MaxWidth` ignored).
- All rows are realized (no virtualization), so the "widest cell" is the true
  max; with virtualization this would need revisiting.

## Next Session

1. `Star` width distribution across remaining viewport width.
2. Honor `MinWidth`/`MaxWidth` in the Auto/explicit width paths.
3. Commit-on-blur for editing; honor `DataGrid.IsReadOnly`.
