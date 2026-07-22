# DataGrid Port - Session 80

Date: 2026-06-13

## Goal

Begin the routed-event infrastructure bridge: create a shim
`System.Windows.Controls.ContentControl` that extends WinUI `ContentControl`
with the full WPF-compatible surface — routed events, focus helpers,
`DependencyPropertyKey` write path, and visual-state hooks — so that every
`ContentControl` subclass we port compiles upstream WPF source without
per-class workarounds or `#if !HAS_UNO` guards.

## Motivation

Session 79 added twelve `#if !HAS_UNO` guards to `DataGridCell.upstream.cs`
because `DataGridCell : ContentControl` (WinUI) lacked:
- `AddHandler(System.Windows.RoutedEvent, Delegate)` — WinUI's `AddHandler`
  takes `Microsoft.UI.Xaml.RoutedEvent`, not the shim type
- `RemoveHandler` — same issue
- `RaiseEvent(RoutedEventArgs)` — not on WinUI ContentControl at all
- `SetValue(DependencyPropertyKey, object?)` — WinUI's `SetValue` takes
  `DependencyProperty`, not a key

The root cause: `DataGridCell` inherited WinUI `ContentControl` directly,
bypassing the shim `Control` that already provides these APIs. The fix is to
interpose a shim `ContentControl` in the hierarchy — exactly what was done for
`Control` in earlier sessions.

## What Changed

### `src/LeXtudio.Windows/System.Windows/Controls/ContentControl.cs` (new)

```
System.Windows.Controls.ContentControl
    extends Microsoft.UI.Xaml.Controls.ContentControl
```

APIs provided (mirrors the shim `Control`):

| API | Notes |
|---|---|
| `RaiseEvent(RoutedEventArgs)` | delegates to `WinUIDependencyObjectExtensions.RaiseEvent` |
| `AddHandler(RoutedEvent, Delegate)` | delegates to `WinUIDependencyObjectExtensions.AddHandler` |
| `RemoveHandler(RoutedEvent, Delegate)` | delegates to `WinUIDependencyObjectExtensions.RemoveHandler` |
| `SetValue(DependencyPropertyKey, object?)` | delegates to `SetValue(key.DependencyProperty, value)` |
| `Focus()` | `Focus(FocusState.Programmatic)` |
| `MoveFocus(TraversalRequest)` | no-op returning false |
| `BringIntoView()` | `StartBringIntoView()` |
| `IsVisible` | `Visibility == Visibility.Visible` |
| `IsKeyboardFocusWithin` | stub returning false |
| `IsKeyboardFocused` | stub returning false |
| `AddLogicalChild` / `RemoveLogicalChild` | no-ops |
| `CoerceValue(DependencyProperty)` | no-op |
| `UpdateVisualState()` / `UpdateVisualState(bool)` | delegates to `ChangeVisualState` |
| `ChangeVisualState(bool)` | virtual no-op; subclasses override |

Affected subclasses (all now inherit the WPF surface automatically):
- `DataGridCell`
- `DataGridDetailsPresenter`
- `DataGridRowHeader`
- `GroupItem`
- `ButtonBase` (and `DataGridColumnHeader` through it)

### `ext/wpf/.../Controls/DataGridCell.cs` (upstream guards removed)

Two guards eliminated by the new shim `ContentControl`:

1. **`ColumnPropertyKey` setter** — `SetValue(ColumnPropertyKey, value)` now
   compiles because `ContentControl.SetValue(DependencyPropertyKey, ...)` is
   in scope. Removed the `#if HAS_UNO / #else / #endif` around the setter.

2. **`Selected`/`Unselected` event region** — `AddHandler`, `RemoveHandler`,
   `RaiseEvent` are now available. Removed the `#if !HAS_UNO` guard around
   the entire `RaiseSelectionChangedEvent` / `SelectedEvent` / `UnselectedEvent`
   / `OnSelected` / `OnUnselected` block.

3. **`OnIsSelectedChanged`** — `RaiseSelectionChangedEvent` call restored to
   active; `CellIsSelectedChanged` and `UpdateVisualState` remain
   `#if !HAS_UNO` (selection routing and VSM still WPF-only).

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridCell.cs` (local)

Removed three methods that are now inherited from `ContentControl`:
- `public bool Focus()`
- `public bool MoveFocus(TraversalRequest)`
- `public bool IsVisible`

### `src/LeXtudio.Windows.Tests/DataGridBoundColumnTests.cs`

Qualified `ContentControl` reference to `System.Windows.Controls.ContentControl`
to resolve ambiguity with `Microsoft.UI.Xaml.Controls.ContentControl`.

## Guards Remaining in DataGridCell.upstream.cs

| Region | Why still guarded |
|---|---|
| Static cctor | `DefaultStyleKeyProperty.OverrideMetadata`, `EventManager.RegisterClassHandler` |
| `OnIsEditingChanged` keyboard block | `IsKeyboardFocusWithin` stub always returns false; safe but wrong long-term |
| `NotifyCurrentCellContainerChanged` (0-arg) | different signature from local version |
| `IsCurrent` | uses `dataGrid.IsCurrent(row, column)` — WPF-internal API |
| `IsReadOnlyPropertyKey` / `OnCoerceIsReadOnly` | `GetCoercedTransferPropertyValue` not yet shimmed |
| `OnAnyLostFocus` / `OnAnyGotFocus` | `owner.FocusedCell` — WPF internal DataGrid field |
| `BeginEdit` / `CancelEdit` / `CommitEdit` (WPF versions) | local provides richer implementations |
| `EditingElement` (WPF read-only property) | local provides `{ get; set; }` version |
| `CellIsSelectedChanged` call | would throw `DataGrid_CannotSelectCell` when `SelectionUnit==Row` |
| `UpdateVisualState` calls | VSM not yet wired |
| `#region GridLines` | WPF `DrawingContext` rendering |
| `#region Input` | WPF mouse/keyboard APIs |
| `#region Frozen Columns` | `GetFrozenClipForCell` — WPF geometry API |
| `#region Helpers` | local provides `DataGridOwner`, `RowOwner`, etc. |
| `#region Data` | local provides backing fields |

## Next Steps

The remaining guards fall into three categories:
1. **VSM** (`UpdateVisualState` calls) — wire `ChangeVisualState` to WinUI VSM
2. **Property coercion** (`GetCoercedTransferPropertyValue`) — implement the
   cascading coerce pattern; removes `IsReadOnlyPropertyKey` guard
3. **Structural** (Helpers, Data, GridLines, Input) — require deeper WPF
   container pattern work

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/                     # Passed: 136
dotnet run   src/LeXtudio.Windows.Sample/ -- --probe         # DONE failures=0
```
