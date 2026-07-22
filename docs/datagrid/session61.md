# DataGrid Port - Session 61

Date: 2026-06-13

## Goal

Continue code reuse beyond columns. The column arc is complete (sessions 58–60);
the next high-value "key place" is the **selection engine**.

## Survey & decision

- The base `DataGridColumn` (1553 lines upstream) carries width-recompute,
  display-index, and header-coordination machinery that would directly fight the
  shim's own width/render path (`ShimColumnWidth`, `OnAutoWidthLayoutUpdated`).
  High regression risk for little behavioral gain — **deferred**.
- `Selector` and `MultiSelector` are **already linked** from upstream, so the full
  WPF selection engine (`MakeFullRowSelection`, `SelectionChange`, `_selectedItems`,
  `SelectedItems`, `SelectionChanged`) is compiled — but the shim never used it,
  maintaining a parallel `_shimSelectedItems` and only ever setting `SelectedItem`.
  The public `SelectedItems` collection and `SelectionChanged` event were **dead**.

A full engine swap (anchors, `_selectedCells`, container-driven visuals, modifier
bridging) is a multi-session subsystem that would risk regressing ~8 passing
selection probes. So this session takes the **safe, verified first increment**.

## What Changed

`DataGrid.cs` (shim partial): added `SyncRealSelectedItems()`, which drives the
linked **MultiSelector batch API** (`BeginUpdateSelectedItems` → mutate
`SelectedItems` → `EndUpdateSelectedItems`) from the shim selection set. The
Selector engine then:
- populates the real `SelectedItems` collection,
- sets `SelectedItem` (replacing the prior manual set),
- raises the real `SelectionChanged` event with correct `AddedItems`/`RemovedItems`.

Wired at every selection mutation: row click (`HandleShimRowClicked`), cell-click
clear (`HandleShimCellClicked`), and the rebuild prune path. A `_syncingRealSelection`
guard prevents reentrancy. The shim's visual application + retention are unchanged
(their migration onto the engine is the next step).

This reuses the Selector/MultiSelector collection + event + change-notification
machinery instead of leaving it dead; `grid.SelectedItems` and
`grid.SelectionChanged` now behave like WPF.

## Verification (confirmed, not assumed)

```
dotnet build  → 0 errors
dotnet run … --probe  → DONE failures=0  (34 steps, +1)
dotnet test  → 126 passed, 0 failed  (+1 RealSelectionEngineIsDriven)
```

New probe step `real SelectedItems + SelectionChanged are driven by the Selector
engine`:
- `SelectedItems=2` containing both rows; `SelectionChanged fired=1, added=1`
  from a known [r0] baseline (the engine correctly fires only on net change —
  which is exactly why the first draft's "fired>=2" assertion was wrong and was
  corrected once the probe logged the real counts).
- plain click collapses to `SelectedItems=1, removed=1`.

All prior selection probes (single, multi-select Ctrl/Shift, survives-sort,
keyboard nav, cell selection) still pass.

## Next Batch

1. Migrate selection **visuals + retention** onto the engine: drive
   `DataGridRow.IsSelected` from `OnSelectionChanged`/container selection and
   retire `_shimSelectedItems` + `ApplyRowSelectionVisuals`.
2. Then route clicks through the real `HandleSelectionForCellInput` /
   `HandleSelectionForRowHeaderAndDetailsInput` → `MakeFullRowSelection`, retiring
   the shim's Ctrl/Shift range logic.
3. `_selectedCells` / `CurrentCell` through the real cell-selection engine.
