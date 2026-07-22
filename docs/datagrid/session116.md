# Session 116

Date: 2026-06-24

## Goal

Plan and start a stronger DataGrid test strategy for the Roma-driven WPF DataGrid
port. Coverage should be split between:

1. Fast WindowsShims unit tests that pin API/bridge contracts without requiring a
   live Uno UI runtime.
2. Roma DevFlow integration tests that exercise the actual ILSpy metadata pane
   and its DataGrid-backed workflows inside the app process.

## Test Strategy

### Layer 1: WindowsShims unit tests

Use these for:

- WPF upstream source-link contracts.
- Shim bridge surfaces that must remain present.
- Filter object behavior.
- Reflection-level regression guards for paths that cannot instantiate `DataGrid`
  in the headless desktop runner because Uno dispatcher initialization is required.

Good candidates:

- `DataGridExtensions` filter types and state shape.
- `ShimDataTemplate` and row-details factory handoff.
- `DataGridHelper.TransferProperty` bridge surfaces.
- `DataGridRow.BuildRowDetails` / selector-only row details support.
- DataGrid cell style application surface, including the template-skip path.
- Column width and filter-row synchronization entry points.

### Layer 2: Roma DevFlow integration tests

Use these for real app acceptance:

- Open an assembly and navigate to metadata nodes.
- Confirm metadata `View()` renders a `DataGrid`.
- Confirm generated columns exist and row counts are non-zero.
- Exercise text/regex, hex, and flags filters through the real metadata table.
- Exercise sort, row selection, and context menu/token navigation flows.
- Exercise row details for:
  - `CustomDebugInformationTableTreeNode`
  - `CoffHeaderTreeNode`
  - `OptionalHeaderTreeNode`
- Confirm nested row-details DataGrids render and have expected rows/columns.

The existing Roma integration suite already launches `Roma.Host` and drives stable
`roma.probe.*` DevFlow actions over port 9223. DataGrid coverage should extend that
stable probe surface rather than depend on older ad-hoc actions such as
`select-metadata-node`.

## Proposed Roma Probe Surface

Add stable Debug-only actions in `Roma.Host/Diagnostics/RomaIntegrationProbes.cs`:

| Action | Purpose |
|--------|---------|
| `roma.probe.metadata-open-table` | Open assembly, select a metadata tree/table node by name or handle kind, return DataGrid snapshot. |
| `roma.probe.metadata-grid-state` | Report active metadata DataGrid: row count, column headers, selected row, filter row present, details presenter count. |
| `roma.probe.metadata-filter` | Apply filter text to a column by header/key and return filtered row count. |
| `roma.probe.metadata-sort` | Sort a column and return first visible row summary before/after. |
| `roma.probe.metadata-select-row` | Select a metadata grid row and return selected item/token/detail visibility. |
| `roma.probe.metadata-row-details` | Select a row that has details and return details element type + nested grid row/column counts. |

Keep probe results JSON-shaped, small, and deterministic. Avoid visual text scraping where
possible; inspect `DataGrid.Items`, `Columns`, `SelectedItem`, `RowDetailsTemplateSelector`,
and realized row containers directly on the UI thread.

## First Execution

Added a new fast unit-test file:

### `src/LeXtudio.Windows.Tests/DataGridRomaMetadataSurfaceTests.cs`

Covered:

- `ShimDataTemplate` subclasses `Microsoft.UI.Xaml.DataTemplate` and exposes the
  expected factory signature.
- `DataGridDetailsPresenter` exposes `SetShimOwnerRow`, `EffectiveRow`, and
  `ShimContentFactory`.
- `DataGridHelper.TransferProperty` and row-details selector properties are present.
- `DataGridRow` has the private row-details build/visibility hooks needed for
  selector-only row details.
- `DataGridCell.ApplyShimCellStyle` and `ShimAppliedCellStyle` remain available.
- `DataGridFilter.State` preserves text separately from the active filter object.

Registered the file in `LeXtudio.Windows.Tests.csproj`.

## Verification

Command:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 146 passed
- 0 failed
- 0 skipped

One existing nullable warning remains in `DataGridCellInfoTests.cs`.

## Next Execution

Start with a single Roma DevFlow probe/test:

1. Add `roma.probe.metadata-open-table`.
2. Add one xUnit test that opens a stable runtime assembly and selects a known metadata table.
3. Assert:
   - active content contains a `System.Windows.Controls.DataGrid`
   - row count > 0
   - columns generated
   - filter row is enabled

After that first integration test is green, add filter/sort/row-details probes one by one.

## Follow-up Execution

Implemented the first Roma DevFlow metadata DataGrid probe:

### `Roma/src/Roma.Host/Diagnostics/RomaIntegrationProbes.cs`

- Added `roma.probe.metadata-open-table`.
- The probe:
  - opens the target assembly if needed
  - finds a metadata table node by `TableIndex`
  - invokes the real `OnTreeNodeSelected` path
  - inspects `_nodeContent.Content` as `System.Windows.Controls.DataGrid`
  - returns JSON with row count, column count, headers, `AutoGenerateColumns`, and
    DataGridExtensions auto-filter state

### `Roma/tests/Roma.IntegrationTests/RomaIntegrationTests.cs`

- Added `MetadataTable_RendersDataGrid`.
- Uses `typeof(System.Net.Http.HttpClient).Assembly.Location` so the test is not tied
  to a Windows runtime path.
- Verifies the `TypeDef` metadata table renders a real DataGrid with:
  - rows
  - generated columns
  - `AutoGenerateColumns=true`
  - auto-filter enabled
  - non-empty headers

### WindowsShims fix discovered by the probe

The first probe run showed:

```json
{"rows":438,"columns":0,"autoGenerateColumns":true,"autoFilterEnabled":true}
```

Root cause: upstream WPF `DataGrid.AddAutoColumns()` skips generation until
`MeasureOverride` clears `_measureNeverInvoked`. The WindowsShims render path is
hand-built and does not rely on WPF's presenter/measure pipeline, so metadata grids
could have items but no generated columns.

Changed the linked upstream `DataGrid.cs` under `#if HAS_UNO` so `AddAutoColumns()`
and `DeleteAutoColumns()` do not depend on `_measureNeverInvoked`.

## Follow-up Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 146 passed
- 0 failed
- 0 skipped

Roma:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter MetadataTable_RendersDataGrid
```

Result:

- Roma.Host build: 0 errors
- Integration test: 1 passed, 0 failed
