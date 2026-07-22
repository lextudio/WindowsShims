# DataGrid Port - Session 82

Date: 2026-06-13

## Goal

Reduce local shims and `#if !HAS_UNO` guards by enabling the WPF visual-state
machinery (`ChangeVisualState` / `UpdateVisualState`) to compile and run on the
Uno path, without reverting any previously enabled code.

## Changes

### Shim additions

#### `Control.cs`

| API | Notes |
|---|---|
| `UpdateVisualState()` / `UpdateVisualState(bool)` | Delegates to `ChangeVisualState`; same pattern as ContentControl |
| `IsKeyboardFocusWithin` | Stub returning false |
| `IsKeyboardFocused` | Stub returning false |
| `IsMouseOver` | Stub returning false (WinUI `IsPointerOver` is protected; wire up later) |

#### `ContentControl.cs`

| API | Notes |
|---|---|
| `IsMouseOver` | Stub returning false (same as Control) |

#### `DataGridHelperStubs.cs` — `VisualStates`

Added missing state constants:
- `StateCurrent = "Current"`, `StateRegular = "Regular"` — used by `DataGridCell.ChangeVisualState`
- Full `DATAGRIDROW_state*` set (11 constants) — used by `DataGridRow.ChangeVisualState`

#### `ItemsControlSpine.cs` — `ItemsControl`

Added `AlternationIndexProperty` (registered attached, default 0).
`DataGridRow.AlternationIndex` uses `AddOwner` on this property.

### Guards removed in `DataGridRow.upstream.cs`

| Region | Lines | What it enables |
|---|---|---|
| Constants block | 28-87 | Byte state codes + `IdealStateMapping` / `FallbackStateMapping` / `_stateNames` arrays |
| Visual States (`IsDataGridKeyboardFocusWithin` + `ChangeVisualState`) | 189-260 | Row VSM transitions now run on every `IsSelected`/`IsEditing`/`IsMouseOver` change |
| `AlternationIndex` property | 1001-1019 | Row alternation support; `ChangeVisualState` reads `AlternationIndex % 2` |
| `row.UpdateVisualState()` call | 1088 | Selection change now triggers VSM immediately |

### Guards split in `DataGridCell.upstream.cs`

The monolithic guard starting at line 325 was split:

- **Unguarded:** `ChangeVisualState(bool)` — cell VSM transitions now run (Selected,
  Current, Editing, Focus states)
- **Still guarded:** `BuildVisualTree()` — local provides a different version with an
  `|| IsEditing` early-return guard
- **Still guarded:** `RemoveBindingExpressions()` — depends on `BindingGroup` /
  `BindingExpressionBase.DisconnectedItem`

## Guard counts after session 82

| File | Guards before | Guards after |
|---|---|---|
| `DataGridRow.upstream.cs` | 23 | 19 |
| `DataGridCell.upstream.cs` | 16 | 15 |

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/                     # Passed: 136
dotnet run   src/LeXtudio.Windows.Sample/ -- --probe         # DONE failures=0
```
