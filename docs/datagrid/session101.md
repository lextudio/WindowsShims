# Session 101

Date: 2026-06-14

## Goal

Stop hand-porting `DataGridCellsPanel` slices into a local `.uno.cs` partial and
instead **reuse the real WPF source**: remove the fork guard so the full
upstream `DataGridCellsPanel.cs` (measure, arrange, generation, virtualization,
frozen-column layout) compiles on `HAS_UNO`, and delete the duplicated local
implementation.

## Background

Sessions 96-100 grew a `DataGridCellsPanel.uno.cs` partial that re-implemented,
slice by slice, methods that already existed in the linked upstream
`DataGridCellsPanel.cs` under a single `#if HAS_UNO … #else <full body> #endif`
guard. On `HAS_UNO` only a tiny `InternalBringIndexIntoView` stub compiled; the
real body was dropped. Per direction to reuse WPF source and remove local-shim
duplication, this session inverts that approach.

## What changed

### `DataGridCellsPanel.cs` (linked upstream, fork-patched)

- Removed the outer `#if HAS_UNO / #else / #endif` so the entire upstream
  measure/arrange/generator/virtualization/frozen-column body compiles on
  `HAS_UNO`.
- Added four narrow `#if HAS_UNO` fork-patches for genuine WinUI platform gaps,
  each preserving the original WPF code under `#else`:
  - `ComputeCellsPanelHorizontalOffset`: `VisualTreeHelper.GetOffset(this).X`
    instead of `TransformToAncestor(scrollViewer).Transform(...)`.
  - `GetViewportWidth`: `ScrollContentPresenter.ActualWidth` instead of
    `.ViewportWidth`, and the `IHierarchicalVirtualizationAndScrollInfo`
    grouping branch guarded out.
  - `ArrangeChild`: `new Rect(0, 0, w, h)` instead of `new Rect(new Size(...))`
    (WinUI `Rect` has no `Size` constructor).
  - `BringIndexIntoView`: validates the index then defers to
    `base.BringIndexIntoView` on Uno; the `IScrollInfo.SetHorizontalOffset` /
    dispatcher-priority retry loop (and `IsChildInView`/`RetryBringIndexIntoView`
    helpers) stay WPF-only. Column scroll-into-view is the one behavior still
    deferred.

### Deleted

- `System.Windows/Controls/DataGridCellsPanel.uno.cs` — the entire sessions
  96-101 hand-port, now superseded by the linked upstream body.

### Narrow bridges (replacing the deleted local code)

- `VirtualizingPanel`: `ItemContainerGenerator` (panel-scoped generator from the
  items owner), `MeasureDirty` / `MeasureDuringArrange` flags (report clean —
  the Uno measure/arrange split does not re-enter measure). Removed the now-
  redundant base `IsVirtualizing` / `InRecyclingMode` instance properties (the
  linked panel declares its own, matching upstream), clearing two CS0108 hide
  warnings.
- `MS.Internal.DoubleUtil`: `AreClose(Size, Size)` overload for the
  `MeasureOverride` desired-size comparison.
- `DataGridHelper`: `TreeHasFocusAndTabStop` (focus-trail probe used by the
  linked `EnsureFocusTrail`); `KeyboardNavigation.GetIsTabStop` accessor over the
  existing attached DP. These remain local because the upstream `DataGridHelper`
  is not yet linked — a follow-up candidate.

## Why this rung

This converts ~750 lines of duplicated local panel logic into direct reuse of
the upstream WPF source, so the panel's measure/arrange/generation behavior is
now the real WPF implementation rather than a parallel re-port. The live
DataGrid template still uses the manual shim rows host, so `MeasureOverride` is
compiled and available but not yet the live layout path — wiring the WPF
item-hosted panel as the live host remains the next step.

## Verification

- `dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly`
- `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/ --nologo -v:minimal`
- `dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe`

Results:

- Build succeeded (129 warnings, down from 130; 0 errors)
- Tests passed: 136 / 136
- Probe passed with `DONE failures=0` (grid `DesiredSize` unchanged — no
  regression, since the live host is still the shim rows host)

## Next blocker

- Wire the WPF item-hosted `DataGridCellsPanel` / presenters as the live layout
  host (replace the manual `PART_ShimRowsHost` path) so the linked
  `MeasureOverride`/`ArrangeOverride` actually drive layout.
- Bridge the horizontal scroll host (`IScrollInfo.SetHorizontalOffset`,
  `ScrollContentPresenter` as `IScrollInfo`, dispatcher-priority retry) to
  un-defer `BringIndexIntoView`.
- Link upstream `DataGridHelper.cs` to retire the local `TreeHasFocusAndTabStop`
  shim.
