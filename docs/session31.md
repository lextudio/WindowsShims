# DataGrid Port - Session 31

Date: 2026-06-12

## Goal

Preserve selection across render rebuilds (session-30 next step #1): after a
sort or a collection change rebuilds the rows, the highlighted row should
follow its data item rather than being lost.

## Outcome

Selection survives a rebuild. Probe: select a row, re-sort (which rebuilds all
rows), and the row now holding the originally-selected item is still
highlighted.

## What Changed

- `DataGrid._shimSelectedItem` retains the selected item by identity.
  `HandleShimRowClicked` records it alongside setting `IsSelected` /
  `SelectedItem`.
- `BuildShimVisualTree` re-applies selection while building rows: a freshly
  built `DataGridRow` whose item matches `_shimSelectedItem` (via
  `ItemsControl.EqualsEx`) is marked `IsSelected`, so the highlight tracks the
  item through sort and reactivity rebuilds.
- Test `RetainedSelectionFieldExists` pins the retained-selection field;
  the behavior is verified by the probe. 115 tests total.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 115 tests passed, 0 failed; probe `DONE failures=0`
— "selected item still highlighted after sort = True" (15 probe steps ok).

## Notes / honest limits

- Single-item retention only (matches the shim single-select). When the
  selected item is removed from the collection, `_shimSelectedItem` is left
  dangling until the next click — no row matches, so nothing highlights, but
  the stale reference is not cleared.
- Still does not flow through the WPF `Selector` selection pipeline;
  `SelectedItems`/`SelectionChanged` remain inert.

## Next Session

1. Clear/repair `_shimSelectedItem` when the selected item leaves the
   collection (and reflect into `SelectedItem`).
2. Route shim selection + sort through the WPF `Selector` /
   `SortDescriptions` pipelines so `SelectionChanged` / `Sorting` events fire.
3. `Auto` column width (measure content) and cell-level selection with
   hit-testing.
