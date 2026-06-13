# DataGrid Port - Session 56

Date: 2026-06-13

## Goal

Fix the placeholder-cell-edit add-new probe step so the full WPF-routed add-new
flow is probe-verified end-to-end, completing the work started in session 55.

## Root Cause

`RestoreEditingCellAfterRebuild` called `row.TryGetCell(i)` on a freshly-built
`DataGridRow` before the WinUI layout system had applied the row's
`ControlTemplate`. Template application (`OnApplyTemplate` → `BuildCells()`)
fires only after `base.UpdateLayout()` returns, but
`RestoreEditingCellAfterRebuild` is called from inside `BuildShimVisualTree()`
which runs before `base.UpdateLayout()`. So `_cells` was empty, `TryGetCell`
returned `null`, and editing was not restored on the new-item row.

The same issue did not manifest for the normal-sort editing restore path because
the probe always called `_grid.UpdateLayout()` before reading cells, and the
prior probes did not rebuild during an active edit. The add-new path is the first
case that calls `_grid.UpdateLayout()` (from the probe) while an add transaction
is in progress and must restore an editing cell on a brand-new row instance.

## What Changed

`DataGrid.cs` (local partial) — `RestoreEditingCellAfterRebuild`:

- Added `row.ApplyTemplate()` before `TryGetCell` to force synchronous template
  application on the freshly-inserted row.  This is the same technique
  `ShimBeginEditPlaceholder` achieves via `row.UpdateLayout()`, but without
  triggering a full WinUI layout pass.

## Verification

```
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
→ 122 passed, 0 failed

dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
→ DONE failures=0
```

The add-new probe step now confirms:
- `AddingNewItem` and `InitializingNewItem` each fire once
- `Items.IsAddingNew` is true after placeholder begin-edit
- The new row and Age cell are realized and in editing mode
- `CommitEdit` keeps the item, restores the placeholder, and leaves `IsAddingNew=false`

## Next Batch

1. Selection interactions during add-new (cancel-new via Escape, `CurrentItem`
   tracking during the add transaction).
2. `RowDetails` surface (session 49 backlog item).
3. Alternating row background (`AlternatingRowBackground` / `AlternationCount`).
