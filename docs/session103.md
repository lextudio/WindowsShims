# Session 103

Date: 2026-06-14

## Goal

Continue the session-102 `DataGridHelper` link by migrating the next batch of
upstream members out of `#if !HAS_UNO`, taking the safe per-method wins from the
Tree Helpers and Cells Panel Helper regions.

## What changed

### `DataGridHelper_upstream.cs` (linked, finer fork-guards)

Replaced the single region-wide `#if !HAS_UNO` (Tree Helpers → end) with
per-method guards so individually-safe upstream methods compile on `HAS_UNO`:

- **Now from upstream (un-guarded):**
  - `FindVisualParent<T>` — byte-equivalent to the local shim.
  - `GetParentPanelForCell`, `GetFrozenClipForCell`,
    `GetParentCellsPanelHorizontalOffset(IProvideDataGridColumn)` — backed by the
    now-fully-linked `DataGridCellsPanel` (session 101). Behavior-equivalent to
    the previous `null`/`0` shims in practice: on Uno the panel's frozen-clip
    field stays null outside the (still-deferred) live arrange path, and
    `ComputeCellsPanelHorizontalOffset` resolves through the `VisualTreeHelper.
    GetOffset` stub.
- **Still guarded (kept local):**
  - `FindParent<T>` — upstream walks `TemplatedParent` with a
    `where T : FrameworkElement` constraint; the local version walks the visual
    tree with `where T : class` (used by `DataGridRowHeader.ParentRow`).
  - `TreeHasFocusAndTabStop` — needs a `UIElement.Focusable` bridge and has a
    `ContentElement` branch that does not type-check against the local
    `ContentElement` stub.
  - `OnColumnWidthChanged` — the local version carries real Uno render behavior.
  - Property Helpers / Binding / Other Helpers — the property-transfer/coercion
    engine, binding-expression internals, and flow-direction caching.

### `DataGridHelper.cs` (local shim)

- Removed `FindVisualParent`, `GetFrozenClipForCell`, and the
  `GetParentCellsPanelHorizontalOffset(IProvideDataGridColumn)` overload (now
  upstream). Kept the `DependencyObject` overload, `FindParent`,
  `TreeHasFocusAndTabStop`, and the engine-coupled shims.

## Why this rung

Each migrated method removes a local duplicate in favor of the real WPF source
with no behavior change, and the per-method guard structure makes the remaining
coupling explicit (exactly which members still need a substrate). The
cells-panel accessors are now genuine reuse, enabled by session 101 fully
linking the panel.

## Verification

- `dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly`
- `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/ --nologo -v:minimal`
- `dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe`

Results:

- Build succeeded (129 warnings, 0 errors)
- Tests passed: 136 / 136
- Probe passed with `DONE failures=0`

## Next blocker

- `TreeHasFocusAndTabStop`: add a `UIElement.Focusable` bridge (carefully, to
  avoid ambiguity with the existing `FrameworkElement.Focusable` extension) and
  guard only the `ContentElement` branch inline, then migrate it.
- `FindParent<T>`: reconcile the templated-parent vs visual-tree walk so the
  upstream version can serve `DataGridRowHeader.ParentRow`.
- Other Helpers (`AreRowHeadersVisible`, `CoerceToMinMax` — note the NaN-clamp
  difference, `HasNonEscapeCharacters`/`IsImeProcessed` + `_escapeChar`).
- Property Helpers / Binding remain the hard blocker (WPF property-transfer/
  coercion + binding-expression internals).
