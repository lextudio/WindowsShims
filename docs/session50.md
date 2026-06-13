# DataGrid Port - Session 50

Date: 2026-06-12

## Goal

Reuse milestone (option chosen: **Sorting via SortDescriptions**). Replace the
shim reflection sort with the real WPF `DataGrid` sort path so the linked
`PerformSort`/`DefaultSort`/`Sorting`-event code drives ordering.

## Why this matters

Audit finding: the upstream `DataGrid.cs` compiles with only 13 small fork
guards — its algorithms are present but dormant because their substrate is
shimmed. Sorting was the lowest-risk place to prove the loop: *wire the
substrate → the WPF logic runs → delete the parallel shim.*

## What Changed

- `ItemCollection`:
  - `SortDescriptions` getter now subscribes to its `CollectionChanged` and
    flags `NeedsRefresh` (the WPF sort path checks `Items.NeedsRefresh`).
  - `Refresh()` applies the sort descriptions — a stable multi-key
    `OrderBy`/`ThenBy` (reflection on `PropertyName`) over the backing list —
    and raises `Reset`. This is the real collection-view sort the WPF
    `DefaultSort` expects.
- `DataGridBoundColumn.OnBindingChanged` derives `SortMemberPath` from the
  binding path (as WPF does) so `PerformSort` has a property to sort on.
- `DataGrid`:
  - `HandleShimHeaderClicked(column)` now simply calls the linked
    `PerformSort(column)` — which commits edits, raises `Sorting`, runs
    `DefaultSort` (toggle direction, update `Items.SortDescriptions`), and
    refreshes. The `Reset` from `Refresh` triggers the existing reactivity
    rebuild, rendering rows in sorted order.
  - **Deleted** the shim sort: `_activeSortColumn`, `GetSortValue`, and the
    reflection branch of `OrderedItems` (now just `Items.Cast<object?>()`,
    i.e. the collection-view order). `HeaderContent` reads `column.SortDirection`
    (set by `DefaultSort`) for the ▲/▼ glyph.

## Verification

Build succeeded; 121 passed/0 failed; probe `DONE failures=0`. Same sort
results (ascending `[36,39,41,45]`, descending `[45,41,39,36]`) — now produced
by the WPF path. Reuse evidence asserted in the probe:
`Items.SortDescriptions.Count == 1` (PropertyName "Age") and
`column.SortDirection == Ascending`, both set by the linked `DefaultSort`, not
the shim.

## Notes / honest limits

- The actual *comparison* still uses reflection inside `ItemCollection`
  (a real `ICollectionView` with a property-engine comparer is not bridged) —
  but the *control flow* (PerformSort → Sorting event → SortDescriptions →
  Refresh → direction toggle/glyph) is now WPF's, and a `Sorting` handler can
  cancel/customize as in WPF.
- Multi-column sort (Shift) follows `PrepareForSort` (which skips clearing on
  Shift), but the shim header input does not pass Shift to add a second
  `SortDescription`; only single-key is exercised.
- Grouping sort-description bookkeeping (`GroupingSortDescriptionIndices`) is
  inert (no grouping).

## Reuse takeaway

The loop works: deleting ~40 lines of shim sort and wiring two substrate
points (ItemCollection sort + SortMemberPath) made the linked WPF sort code
run. The same approach applies next to editing (CurrentCell + edit commands)
and selection (Selector engine), which are larger substrate bridges.

## Next Session

1. Editing reuse: wire `CurrentCell`/`CurrentCellContainer` so the WPF
   `BeginEdit`/`CommitEdit` command flow runs.
2. Or selection reuse: drive the `Selector` selection engine.
3. Or continue feature breadth (row details, alternating rows).
