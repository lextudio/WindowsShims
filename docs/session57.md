# DataGrid Port - Session 57

Date: 2026-06-13

## Goal

Land the **RowDetails** rung (session 49 backlog item): a row materializes the
grid's `RowDetailsTemplate` into an expandable details section below its cells,
honoring `RowDetailsVisibilityMode` and selection, and raising the WPF
`LoadingRowDetails` / `UnloadingRowDetails` / `RowDetailsVisibilityChanged`
events.

## Design (confirmed, not guessed)

Investigated the upstream substrate before writing code:

- The linked `DataGrid.cs` already provides the public surface
  (`RowDetailsTemplate`, `RowDetailsTemplateSelector`, `RowDetailsVisibilityMode`,
  the three events) **and** the internal raise helpers
  `OnLoadingRowDetailsWrapper` / `OnUnloadingRowDetailsWrapper` /
  `OnRowDetailsVisibilityChanged`. These are reused as-is.
- `DataGridRow.cs` is a **local shim** (not linked from upstream), so the
  details rendering and effective-visibility computation live there.
- Upstream `OnCoerceDetailsVisibility` resolves visibility through the heavy
  `DataGridHelper.GetCoercedTransferPropertyValue` path. Rather than pull that
  in, the shim re-implements the same switch over `RowDetailsVisibilityMode`
  (Collapsed / Visible / VisibleWhenSelected), gated on
  `hasTemplate && isRealItem` (and `IsSelected` for VisibleWhenSelected) — a
  faithful mirror of the upstream logic.
- `RowDetailsTemplate` resolves to `Microsoft.UI.Xaml.DataTemplate` (no global
  alias), so the shim materializes it via `LoadContent()`.

## What Changed

`DataGridRow.cs`:
- Row template is now **vertical**: the existing header+cells row sits above a
  new `PART_DetailsHost` `ContentControl` (collapsed by default).
- Added `ComputeDetailsVisibility(owner)` mirroring the upstream coercion switch.
- Added `BuildRowDetails(owner)`: computes visibility, sets `DetailsVisibility`,
  materializes `RowDetailsTemplate` into a `DataGridDetailsPresenter`, and reuses
  the linked `OnLoadingRowDetailsWrapper` / `OnUnloadingRowDetailsWrapper` to
  raise events exactly once.
- `BuildCells()` now also calls `BuildRowDetails`.
- `UpdateSelectionVisual()` recomputes details for `VisibleWhenSelected` and
  raises the reused `OnRowDetailsVisibilityChanged` when the effective
  visibility flips on selection change.

`MainPage.cs` (probe): new step **"row details: template expands per
RowDetailsVisibilityMode + selection"** —
- `Visible` mode expands every real row's details and binds `{Binding City}`
  (verified text + `LoadingRowDetails` count == one per real row).
- `VisibleWhenSelected` collapses the unselected row, then expands it on
  selection, raising `RowDetailsVisibilityChanged`.

`DataGridControlRootLinkTests.cs`: new `RowDetailsSurfaceExists` test asserting
the shim methods exist and the reused linked wrappers/events/properties are
present.

## Verification

```
dotnet test  → 123 passed, 0 failed   (was 122; +1 RowDetailsSurfaceExists)
dotnet run … --probe  → DONE failures=0   (33 steps, +1 row-details)
```

Probe evidence:
- `Visible mode: host.Visibility=Visible, text='Paris', loading=4`
- `after select: host.Visibility=Visible, visChanged=2`
  (deselecting the old row collapses its details, selecting the new one expands
  it — one visibility change each, as WPF does.)

## Next Batch

1. `AlternatingRowBackground` / `AlternationCount` row striping.
2. Selection-during-add-new interactions (Escape cancels the add, `CurrentItem`
   tracking through the add transaction).
3. `AreRowDetailsFrozen` horizontal-scroll behavior (lower priority; needs a
   real scrolling viewport).
