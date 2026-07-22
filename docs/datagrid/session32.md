# DataGrid Port - Session 32

Date: 2026-06-12

## Goal

Clear the retained selection when the selected item leaves the collection
(session-31 next step #1), so a removed item does not leave a dangling
`_shimSelectedItem` / stale `SelectedItem`.

## Outcome

Removing the selected item clears the selection. Probe: select row 0, remove
its item, and `SelectedItem` becomes null with no row highlighted.

## What Changed

- `BuildShimVisualTree` tracks whether the retained `_shimSelectedItem` was
  matched by any rebuilt row. If a selection was retained but no row matches
  (the item left the collection), it clears `_shimSelectedItem` and resets
  `SelectedItem` to null.
- Because the rebuild already runs on `Items` `CollectionChanged` (session 26
  reactivity), removing the selected item triggers this cleanup automatically.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 115 tests passed, 0 failed; probe `DONE failures=0`,
16 steps ok — "SelectedItem after remove = null".

## Notes / honest limits

- Cleanup happens on the next rebuild (collection-change driven), not via a
  per-item removal hook; for the shim's full-rebuild model these coincide.
- Still single-select and shim-driven; the WPF `Selector` selection pipeline
  (`SelectedItems`, `SelectionChanged`) remains inert.
- No "select nearest remaining row" behavior (WPF moves selection to a
  neighbor on delete); the shim simply clears.

## Next Session

1. Route shim selection + sort through the WPF `Selector` / `SortDescriptions`
   pipelines so `SelectionChanged` / `Sorting` events fire and `SelectedItems`
   is populated.
2. `Auto` column width (measure content) and cell-level selection with
   hit-testing.
3. Keyboard navigation (arrow keys move selection; `BringIntoView` scrolls).
