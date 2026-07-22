# DataGrid Port - Session 79

Date: 2026-06-13

## Goal

Link the upstream WPF `DataGridCell.cs` as a partial file, bringing in
`ColumnPropertyKey`/`ColumnProperty`, `IsEditingProperty`, `IsReadOnlyPropertyKey`/`IsReadOnlyProperty`
(guarded), `IsSelectedProperty`, `SyncIsSelected`, and the instance constructor — maximizing WPF code
reuse and reducing local shims.

## What Changed

### `ext/wpf/.../Controls/DataGridCell.cs` (upstream)

Added `partial` to the class declaration. Applied `#if !HAS_UNO` guards to all
code that references WPF-only APIs:

| Section | Guard |
|---|---|
| Static fields (`IsDataGridKeyboardSortDisabled`, `OptOutOfGridColumnResizeUsingKeyboard`) | `#if !HAS_UNO` |
| Static cctor body | `#if !HAS_UNO` |
| `#region Automation` (`OnCreateAutomationPeer`) | `#if !HAS_UNO` |
| `#region Cell Generation` (`PrepareCell`, `ClearCell`, `Tracker` property) | `#if !HAS_UNO` |
| `OnColumnChanged` body (style/IsReadOnly transfers) | `#if !HAS_UNO` |
| `#region Notification Propagation` | `#if !HAS_UNO` |
| `#region Style` (`OnCoerceStyle`) | `#if !HAS_UNO` |
| `#region Template` (`ChangeVisualState`, `UpdateVisualState`, `RemoveBindingExpressions`) | `#if !HAS_UNO` |
| `OnIsEditingChanged` body: keyboard-focus block, `UpdateVisualState()` call | `#if !HAS_UNO` |
| `NotifyCurrentCellContainerChanged` (0-arg WPF version) | `#if !HAS_UNO` |
| `IsCurrent` property (uses `dataGrid.IsCurrent(row, column)`) | `#if !HAS_UNO` |
| `IsReadOnlyPropertyKey`/`IsReadOnlyProperty`/`OnCoerceIsReadOnly` | `#if !HAS_UNO` |
| `OnAnyLostFocus`, `OnAnyGotFocus` | `#if !HAS_UNO` |
| `BeginEdit(RoutedEventArgs)`, `CancelEdit()`, `CommitEdit()` (WPF versions) | `#if !HAS_UNO` |
| `RaisePreparingCellForEdit`, `EditingElement` (WPF read-only property) | `#if !HAS_UNO` |
| `OnIsSelectedChanged` body: `CellIsSelectedChanged`, `RaiseSelectionChangedEvent`, `UpdateVisualState` | `#if !HAS_UNO` |
| `RaiseSelectionChangedEvent`, `SelectedEvent`, `UnselectedEvent`, `Selected`/`Unselected` events, `OnSelected`, `OnUnselected` | `#if !HAS_UNO` (AddHandler/RaiseEvent not on ContentControl) |
| `#region GridLines` (`MeasureOverride`, `ArrangeOverride`, `OnRender`) | `#if !HAS_UNO` |
| `#region Input` (all mouse/keyboard WPF input handlers) | `#if !HAS_UNO` |
| `#region Frozen Columns` (`OnCoerceClip`) | `#if !HAS_UNO` |
| `#region Helpers` (`DataGridOwner`, `ParentPanel`, `RowOwner`, `RowDataItem`, `CellsPresenter`, `NeedsVisualTree`) | `#if !HAS_UNO` (local provides these) |
| `#region Data` (`_owner`, `_tracker`, `_syncingIsSelected`, constants) | `#if !HAS_UNO` |

