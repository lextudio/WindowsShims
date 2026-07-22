# DataGrid Port - Session 78

Date: 2026-06-13

## Goal

Link the upstream WPF `DataGridRow.cs` as a partial file, bringing in
`IsSelectedProperty`, `IsEditingPropertyKey`/`IsEditingProperty`,
`IsNewItemPropertyKey`/`IsNewItemProperty`, `DataGridOwner`, `Tracker`,
`GetRowContainingElement`, and the instance constructor — maximizing WPF code
reuse and reducing local shims.

## What Changed

### `ext/wpf/.../Controls/DataGridRow.cs` (upstream)

Added `partial` to the class declaration. Applied `#if !HAS_UNO` guards to all
code that references WPF-only APIs:

| Section | Guard |
|---|---|
| `#region Constants` body (state machine arrays) | `#if !HAS_UNO` |
| Static cctor body | `#if !HAS_UNO` |
| `#region Template` | `#if !HAS_UNO` |
| `#region Visual States` (`ChangeVisualState`, `IsDataGridKeyboardFocusWithin`) | `#if !HAS_UNO` |
| `#region Row Header` | `#if !HAS_UNO` |
| `#region Row Details` (`DetailsTemplate`, `DetailsTemplateSelector`, `DetailsVisibility` DP, `DetailsVisibilityProperty`, `DetailsLoaded`) | `#if !HAS_UNO` |
| `#region Row Generation` (`OnPropertyChanged`, `PrepareRow`, `ClearRow`, `PersistAttachedItemValue`, `RestoreAttachedItemValue`) | `#if !HAS_UNO` |
| `#region Row Resizing` | `#if !HAS_UNO` |
| `#region Columns Notification` (`OnColumnsChanged`) | `#if !HAS_UNO` |
| `#region Property Coercion` | `#if !HAS_UNO` |
| `OnNotifyDetailsTemplatePropertyChanged`, `DelayedRowDetailsVisibilityChanged` | `#if !HAS_UNO` |
| `CellsPresenter`, `DetailsPresenter`, `RowHeader` properties | `#if !HAS_UNO` |
| Both `NotifyPropertyChanged` overloads (upstream) | `#if !HAS_UNO` |
| `DelayedValidateWithoutUpdate`, `SyncProperties` | `#if !HAS_UNO` |
| `#region Alternation` (`AlternationIndexProperty`) | `#if !HAS_UNO` |
| Automation block in `OnIsSelectedChanged` | `#if !HAS_UNO` |
| `IsSelectable` SR string reference | `#if !HAS_UNO` |
| `row.UpdateVisualState()` | `#if !HAS_UNO` |
| `OnCreateAutomationPeer` | `#if !HAS_UNO` |
| `ScrollCellIntoView(int)` | `#if !HAS_UNO` |
| `ArrangeOverride` | `#if !HAS_UNO` |
| `GetIndex()`, `DetailsPresenterDrawsGridLines`, `TryGetCell` | `#if !HAS_UNO` |
| `#region Data` (backing fields `_owner`, `_tracker`) | `#if !HAS_UNO` |
| `OnItemChanged` body (`cellsPresenter?.Item = newItem`) | `#if !HAS_UNO` |

