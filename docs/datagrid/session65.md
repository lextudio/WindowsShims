# DataGrid Port - Session 65

Date: 2026-06-13

## Goal

Execute the pending `DataGridColumnCollection` reuse: replace the hand-written
local shim (581 lines reimplementing the WPF display-index model) with the
**linked upstream `DataGridColumnCollection.cs`**, keeping only the
width/virtualization regions out of scope (owned by the shim width pass).

## What Changed

- **Linked** `ext/.../DataGridColumnCollection.cs` and fork-guarded it:
  - class declaration → `internal partial class` under `#if HAS_UNO`;
  - the contiguous width/realization span (Star Column Helper → Column
    Virtualization, ~1700 lines) wrapped in a single `#if !HAS_UNO`;
  - the `RealizedColumnsBlock` fields in the Data region guarded out.
- **Deleted** the 581-line local `DataGridColumnCollection.cs`. The display-index
  model, frozen-column handling, notification propagation, and hidden-column
  helpers now come from real WPF.
- **Added** `DataGridColumnCollection.uno.cs` (~80 lines): light stubs for the
  members the guarded regions defined and that linked `DataGrid`/`DataGridColumn`
  code calls — `Invalidate*`, `Redistribute*`, `RecomputeColumnWidthsOnColumnResize`,
  `HasVisibleStarColumns` (computed live), `ColumnWidthsComputationPending`,
  `OnCellsPanelHorizontalOffsetChanged`, and the realized-column block-list
  properties (typed `object?` to avoid pulling in the virtualization type).
- **Bridged the Uno DP gap** with `RefreshDisplayIndexMap()`: WPF maintains the
  map incrementally via the `DisplayIndexProperty` changed-callback, which the Uno
  DP shim does not fire for direct `column.DisplayIndex = n` sets. The render path
  (`BuildShimVisualTree`, `ColumnsInDisplayOrder`) calls this to rebuild the map
  from current column values.

## Two regressions found and fixed (verify-driven)

1. **`Debug.Assert(DisplayIndexMap.Count == 0)`** tripped on the second refresh
   per render — the upstream init asserts an empty map. Fixed by clearing before
   re-init.
2. **Empty map after refresh** — the upstream `DisplayIndexMap` *property getter*
   lazily calls `InitializeDisplayIndexMap()` when the flag is false, so
   `DisplayIndexMap.Clear()` rebuilt-then-cleared the map and set the flag,
   making the explicit init early-return. Fixed by clearing the backing
   `_displayIndexMap` field directly. (Found by instrumenting `mapCount`/`flag`,
   not by guessing.)

## Test update

`ColumnCollectionRejectsNullOwner` asserted a shim-specific null-throw; the reused
upstream ctor uses `Debug.Assert` instead. Replaced with
`ColumnCollectionBodyIsReusedFromUpstream`, asserting the non-public `ctor(DataGrid)`,
`ColumnFromDisplayIndex`, and the `RefreshDisplayIndexMap` bridge are present.

## Verification

```
dotnet build … --no-restore   → 0 errors
dotnet test  … --no-restore   → 126 passed, 0 failed
dotnet run   … -- --probe     → DONE failures=0
```

The display-index probe (move Age to display index 0; verify
`ColumnFromDisplayIndex(0)`, first header, first cell) passes against the linked
upstream model.

## Still Deferred

The width-computation / column-resize / star-distribution / column-virtualization
regions remain `#if !HAS_UNO`. They depend on the virtualizing cells panel and a
measure/realization pipeline the shim does not have; the shim width pass
(`OnAutoWidthLayoutUpdated`) still owns runtime width behavior. Revisit when that
pass is ready to be replaced by upstream measure/realization.

## Next Batch

1. Row/cell container cleanup (`DataGridCell` edit/current-cell surfaces,
   `DataGridRow` container state) per session 64's recommendation.
2. The deferred column-width/realization regions, once the shim width pass is
   ready to hand off to upstream measure behavior.
