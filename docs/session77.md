# DataGrid Port - Session 77

Date: 2026-06-13

## Goal

Promote `ButtonBase` from a static helper class to a proper abstract class
inheriting `ContentControl`, change `DataGridColumnHeader`'s base class from
`ContentControl` to `ButtonBase`, and link the upstream WPF
`DataGridColumnHeader.cs` as a partial file under `#if HAS_UNO` guards — all to
maximize WPF code reuse and shrink local shims.

## What Changed

### New file: `src/LeXtudio.Windows/System.Windows/Controls/ClickMode.cs`

Added `System.Windows.Controls.ClickMode` enum (`Release`, `Press`, `Hover`)
required by the new instance `ButtonBase`.

### `src/LeXtudio.Windows/System.Windows/Controls/Primitives/ButtonBase.cs`

Rewrote from `public static class ButtonBase` to
`public abstract partial class ButtonBase : ContentControl`.

- Registers `ClickModeProperty`, `CommandProperty`, `CommandParameterProperty`,
  `CommandTargetProperty` as real DPs.
- Exposes `protected virtual` WPF entry points: `OnClick`, `OnMouseLeftButtonDown`,
  `OnMouseMove`, `OnMouseLeftButtonUp`, `OnLostMouseCapture`.
- Bridges WinUI pointer events → WPF mouse virtuals:
  - `OnPointerPressed` → `OnMouseLeftButtonDown`; fires `OnClick` when
    `ClickMode == Press`.
  - `OnPointerReleased` → `OnMouseLeftButtonUp`; fires `OnClick` when
    `ClickMode == Release`.
  - `OnPointerMoved` → `OnMouseMove`.
  - `OnPointerCaptureLost` → `OnLostMouseCapture`.
- Stubs `IsMouseCaptured`, `CaptureMouse()`, `ReleaseMouseCapture()` (no-ops
  until full drag is ported).
- `internal virtual ChangeVisualState(bool)` stub for the upstream call in
  `DataGridColumnHeader`.

### `src/LeXtudio.Windows/System.Windows/Controls/Primitives/DataGridColumnHeader.cs`

Rewrote local partial to inherit `ButtonBase` instead of `ContentControl`.

- `Column` property wraps the upstream `_column` field with an `internal` setter
  so `BuildShimVisualTree` can wire column associations.
- `HasShimGridLine` computed property (for probe assertions on border thickness).
- `IsVisible` derived property.
- `ApplyShimFrozenState` uses `SetValue(IsFrozenPropertyKey.DependencyProperty, …)`
  to route through WinUI's `SetValue` that takes a `DependencyProperty`.
- Removed `OnPointerPressed` (now provided by `ButtonBase`).
- `NotifyPropertyChanged` overload kept local (upstream version guarded with
  `#if !HAS_UNO`).

### `ext/wpf/src/.../Primitives/DataGridColumnHeader.cs` (upstream)

Added `partial` keyword; applied `#if !HAS_UNO` guards to all code that
references WPF-only APIs:

| Section | Guard |
|---|---|
| Static cctor body | `#if !HAS_UNO` |
| `Column` getter (upstream reads from `_column` field) | `#if !HAS_UNO` |
| `NotifyPropertyChanged` 2-arg body | `#if !HAS_UNO` |
| `PrepareColumnHeader` | `#if !HAS_UNO` |
| `DisplayIndexPropertyKey`/`DisplayIndexProperty`/`DisplayIndex` getter | `#if !HAS_UNO` |
| `OnDisplayIndexChanged` | `#if !HAS_UNO` |
| `HookupGripperEvents()` call in `OnApplyTemplate` | `#if !HAS_UNO` |
| Gripper hookup, resize event handlers, `ColumnActualWidth` | `#if !HAS_UNO` |
| `OnCanUserResizeColumnsChanged` … `OnColumnVisibilityChanged` | `#if !HAS_UNO` |
| `#region Style and Template Coercion callbacks` | `#if !HAS_UNO` |
| `SortDirectionPropertyKey`/`SortDirectionProperty`/`SortDirection` getter | `#if !HAS_UNO` |
| `OnClick` automation block | `#if !HAS_UNO` |
| `OnCreateAutomationPeer` | `#if !HAS_UNO` |
| `OnCoerceClip` | `#if !HAS_UNO` |
| Mouse override methods (`OnMouseLeftButtonDown/Move/Up/LostMouseCapture`) | `#if !HAS_UNO` |
| `ColumnHeaderDropSeparatorStyleKey`, `ColumnFloatingHeaderStyleKey` | `#if !HAS_UNO` |
| `#region VSM` (`ChangeVisualState`) | `#if !HAS_UNO` |
| `ParentPanel`, `PreviousVisibleHeader` helpers | `#if !HAS_UNO` |
| `OnApplyTemplate` automation / template-transfer body | `#if !HAS_UNO` |
| `public override` → `protected override` for `OnApplyTemplate` | Fixed access modifier |

`CanUserSortPropertyKey`/`CanUserSortProperty`/`CanUserSort` and `IsFrozenPropertyKey`/
`IsFrozenProperty`/`IsFrozen` remain active under HAS_UNO (used by
`ApplyShimFrozenState` and `NotifyPropertyChanged`).

### `src/LeXtudio.Windows/LeXtudio.Windows.csproj`

Added `<Compile>` link pointing at the upstream `DataGridColumnHeader.cs` as
`System.Windows.Controls.Primitives\DataGridColumnHeader.upstream.cs`.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/                     # Passed: 136
dotnet run   src/LeXtudio.Windows.Sample/ -- --probe         # DONE failures=0
```
