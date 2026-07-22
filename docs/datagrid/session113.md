# Session 113

Date: 2026-06-24

## Goal

Close the remaining gap from session 112: filter row cell widths were only
synchronized once (at build time via `OnAutoWidthLayoutUpdated`). After a
column gripper drag, the filter cell for that column kept its stale width.

## Changes

### `ext/wpf/.../DataGrid.cs` (upstream, `#else HAS_UNO` branch)

Added a `WidthProperty` branch to the existing column-property-change handler:

```csharp
else if (e.Property == DataGridColumn.WidthProperty && d is DataGridColumn resizedColumn)
    ShimApplyColumnWidth(resizedColumn);
```

This fires via the existing `NotifyPropertyChanged` → `InternalColumns` →
`DataGridColumnCollection` pipeline whenever a column's `Width` DP changes,
which is the path taken by the gripper drag (`RecomputeColumnWidthsOnColumnResize`
→ `column.Width = newWidth`).

### `src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs`

Added `ShimApplyColumnWidth(DataGridColumn column)`:

- Looks up the column's index in `_visibleColumns`.
- Computes `ShimColumnWidth(column)` and clamps it.
- Updates the header cell (`_headerCells[i].Width`), all realized data cells for
  that column, and the filter cell (`_filterCells[i].Width`).
- Skips if the column is not visible or the computed width is ≤ 0 / NaN.

This is a targeted update — no full `BuildShimVisualTree()` rebuild is needed.

## Verification

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 140 passed
- 0 failed
- 0 skipped

## Notes

- The `CoerceValue(ActualWidthProperty)` call in the WPF column width chain is a
  no-op in the shim, so `column.ActualWidth` is never updated by a resize — only
  `column.Width` (the `DataGridLength`) changes. `ShimColumnWidth` already reads
  `Width.Value` for absolute columns, so no extra plumbing is needed.
- Star-column resize support: `ShimColumnWidth` returns `column.ActualWidth` for
  star columns; since `ActualWidth` stays 0 in the shim, `ShimApplyColumnWidth`
  will skip (width ≤ 0). Star columns are already handled by `OnAutoWidthLayoutUpdated`
  which runs on every layout pass; they remain unchanged.
- There are no new known gaps from session 112 remaining.
