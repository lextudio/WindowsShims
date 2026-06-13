# DataGrid Port - Session 69

Date: 2026-06-13

## Goal

Row-level property notifications: add alternating-row-background striping (initial
build + live update via notification chain), verify column-level `IsReadOnly`
propagates to cells live, and probe grid-level `IsReadOnly` (already wired in
session 68, confirmed here).

## What Changed

### Local DataGrid.cs — ShimRowBackground + BuildShimVisualTree row indexing

New internal helper that computes a row's background brush from the grid's
`RowBackground` / `AlternatingRowBackground` and the row's 0-based index:

```csharp
internal Microsoft.UI.Xaml.Media.Brush? ShimRowBackground(int rowIndex)
    => rowIndex % 2 == 1 && AlternatingRowBackground is { } alt ? alt : RowBackground;
```

WPF alternation convention: index 0 → `RowBackground`, index 1 → `AlternatingRowBackground`.

`BuildShimVisualTree` now tracks a `rowIndex` counter, assigns it to `row.ShimRowIndex`,
and calls `row.ApplyShimRowBackground()` after `PrepareRow`:

```csharp
var rowIndex = 0;
foreach (var item in OrderedItems())
{
    ...
    row.ShimRowIndex = rowIndex++;
    row.ApplyShimRowBackground();
    ...
}
```

### Local DataGridRow.cs — ShimRowIndex, ApplyShimRowBackground, NotifyPropertyChanged

Three additions to the local partial:

1. `ShimRowIndex` property — stores the 0-based position in the rendered set.

2. `ApplyShimRowBackground()` — applies the grid's stripe brush when the row is
   not selected:
   ```csharp
   internal void ApplyShimRowBackground()
   {
       if (!_isSelected)
           Background = DataGridOwner?.ShimRowBackground(ShimRowIndex);
   }
   ```

3. `UpdateSelectionVisual` — changed to restore the stripe brush on deselect
   instead of setting `null`:
   ```csharp
   Background = _isSelected ? _selectedBrush : DataGridOwner?.ShimRowBackground(ShimRowIndex);
   ```

4. `NotifyPropertyChanged` (local) — added `ShouldNotifyRows` branch:
   ```csharp
   if (DataGridHelper.ShouldNotifyRows(target))
   {
       if (args.Property == DataGrid.RowBackgroundProperty
           || args.Property == DataGrid.AlternatingRowBackgroundProperty)
           ApplyShimRowBackground();
   }
   ```

## End-to-end chains now live (session 69 additions)

### AlternatingRowBackground live update

`grid.AlternatingRowBackground = brush` → Bridge → `OnNotifyDataGridAndRowPropertyChanged`
(target=`Rows|DataGrid`) → `DataGrid.NotifyPropertyChanged` → `ShouldNotifyRowSubtree` →
each `row.NotifyPropertyChanged` → `ShouldNotifyRows` → `ApplyShimRowBackground()` →
`row.Background = ShimRowBackground(ShimRowIndex)`.

Odd-indexed rows immediately update to the alternating brush; even-indexed rows
use `RowBackground` (or null if not set).

### Column.IsReadOnly live update

`column.IsReadOnly = true` → Bridge → `DataGridColumn.OnNotifyCellPropertyChanged`
(target=`Columns|Cells`) → `DataGrid.NotifyPropertyChanged` → `_rowTrackingRoot` →
each `row.NotifyPropertyChanged` → each cell → `cell.IsReadOnly =
IsCellEffectivelyReadOnly(column)`. No rebuild needed.

## Probe ordering note

Steps that rely on `TryGetCell` (which requires `OnApplyTemplate` to have fired)
must run before the column-visibility step. The visibility step triggers
`BuildShimVisualTree()` twice (collapse + restore), creating fresh rows whose
cells are not populated until the next layout pass. Ordering in the probe:

1. column-header notification steps (work on header cells, not row cells)
2. grid.IsReadOnly, column.IsReadOnly (row cells — before visibility)
3. column visibility (rebuilds — row cells inaccessible after this)
4. alternating row background (uses `Background` property, not `TryGetCell`)

## New probe steps (2)

1. **"column IsReadOnly live update propagates to cells"** — sets `col[1].IsReadOnly =
   true`, asserts `cell[1].IsReadOnly = true`; restores.
2. **"alternating row background: initial stripe + live update"** — sets
   `grid.AlternatingRowBackground = LightBlue`, asserts row1 has the brush and row0
   does not; clears and asserts row1 reverts.

## Verification

```
dotnet build … --no-restore   → 0 errors
dotnet test  … --no-restore   → 136 passed, 0 failed
dotnet run   … -- --probe     → DONE failures=0  (42 steps)
```

## Still Deferred

- `DataGridColumnHeader` upstream link (upstream inherits `ButtonBase`, local
  inherits `ContentControl`).
- `DataGridColumnHeadersPresenter` upstream link.
- `BindingGroup` integration.
- Full `DataGridRow`/`DataGridCell` upstream link.

## Next Batch

1. **RowBackground live update probe** — `grid.RowBackground` change fires
   `OnNotifyRowPropertyChanged(target=Rows)` → same `ApplyShimRowBackground` path.
   Add a probe step verifying even rows update to a custom brush.
2. **GridLinesVisibility** — `grid.GridLinesVisibility` change fires
   `OnNotifyGridLinePropertyChanged(target=Rows|Cells|ColumnHeaders|RowHeaders)`.
   The shim doesn't render grid lines; add a probe confirming the notification fires
   without error.
3. **CanUserResizeColumns / CanUserReorderColumns** — `OnNotifyColumnAndColumnHeaderPropertyChanged`;
   headers currently have no resize gripper; probe step confirms no crash.
4. **Frozen column initial support** — `FrozenColumnCount > 0` means the first N
   columns should not scroll. In the shim's horizontal StackPanel layout, "frozen"
   is currently not honored. Minimal support: mark frozen cells with a distinct
   background or `IsHitTestVisible = false` on the scroll logic.
