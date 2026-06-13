# DataGrid Port - Session 42

Date: 2026-06-12

## Goal

Complete the column-width story: `Star` width distribution across the
available viewport, and `MinWidth`/`MaxWidth` clamping.

## What Changed

- `Clamp(column, width)` applies `MinWidth`/`MaxWidth`; absolute widths are
  clamped at build (`ShimColumnWidth`).
- The post-layout width pass (`OnAutoWidthLayoutUpdated`) now:
  1. computes fixed (absolute) and auto (measured) widths, clamped, and sums
     them;
  2. distributes the remaining width (`ActualWidth - chrome - fixedTotal`)
     among `Star` columns proportionally to their star weight
     (`DataGridLength.Value`), clamped;
  3. applies the resulting width to each column's header + cells.
- Probe: City set to `*` (Star) with `MinWidth=80` expands to fill (357.5 of a
  486px grid) — far wider than the Auto Name column (66.5) and above its
  floor. Test guard extended for `Clamp`. 119 tests; 23 probe steps;
  failures=0.

## Verification

Build succeeded; 119 passed/0 failed; probe `DONE failures=0` —
"Name(auto)=66.5, City(star)=357.5, grid=486".

## Notes / honest limits

- Star uses `DataGrid.ActualWidth - 2` as the budget (a proxy for the rows
  viewport); it does not subtract the vertical scrollbar width or account for
  horizontal overflow precisely.
- `SizeToCells`/`SizeToHeader` are still treated as plain Auto (content), not
  their narrower header-only / cells-only semantics.
- One pass per build; resizing the grid after layout does not re-run star
  distribution until the next rebuild.
- No user column resize/reorder.

## Next Session

1. Re-run the width pass on grid `SizeChanged` so Star reflows on resize.
2. Commit-on-blur for editing; honor `DataGrid.IsReadOnly` / column
   `IsReadOnly` coercion.
3. A non-text column type (e.g. `DataGridCheckBoxColumn`) render + edit.