Active under HAS_UNO (WinUI-compatible):
- `ColumnPropertyKey`/`ColumnProperty` DP + `Column` CLR property (setter uses `ColumnProperty` in HAS_UNO)
- `IsEditingProperty` DP + `IsEditing` CLR property + `OnIsEditingChanged` (body partially guarded)
- `IsSelectedProperty` DP + `IsSelected` CLR property + `SyncIsSelected`
- Instance constructor: `_tracker = new ContainerTracking<DataGridCell>(this)` + HAS_UNO `RegisterPropertyChangedCallback` for cell-selection highlight
- `OnColumnChanged` (header only, body guarded)
- `OnIsSelectedChanged` (WinUI path: no-op body — selection handled by `RegisterPropertyChangedCallback`)

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridCell.cs` (local)

Rewritten to complement the newly-linked upstream:

**Removed** (now from upstream DP):
- `public bool IsEditing { get; set; }` → upstream `IsEditingProperty` DP
- `private bool _isSelected;` + manual `IsSelected` property → upstream `IsSelectedProperty` DP
- `public DataGridColumn? Column { get; set; }` → upstream `ColumnProperty` DP
- `internal void SyncIsSelected(bool) => IsSelected = isSelected;` → upstream version with `_syncingIsSelected`

**Added** (backing fields consumed by upstream):
- `private ContainerTracking<DataGridCell> _tracker;` — assigned in upstream instance ctor
- `private bool _syncingIsSelected;` — used by upstream `SyncIsSelected`

**Kept** (local provides these, upstream versions are guarded):
- `public bool IsReadOnly { get; set; }` (upstream `IsReadOnlyPropertyKey` guarded — coerce callback not portable)
- `internal DataGridRow? RowOwner { get; set; }` (upstream uses `_owner` field approach)
- `internal DataGrid? DataGridOwner => RowOwner?.DataGridOwner ?? Column?.DataGridOwner;`
- `internal object? RowDataItem => RowOwner?.Item;`
- `internal FrameworkElement? EditingElement { get; set; }` (upstream property is read-only and WPF-specific)
- `internal bool BeginEdit(RoutedEventArgs?)`, `CancelEdit()`, `CommitEdit()` — full shim editing logic
- `NotifyCurrentCellContainerChanged(DataGridCell?, DataGridCellInfo)` — different signature from upstream
- `BuildVisualTree()`, `ApplyShimGridLines()`, `NotifyPropertyChanged()`, etc.

**Fixed** (regressions from DP callback side-effects):
- `BuildVisualTree()`: added `|| IsEditing` early-return guard — prevents `OnIsEditingChanged` from overwriting the editing TextBox with the display TextBlock when `IsEditing` is set to `true` in `BeginEdit`

### `src/LeXtudio.Windows/LeXtudio.Windows.csproj`

Added `<Compile>` link:
```xml
<Compile Include="..\..\ext\wpf\...\DataGridCell.cs"
         Link="System.Windows.Controls\DataGridCell.upstream.cs" />
```

## Key Design Decisions

1. **`ColumnPropertyKey` setter in HAS_UNO**: `SetValue(DependencyPropertyKey, object)` is an extension method on `DependencyObject` via C# 14 `extension` blocks, but isn't found when called from within `DataGridCell` (WinUI `ContentControl` hierarchy). Changed HAS_UNO setter to use `SetValue(ColumnProperty, value)` directly.

2. **`IsReadOnlyPropertyKey` guarded entirely**: The coerce callback (`OnCoerceIsReadOnly`) uses `GetCoercedTransferPropertyValue` which is WPF-only, and the notify callback (`OnNotifyIsReadOnlyChanged`) is in the guarded Notification Propagation region. Keeping the whole block in `#if !HAS_UNO` is cleaner than providing a stub callback; local CLR `IsReadOnly { get; set; }` property suffices.

3. **`Selected`/`Unselected` events guarded entirely**: `DataGridCell` inherits from WinUI `ContentControl` (not the shim `System.Windows.Controls.Control`), so `AddHandler(System.Windows.RoutedEvent, Delegate)` and `RaiseEvent(RoutedEventArgs)` are not available. The `RegisterPropertyChangedCallback` in the upstream ctor handles cell-selection highlights.

4. **`CellIsSelectedChanged` guarded in HAS_UNO**: In the shim, row-level selection doesn't route through `cell.IsSelected = ...`, so this notification path is unnecessary and would throw `DataGrid_CannotSelectCell` when `SelectionUnit == Row`.

5. **`BuildVisualTree()` IsEditing guard**: `OnIsEditingChanged` calls `BuildVisualTree()` (active in HAS_UNO). Local `BeginEdit` sets `Content = _editingBox` then `IsEditing = true`, causing `BuildVisualTree()` to overwrite the TextBox. Fixed by returning early in `BuildVisualTree()` when `IsEditing == true`.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/                     # Passed: 136
dotnet run   src/LeXtudio.Windows.Sample/ -- --probe         # DONE failures=0
```
