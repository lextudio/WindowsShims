# Session 104

Date: 2026-06-14

## Goal

Continue the `DataGridHelper` link (sessions 102-103) by migrating the
`TreeHasFocusAndTabStop` tree helper and the Other Helpers region to the linked
upstream source.

## What changed

### `WinUIFrameworkElementExtensions.cs`

- Moved the `Focusable` shim extension from `FrameworkElement` to `UIElement`
  (WinUI has no `Focusable`; the shim reports `true`). `.Focusable` is used only
  by `DataGridHelper`, so there is no ambiguity, and `FrameworkElement` still
  inherits it. This lets the linked upstream `TreeHasFocusAndTabStop` resolve
  `uielement.Focusable` for a `UIElement`.

### `DataGridHelper_upstream.cs` (linked, finer fork-guards)

- **Now from upstream (un-guarded):**
  - `TreeHasFocusAndTabStop` — the `UIElement` branch compiles via the moved
    `Focusable` extension and `KeyboardNavigation.GetIsTabStop`; only the
    `ContentElement` else-branch stays inline `#if !HAS_UNO` (the local
    `ContentElement` stub is not a `DependencyObject`).
  - `AreRowHeadersVisible`, `HasNonEscapeCharacters`, `IsImeProcessed`, and the
    `_escapeChar` const (Other Helpers) — pure logic over the existing
    `TextCompositionEventArgs`/`KeyEventArgs`/`Key.ImeProcessed` shims.
- **Still guarded (kept local):**
  - `CoerceToMinMax` — upstream returns `NaN` for a `NaN` input; the local shim
    clamps `NaN` to `0` first, which matters for `Auto` column-width coercion.
  - `FindParent`, `OnColumnWidthChanged`, and the Property Helpers / Binding
    engine regions, as before.

### `DataGridHelper.cs` (local shim)

- Removed `TreeHasFocusAndTabStop`, `HasNonEscapeCharacters`, `IsImeProcessed`,
  and the `_escapeChar` const (now upstream). Kept `CoerceToMinMax`,
  `FindParent`, `OnColumnWidthChanged`, and the engine-coupled shims.

## Why this rung

Another set of helpers becomes real WPF source. `TreeHasFocusAndTabStop` retires
the local copy added in session 101; behavior is unchanged on Uno (every
`UIElement` is reported focusable and tab-stoppable by default, as the shim did).
The `CoerceToMinMax` NaN-clamp difference is called out and deliberately kept
local until column-width coercion is reconciled.

## Verification

- `dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly`
- `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/ --nologo -v:minimal`
- `dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe`

Results:

- Build succeeded (129 warnings, 0 errors)
- Tests passed: 136 / 136
- Probe passed with `DONE failures=0`

## Next blocker

Remaining `DataGridHelper` members under `#if !HAS_UNO`:

- `FindParent<T>` — reconcile the templated-parent walk (upstream) with the
  visual-tree walk the local `DataGridRowHeader.ParentRow` needs.
- `CoerceToMinMax` — decide whether the NaN→0 clamp is needed once width
  coercion runs through real WPF.
- `OnColumnWidthChanged` — carries real Uno render behavior.
- Property Helpers / Binding — the WPF property-transfer/coercion engine and
  binding-expression internals; the hard, last blocker.
