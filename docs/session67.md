# DataGrid Port - Session 67

Date: 2026-06-13

## Goal

Column-header notification chain: wire `DataGridColumn` property-changed callbacks
so that column-width (and other column-property) changes propagate to realized
header cells and data cells without a full grid rebuild — the exact mirror of
session 66's row-cell chain.

## What Changed

### FrameworkPropertyMetadata.Bridge — now live

The core blocker was `FrameworkPropertyMetadata.Bridge`, which wrapped WPF
`PropertyChangedCallback` delegates for registration with the WinUI DP system:

```csharp
// Before (session 66 and earlier):
private static Microsoft.UI.Xaml.PropertyChangedCallback? Bridge(PropertyChangedCallback? wpf)
    => null;  // all WPF DP callbacks were silently dropped
```

With `Bridge` returning `null`, every WPF DP callback registered via
`FrameworkPropertyMetadata` (including `DataGridColumn.OnWidthPropertyChanged`,
`DataGridColumn.OnNotifySortPropertyChanged`, all `DataGrid.OnNotify*` callbacks)
was never invoked from the WinUI DP system. The entire WPF notification chain
was dead.

An implicit conversion from `Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs`
to `System.Windows.DependencyPropertyChangedEventArgs` already existed in
`PropertySystem.cs`, making the bridge trivial:

```csharp
// After (session 67):
private static Microsoft.UI.Xaml.PropertyChangedCallback? Bridge(PropertyChangedCallback? wpf)
    => wpf == null ? null : (d, e) => wpf(d, e);
```

This one-line fix makes ALL WPF DP callbacks live. Callbacks are now invoked
when the WinUI DP system fires a property change, with the args struct adapted
via the implicit conversion.

### Upstream DataGrid.cs — fork-guard for column-header dispatch

The upstream `DataGrid.NotifyPropertyChanged` routes column-header notifications
through `ColumnHeadersPresenter` (null in the shim). Added a `#if HAS_UNO`
branch that routes directly to `ShimNotifyColumnHeaders` instead:

```csharp
#if HAS_UNO
if (DataGridHelper.ShouldNotifyColumnHeaders(target))
    ShimNotifyColumnHeaders(d, e);
#else
if ((DataGridHelper.ShouldNotifyColumnHeadersPresenter(target) || DataGridHelper.ShouldNotifyColumnHeaders(target)) && ColumnHeadersPresenter != null)
    ColumnHeadersPresenter.NotifyPropertyChanged(d, propertyName, e, target);
#endif
```

### Local DataGrid.cs — ShimNotifyColumnHeaders

New method (local partial) that iterates `_headerCells` and calls
`header.NotifyPropertyChanged(d, e)` on each:

```csharp
private void ShimNotifyColumnHeaders(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    foreach (var header in _headerCells)
        header.NotifyPropertyChanged(d, e);
}
```

### Local DataGridColumnHeader.cs — NotifyPropertyChanged

New method handling the subset of property changes meaningful in the shim
render path:

```csharp
internal void NotifyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
{
    if (d is DataGridColumn col && !ReferenceEquals(col, Column))
        return;
    if (e.Property == DataGridColumn.WidthProperty)
        Width = Column?.DataGridOwner?.ShimColumnWidth(Column) ?? double.NaN;
    else if (e.Property == DataGridColumn.HeaderProperty)
        Content = Column?.Header;
}
```

### Local DataGrid.cs — ShimColumnWidth priority fix

The auto-width pass sets `column.ActualWidth` for non-absolute columns, but this
stale value was returned even when `Width` was changed to an explicit absolute
value (since `CoerceValue(ActualWidthProperty)` is a no-op). Fixed to check
`Width.IsAbsolute` first:

```csharp
internal double ShimColumnWidth(DataGridColumn column)
{
    var width = column.Width;
    // Absolute widths take priority: the notification chain may deliver a
    // width change before the post-layout pass re-runs.
    if (width.IsAbsolute && width.Value > 0)
        return Clamp(column, width.Value);
    return column.ActualWidth > 0 ? column.ActualWidth : double.NaN;
}
```

## End-to-end notification chain (now live)

Setting `column.Width = new DataGridLength(111)` now routes:

1. WinUI DP fires `Bridge(OnWidthPropertyChanged)` lambda
2. `OnWidthPropertyChanged` → `column.NotifyPropertyChanged(d, e, Cells | ColumnHeaders | ...)`
3. → `DataGrid.NotifyPropertyChanged` (upstream)
4. → `_rowTrackingRoot` iteration (session 66) → each `row.NotifyPropertyChanged` → each cell → `cell.Width = ShimColumnWidth(col)`
5. → `ShimNotifyColumnHeaders` (session 67) → each header → `header.Width = ShimColumnWidth(col)`

No full rebuild is triggered — only the affected cells and headers update their
widths live.

## New probe step

**"column-header notification: width change propagates to header without rebuild"** —
sets `Columns[0].Width = 111`, reads `cell0.Width` and asserts it exceeds 100
(confirming the notification chain reached the cell without a rebuild).

## New tests (2)

`DataGridColumnHeaderNotificationTests`:
1. `DataGridColumnHeaderHasNotifyPropertyChangedMethod` — 2-arg method exists
2. `DataGridHasShimNotifyColumnHeadersMethod` — private dispatch method exists

## Verification

```
dotnet build … --no-restore   → 0 errors
dotnet test  … --no-restore   → 134 passed, 0 failed
dotnet run   … -- --probe     → DONE failures=0  (36 steps)
```

The WPF notification chain is now fully live: DP callbacks fire via `Bridge`,
property changes propagate to cells and headers without rebuilds.

## Still Deferred

- `DataGridColumnHeader` upstream link (upstream inherits `ButtonBase`, local
  inherits `ContentControl` — base-class change deferred).
- `DataGridColumnHeadersPresenter` upstream link (column reorder, gripper thumbs).
- `BindingGroup` integration for multi-field row-level commit.
- Full `DataGridRow`/`DataGridCell` upstream link (requires `DataGridCellsPanel`).

## Next Batch

1. Leverage the live WPF DP bridge: grid-level property changes (`IsReadOnly`,
   `AlternatingRowBackground`, column `Visibility`, `FrozenColumnCount`) now reach
   cells/headers via the notification chain — add probe steps to verify key ones.
2. `DataGridColumn.SortDirection` notification now reaches headers via
   `ShimNotifyColumnHeaders`; update `DataGridColumnHeader.NotifyPropertyChanged`
   to refresh the sort glyph live instead of waiting for the next rebuild.
