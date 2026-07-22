# DataGrid Port - Session 66

Date: 2026-06-13

## Goal

Row/cell container state cleanup: wire the upstream DataGrid notification chain
(`DataGrid.NotifyPropertyChanged → _rowTrackingRoot → row → cells`) so that
column/grid property changes propagate to realized cells without a full rebuild.
Also add a live current-cell focus-border visual via `NotifyCurrentCellContainerChanged`.

## What Changed

### Row tracker wiring

The upstream `DataGrid.NotifyPropertyChanged` iterates `_rowTrackingRoot` (a
`ContainerTracking<DataGridRow>` linked list) to push property changes to every
realized row container. Previously, `_rowTrackingRoot` was always null because
`PrepareContainerForItemOverride` (where `StartTracking` is called) is
fork-guarded in the shim.

- **`DataGridRow.PrepareRow`** — initializes `Tracker ??= new ContainerTracking<DataGridRow>(this)`.
- **`DataGrid.BuildShimVisualTree`** (local partial) — resets `_rowTrackingRoot = null`
  before each rebuild; calls `row.Tracker!.StartTracking(ref _rowTrackingRoot)`
  after `PrepareRow`. The partial-class shares the private field with the upstream
  DataGrid.cs, so this compiles without indirection.

### DataGridRow.NotifyPropertyChanged real implementation

Replaces the no-op stub. Forwards cell-targeting notifications to the `_cells`
backing list so the upstream column-property-changed callbacks reach realized
cells. The upstream would route through `DataGridCellsPresenter`; the shim routes
directly.

```
Notifications forwarded when target includes Cells | CellsPresenter | RefreshCellContent:
  → each cell in _cells receives cell.NotifyPropertyChanged(...)
```

### DataGridRow.OnColumnsChanged signature fix

The upstream `DataGrid.UpdateColumnsOnRows` calls
`row.OnColumnsChanged(_columns, e)` where `_columns` is
`ObservableCollection<DataGridColumn>`. The shim had the wrong signature
`(object? sender, ...)` — updated to match the upstream:

```csharp
protected internal virtual void OnColumnsChanged(
    ObservableCollection<DataGridColumn> columns,
    NotifyCollectionChangedEventArgs e) { }
```

### DataGridCell.NotifyPropertyChanged real implementation

Replaces the no-op stub. Handles the subset meaningful in the shim render path:

| Property changed | Action |
|---|---|
| `DataGridColumn.WidthProperty` | `Width = DataGridOwner?.ShimColumnWidth(Column)` |
| `DataGrid.IsReadOnlyProperty` / `DataGridColumn.IsReadOnlyProperty` | `IsReadOnly = DataGridOwner?.IsCellEffectivelyReadOnly(Column)` |
| `ShouldRefreshCellContent` flag | `BuildVisualTree()` |

Notifications from a column other than `cell.Column` are dropped — matching the
upstream guard.

### DataGridCell.NotifyCurrentCellContainerChanged real implementation

Replaces the no-op stub. Paints a system-accent blue focus border on the current
cell; clears it (if no validation error) when the cell is no longer current.

```csharp
var isCurrent = DataGridOwner?.CurrentCellContainer == this;
if (isCurrent)
{
    BorderBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x78, 0xD4));
    BorderThickness = new Thickness(2);
}
else if (!HasValidationError)
{
    BorderBrush = null;
    BorderThickness = new Thickness(0);
}
```

### HandleShimCellClicked explicit visual sync

The upstream `CurrentCellContainer` setter drives `NotifyCurrentCellContainerChanged`
only when `IsKeyboardFocusWithin`, which is false in headless / non-focused
scenarios. Added an explicit call in `HandleShimCellClicked` after selection sync:

```csharp
if (oldCurrentCell is not null && !ReferenceEquals(oldCurrentCell, cell))
    oldCurrentCell.NotifyCurrentCellContainerChanged();
cell.NotifyCurrentCellContainerChanged();
```

### DataGridHelper ShouldNotify* predicates

Added the missing predicates the new row/cell implementations call:
`ShouldNotifyCells`, `ShouldNotifyCellsPresenter`, `ShouldNotifyDetailsPresenter`,
`ShouldNotifyRows`, `ShouldNotifyRowHeaders`, `ShouldRefreshCellContent`.
These match the upstream implementations exactly.

## New probe step

**"current-cell visual: NotifyCurrentCellContainerChanged paints focus border"** —
sets `SelectionUnit = Cell`, clicks `cell0`, verifies `BorderThickness` is
non-zero; clicks `cell1`, verifies `cell0`'s border is cleared.

## New tests (6)

`DataGridNotificationChainTests`:
1. `DataGridRowTrackerPropertyExists` — `Tracker` is `ContainerTracking<DataGridRow>`
2. `DataGridRowNotifyPropertyChangedSignatureMatchesUpstream` — correct 4-arg signature
3. `DataGridCellNotifyPropertyChangedSignatureMatchesUpstream` — correct 4-arg signature
4. `DataGridRowOnColumnsChangedSignatureMatchesUpstream` — `ObservableCollection<DataGridColumn>` signature
5. `DataGridCellNotifyCurrentCellContainerChangedSignatureExists` — no longer a stub
6. `DataGridHelperExposesNotificationTargetPredicates` — all 6 predicates present

## Verification

```
dotnet build … --no-restore   → 0 errors
dotnet test  … --no-restore   → 132 passed, 0 failed
dotnet run   … -- --probe     → DONE failures=0
```

The notification chain is now live: column width changes propagate to cells
without a full rebuild, and the current cell shows a distinct focus border.

## Still Deferred

- Route clicks through `HandleSelectionForRowHeaderAndDetailsInput →
  MakeFullRowSelection`, retiring `_shimSelectedItems` + shim Ctrl/Shift range
  logic (session 62 pending).
- Column width/realization regions in `DataGridColumnCollection.uno.cs`
  (deferred until shim width pass is ready to hand off).
- Full `DataGridRow`/`DataGridCell` upstream link — infeasible until
  `DataGridCellsPanel` and WPF visual-state machinery are bridged.

## Next Batch

1. Selection engine cleanup: route `HandleShimRowClicked` through
   `HandleSelectionForRowHeaderAndDetailsInput → MakeFullRowSelection`, retiring
   the `_shimSelectedItems` backing list and the shim Ctrl/Shift range logic.
2. `DataGridRow` container state: `BindingGroup` integration for multi-field
   row-level commits (currently the row edit path bypasses binding groups).