Active under HAS_UNO (WinUI-compatible):
- `IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(...)` + `Selected`/`Unselected` events + `OnSelected`/`OnUnselected` + `RaiseSelectionChangedEvent`
- `IsEditingPropertyKey`/`IsEditingProperty`/`IsEditing`
- `IsNewItemPropertyKey`/`IsNewItemProperty`/`IsNewItem`
- `DataGridOwner` property (backed by local `_owner`)
- `Tracker` property (backed by local `_tracker`)
- `GetRowContainingElement` static helper
- Instance constructor (`_tracker = new ContainerTracking<DataGridRow>(this)`)
- `OnNotifyRowPropertyChanged`, `OnNotifyRowAndRowHeaderPropertyChanged` (simple delegates)

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridRow.cs` (local)

Complete rewrite of the local partial to complement the newly-linked upstream:

- **Backing fields**: `_owner` (DataGrid?), `_tracker` (ContainerTracking<DataGridRow>) — consumed by upstream properties
- **`DetailsVisibilityProperty`**: kept local (upstream version references WPF-only callbacks)
- **`DetailsVisibility`**, `DetailsLoaded`, `ShimAppliedRowStyle`, `CellsPresenter`, `DetailsPresenter`, `RowHeader`, `BindingGroup`, `ShimRowIndex` — local properties
- **`PrepareRow`**: sets `Item`, `_owner`, `DataContext`, `IsNewItem`, calls `BuildCells()`
- **`BuildCells()`**: iterates `DataGridOwner.ColumnsInDisplayOrder()` (not `Columns`, to respect display-index ordering), calls `cell.BuildVisualTree()`, `DataGridOwner.TryReselectCell(cell)`, `cell.ApplyShimGridLines()`
- **`BuildRowDetails()`**: creates `DataGridDetailsPresenter`, calls `owner.OnLoadingRowDetailsWrapper(this)` so `LoadingRowDetails` event fires
- **`BuildRowHeader()`**: creates `DataGridRowHeader`, calls `_rowHeaderElement.ApplyShimGridLines()` so grid-line borders are applied on construction
- **`InitializeDefaultStyleKey()`**: `RegisterPropertyChangedCallback(IsSelectedProperty, ...)` fires `UpdateSelectionVisual()` AND `RaiseEvent(SelectedEvent/UnselectedEvent)` — necessary because `AddOwner` is a no-op shim that doesn't register the upstream `OnIsSelectedChanged` callback
- **`RegisterPropertyChangedCallback(IsEditingProperty, ...)`**: fires `RefreshRowHeaderGlyph()`
- **3-arg `NotifyPropertyChanged`**: delegates to 4-arg version (upstream callbacks use 3-arg form)
- **`OnColumnsChanged`**: kept local, calls `BuildCells()`

### `src/LeXtudio.Windows/LeXtudio.Windows.csproj`

Added `<Compile>` link:
```xml
<Compile Include="..\..\ext\wpf\...\DataGridRow.cs"
         Link="System.Windows.Controls\DataGridRow.upstream.cs" />
```

## Key Fixes During Session

1. **`#if`/`#endif` balance**: two unclosed guards (Constants region arrays, Property Coercion) added `#endif` before `#endregion`
2. **`OnItemChanged` body**: guarded `cellsPresenter?.Item = newItem` — `DataGridCellsPresenter` has no `Item` property in the shim
3. **Display-index ordering**: changed `BuildCells()` from `DataGridOwner.Columns` to `ColumnsInDisplayOrder()` so column reordering propagates to row cells
4. **Cell reselection after rebuild**: added `DataGridOwner.TryReselectCell(cell)` in `BuildCells()` — comment on the method explicitly says "Called by a row as it (re)builds a cell"
5. **`IsSelected` callback chain**: `AddOwner` shim is a no-op so `OnIsSelectedChanged` (and thus `RaiseSelectionChangedEvent`) never fires; replicated by calling `RaiseEvent(SelectedEvent/UnselectedEvent)` from the `RegisterPropertyChangedCallback`
6. **`LoadingRowDetails` event**: `OnLoadingRow` is only called from `PrepareContainerForItemOverride` (WPF layout path not used in shim); fixed by calling `owner.OnLoadingRowDetailsWrapper(this)` directly from `BuildRowDetails`
7. **Row-header grid lines**: `BuildRowHeader` didn't call `ApplyShimGridLines()`; added the call after constructing `_rowHeaderElement`
8. **Cell `HasShimGridLine`**: replaced manual `BorderBrush`/`BorderThickness` assignment in `BuildCells()` with `cell.ApplyShimGridLines()` so the tracked `HasShimGridLine` flag is set

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/                     # Passed: 136
dotnet run   src/LeXtudio.Windows.Sample/ -- --probe         # DONE failures=0
```
