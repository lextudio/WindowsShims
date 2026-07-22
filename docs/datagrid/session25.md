# DataGrid Port - Session 25

Date: 2026-06-12

## Goal

First visible artifact (ladder step 15, second rung): give the rebased
DataGrid a template so it renders, and produce data rows from items +
columns.

## Outcome

**The DataGrid renders.** With three text columns and three items, the
sample probe now reports `PART_ShimRowsHost` populated with 4 children
(1 header row + 3 data rows) and `DesiredSize = 386×96` — up from the
session-23/24 `0×0`, zero children. Cell values come through the column's
real element-generation path, data-bound to each row item.

## What Changed

### Rendering mechanism

- The session-24 rebase onto WinUI `Control` unlocked templating, but the
  library's `Themes/Generic.xaml` is **not** in the consumer's ms-appx
  resource map (merging `ms-appx:///LeXtudio.Windows/Themes/Generic.xaml`
  throws `Cannot locate resource`). Rather than chase Uno control-library
  packaging, the shim assigns a minimal `ControlTemplate` **directly**, built
  at runtime via `Microsoft.UI.Xaml.Markup.XamlReader.Load` — self-contained,
  no default-style probing. The template root is a `Border > ScrollViewer >
  StackPanel x:Name="PART_ShimRowsHost"`.
- A `<Style TargetType="swc:DataGrid">` was also added to `Themes/Generic.xaml`
  for the day the library self-registers its dictionary; it is not the active
  path yet.

### Shim render path

- `DataGrid` (local partial): `EnsureShimStyleKey()` now assigns the
  code-built `ControlTemplate`; `BuildShimVisualTree()` populates
  `PART_ShimRowsHost` with a header row (column headers) and one cell-panel
  per item. Each visible row is backed by a logical `DataGridRow`
  (`PrepareRow(item, this)`); each cell is a real `DataGridCell` whose
  content is produced by the column.
- `DataGridColumn.BuildCellContent(cell, item)`: internal accessor over the
  `protected GenerateElement`, so the render path uses the column's real
  element generation (e.g. `DataGridTextColumn` → bound `TextBlock`).
- `DataGridCell.BuildVisualTree()`: sets the cell `DataContext` to the row
  item and its `Content` to the column-generated element, so the WinUI
  binding (`Binding "Name"` etc.) resolves against the item.
- Upstream `DataGrid.cs` (fork-guarded `#if HAS_UNO`): call
  `EnsureShimStyleKey()` at the end of the instance ctor; call
  `BuildShimVisualTree()` at the end of `OnApplyTemplate()`.

### Sample probe

- `MainPage` now verifies the **rendered** artifact after the grid's
  `Loaded` event (forcing `UpdateLayout`): asserts `PART_ShimRowsHost` has
  4 children and the grid has non-zero `DesiredSize`. A 15-second fallback
  guarantees the headless `--probe` run never hangs. `App` no longer
  force-exits early; `MainPage` owns the probe exit code.

### Tests

- `ShimRenderPathSurfaceExists` pins the render-path method surface
  (`BuildShimVisualTree`, `EnsureShimStyleKey`, `BuildCellContent`,
  `BuildVisualTree`). UI construction needs a dispatcher, so the runtime
  render gate is the sample probe, not unit tests.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; 107 tests passed, 0 failed; probe `DONE failures=0`
with `rows host children = 4` and `DesiredSize=386,96`.

## Notes / honest limits

This is the first-artifact rung; the render path is deliberately simple and
diverges from WPF's real pipeline:

- No virtualization, no `ItemContainerGenerator` containers, no
  `PART_RowsPresenter`/`ItemsHost` — the shim builds plain WinUI
  `StackPanel`s. `DataGridRow` is created as the logical container but is not
  the visual host (it has no template); cells live in a sibling panel.
- The render runs once in `OnApplyTemplate`. It does **not** yet react to
  later `Items`/`Columns` changes, selection, sorting, column resize, or
  editing.
- Column widths are a flat 120px fallback (or `ActualWidth` if set); no width
  computation.
- Header is plain text blocks, not `DataGridColumnHeader` controls.

## Next Session

1. Re-render on collection changes: hook `Items`/`Columns` change
   notifications to call `BuildShimVisualTree` (or incremental updates).
2. Promote rows to real container generation: have
   `ItemContainerGenerator`/`ContainerFromItemInfo` return the `DataGridRow`s
   the render path builds, and make `DataGridRow` host its own cells (give it
   a cells panel) so the WPF row/cell APIs line up with what is on screen.
3. Use `DataGridColumnHeader` for headers and begin honoring column `Width`
   (at least `Auto`/pixel) instead of the flat fallback.
