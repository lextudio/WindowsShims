# DataGrid Port - Session 68

Date: 2026-06-13

## Goal

Leverage the live WPF DP bridge from session 67 to wire additional property-change
notifications that were already implemented but unverified, add column-visibility
live rebuild, and add sort-direction glyph refresh without full rebuilds.

## What Changed

### Upstream DataGrid.cs — column-visibility #if HAS_UNO fork

The `ShouldNotifyDataGrid` branch in `DataGrid.NotifyPropertyChanged` handles
`VisibilityProperty | WidthProperty | DisplayIndexProperty` by iterating
`RecyclableContainers` (always empty in the shim). Added `#if HAS_UNO` to call
`BuildShimVisualTree()` when `VisibilityProperty` changes:

```csharp
else if ((e.Property == DataGridColumn.VisibilityProperty) || ...)
{
#if !HAS_UNO
    foreach (DependencyObject container in ItemContainerGenerator.RecyclableContainers)
    { ... }
#else
    if (e.Property == DataGridColumn.VisibilityProperty)
        BuildShimVisualTree();
#endif
}
```

This ensures `column.Visibility = Collapsed/Visible` triggers a full rebuild,
which respects `column.IsVisible` in `BuildCells` and rebuilds all rows with
the correct column set.

### Local DataGridColumnHeader.cs — SortDirection glyph + Visibility

Extended `NotifyPropertyChanged` with two new branches:

```csharp
else if (e.Property == DataGridColumn.HeaderProperty || e.Property == DataGridColumn.SortDirectionProperty)
    Content = Column?.DataGridOwner?.HeaderContent(Column) ?? Column?.Header;
else if (e.Property == DataGridColumn.VisibilityProperty)
    Visibility = Column?.Visibility ?? Visibility.Visible;
```

- `SortDirectionProperty`: WPF fires `OnNotifySortPropertyChanged(target=ColumnHeaders)` →
  `ShimNotifyColumnHeaders` → each header gets `HeaderContent(column)`, which
  appends "▲"/"▼" to the header text.
- `VisibilityProperty`: the header hides/shows to match the column.
- `HeaderProperty` now also routes through `HeaderContent` so the glyph is
  preserved when the header text is renamed.

### Local DataGrid.cs — HeaderContent made internal

`HeaderContent(DataGridColumn)` was `private`; changed to `internal` so
`DataGridColumnHeader.NotifyPropertyChanged` can call it for glyph-decorated text.

## End-to-end chains now live (session 68 additions)

### Column rename
`column.Header = "Renamed"` → Bridge → `OnNotifyColumnAndColumnHeaderPropertyChanged`
→ `DataGrid.NotifyPropertyChanged(target=Columns|ColumnHeaders)` →
`ShimNotifyColumnHeaders` → `header.Content = HeaderContent(column)` (glyph preserved).

### Sort direction glyph
`column.SortDirection = Ascending` → Bridge → `OnNotifySortPropertyChanged(target=ColumnHeaders)`
→ `ShimNotifyColumnHeaders` → `header.Content = HeaderContent(column)` ("▲" appended).
No rebuild needed.

### Column visibility
`column.Visibility = Collapsed` → Bridge → `OnVisibilityPropertyChanged(target=DataGrid|ColumnHeaders)`
→ `DataGrid.NotifyPropertyChanged` #if HAS_UNO calls `BuildShimVisualTree()` (rebuilds rows
without the hidden column) + `ShimNotifyColumnHeaders` → `header.Visibility = Collapsed`.

### Grid IsReadOnly
`grid.IsReadOnly = true` → Bridge → `OnIsReadOnlyChanged` → `OnNotifyColumnAndCellPropertyChanged`
→ `DataGrid.NotifyPropertyChanged(target=Columns|Cells)` → `_rowTrackingRoot` → each row →
each cell → `cell.IsReadOnly = IsCellEffectivelyReadOnly(column)`. All live, no rebuild.

## Probe ordering note

The IsReadOnly probe step runs **before** the column-visibility step because the
visibility step calls `BuildShimVisualTree()` twice (collapse + restore), creating
fresh rows whose `OnApplyTemplate` (and therefore `BuildCells`) hasn't fired until
the next UI layout pass. Any step that relies on `TryGetCell` must run before the
visibility step, or obtain its container after the visibility step's restore
triggers a layout pass.

## New probe steps (4)

1. **"column-header notification: Header rename propagates without rebuild"** — sets
   `col.Header = "RenamedName"`, asserts header Content contains "RenamedName".
2. **"column-header notification: SortDirection glyph updates live"** — sets
   `col.SortDirection = Ascending`, asserts header Content contains "▲".
3. **"grid IsReadOnly live update propagates to cells"** — sets `grid.IsReadOnly = true/false`,
   asserts `cell.IsReadOnly` flips without rebuild.
4. **"column visibility: collapse hides column; restore rebuilds"** — sets
   `col.Visibility = Collapsed`, asserts cell count drops; restores.

## New tests (2)

`DataGridColumnHeaderNotificationTests`:
- `DataGridColumnHeaderNotifyPropertyChangedHandlesSortDirectionProperty` — confirms
  `SortDirectionProperty` field is accessible and `NotifyPropertyChanged` exists.
- `DataGridHeaderContentIsInternal` — confirms `HeaderContent` is `internal` (not `private`).

## Verification

```
dotnet build … --no-restore   → 0 errors
dotnet test  … --no-restore   → 136 passed, 0 failed
dotnet run   … -- --probe     → DONE failures=0  (40 steps)
```

## Still Deferred

- `DataGridColumnHeader` upstream link (upstream inherits `ButtonBase`, local
  inherits `ContentControl` — base-class change deferred).
- `DataGridColumnHeadersPresenter` upstream link (column reorder, gripper thumbs).
- `BindingGroup` integration for multi-field row-level commit.
- Full `DataGridRow`/`DataGridCell` upstream link (requires `DataGridCellsPanel`).

## Next Batch

1. **Column-level IsReadOnly live update** — `column.IsReadOnly = true` fires
   `OnNotifyCellPropertyChanged(target=Columns|Cells)` → cells update `IsReadOnly`
   live. Add probe step to verify.
2. **AlternatingRowBackground live update** — `grid.AlternatingRowBackground = brush`
   fires `OnAlternatingRowBackgroundPropertyChanged` → coerces `AlternationCount` →
   rows re-alternate. Add probe step.
3. **FrozenColumnCount live update** — `grid.FrozenColumnCount` change fires
   `OnNotifyColumnAndColumnHeaderPropertyChanged` → columns and headers update
   frozen state. Shim doesn't implement column freezing yet; add probe step to
   confirm the notification fires at minimum.
4. **CellStyle live update** — `grid.CellStyle = style` fires
   `OnNotifyColumnAndCellPropertyChanged` → cells pick up the new style.
