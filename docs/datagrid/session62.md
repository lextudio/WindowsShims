# DataGrid Port - Session 62

Date: 2026-06-13

## Goal

Continue the selection-engine reuse arc (session 61 step 1). Migrate row
selection **visuals** onto the real engine: drive `DataGridRow.IsSelected` from
the linked Selector's `SelectionChanged` event instead of the shim's manual
`ApplyRowSelectionVisuals` pass.

## What Changed

`DataGrid.cs` (shim partial):
- Subscribed once to the real `SelectionChanged` event (in
  `HookShimChangeNotifications`). The new `OnShimSelectionChanged` handler reflects
  `e.AddedItems`/`e.RemovedItems` onto the realized row containers
  (`ItemContainerGenerator.ContainerFromItem` → `DataGridRow.IsSelected`). This is
  now the single source of truth for live row highlight.
- Rebuilds (`BuildShimVisualTree`) re-apply the highlight from the real
  `SelectedItems` collection (`IsRowItemSelected`) instead of the shim's
  `_shimSelectedItems` — containers don't exist yet when the selection batch runs,
  so the build loop reads engine state directly.
- Deleted `ApplyRowSelectionVisuals()` (the manual visual pass) and its call site;
  `HandleShimRowClicked` now just computes the set, calls `SyncRealSelectedItems()`,
  and lets the engine's event paint the rows.

`_shimSelecteditems` still exists, but only as the *input* for click computation
(Ctrl/Shift range, anchor) — no longer the source for visuals. Retiring it fully
(routing clicks through `MakeFullRowSelection`) is the next step.

## Verification (confirmed, not assumed)

```
dotnet build  → 0 errors
dotnet run … --probe  → DONE failures=0  (34 steps)
dotnet test  → 126 passed, 0 failed
```

All selection probes pass with visuals driven by the engine: single-select
clear, multi-select Ctrl/Shift, selection-survives-sort (rebuild reads
`SelectedItems`), removing-selected-item clears highlight, keyboard nav, and the
session-61 `real SelectedItems + SelectionChanged` step.

## Follow-up Completion

The pending click-reroute item is now complete:

- `HandleShimRowClicked(row, modifiers)` now bridges Uno pointer modifiers into
  `Keyboard.Modifiers` and calls the linked WPF
  `HandleSelectionForRowHeaderAndDetailsInput(row, false)` path. That path
  reaches `MakeFullRowSelection`, so plain/Ctrl/Shift row clicks use the real
  Selector/MultiSelector engine.
- `_shimSelectedItems`, `_shimAnchorItem`, `_shimSelectedItem`, and
  `SyncRealSelectedItems()` were removed from the shim. Row selection state now
  lives in `SelectedItems`/`SelectedItem`, and rebuild pruning removes stale
  entries from the real selected-items collection.
- The probe's multi-select checks now assert `SelectedItems.Count` directly.
  Reflection tests pin the WPF row-selection handler and the modifier bridge
  instead of the retired shim fields.

## Verification (follow-up)

```
dotnet run … --probe  → DONE failures=0
dotnet test  → 126 passed, 0 failed
```

## Next Batch

1. Link the remaining base `DataGridColumn.cs` body and keep only narrow Uno
   bridge helpers locally. This is the highest-value column cleanup now that
   row/cell selection clicks are routed through the linked engine.

## Cell Selection Engine Follow-up

- `HandleShimCellClicked` now supplies the focus/current-cell side effect Uno
  lacks and then routes through linked WPF `HandleSelectionForCellInput` /
  `MakeCellSelection`; the retired `_shimSelectedCell*` fields are gone.
- Rebuild retention now reads `CurrentCell` / `SelectedCells` from the linked
  engine and reconciles realized cell visuals by item+column, avoiding stale
  container equality.
- Cell-selected item removal prunes `SelectedCells` and `CurrentCell` even when
  one surface has already been cleared.
- The add-new placeholder edit bridge now sets the new-item current cell with
  an explicit column fallback so upstream add-new logic does not depend on a
  stale or unset current-cell column after pruning.

## Verification (cell follow-up)

```
dotnet run … --probe  → DONE failures=0
dotnet test  → 126 passed, 0 failed
```
