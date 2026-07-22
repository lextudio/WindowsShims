# Session 123

## DataGrid test consolidation and Fluent theme port

This session consolidated the three previously drifting DataGrid surfaces:

- src/LeXtudio.Windows.Sample remains the manual scenario gallery.
- tests/DataGrid.IntegrationTestHost remains the thin DevFlow host.
- tests/DataGrid.IntegrationTests remains the xUnit/DevFlow driver.
- New tests/DataGrid.TestScenarios owns the shared scenario factories used by both apps.

The integration-test project now builds the host explicitly before running. This
avoids an invalid cross-TFM ProjectReference while keeping one command as the entry
point for the whole suite.

## Grid-line rendering investigation

The initial line-alignment probe was invalid: cells in the DevFlow host measured
only 1px high because the runtime DataGridCell template's ContentPresenter did not
explicitly bind Content. The probe could report matching coordinates while testing
a collapsed visual.

The corrected implementation and probes established the actual causes:

1. DataGridCell now binds its content explicitly, so text participates in measure.
2. The row-header host and DataGridRowHeader stretch vertically, making the row
   header, row, and cells share one bottom coordinate.
3. The complete four-sided DataGrid frame is an overlay instead of a wrapping
   border. A wrapping border reserves an inner layout pixel; an overlay lets the
   final visual row and the frame use the same bottom coordinate.
4. The extra row at the bottom is WPF's default NewItemPlaceholder, not unused
   padding. The scroll-bottom regression checks that visual row.

DevFlow actions datagrid.probe.row-line-metrics and
datagrid.probe.scroll-to-bottom cover ordinary line alignment and the placeholder
row overlapping the complete bottom frame at maximum scroll offset.

## Fluent visual layer

The existing Generic.xaml and runtime templates were deliberately minimal and used
WPF's black default grid-line brushes. WCT v7's DataGrid.xaml provides a good Fluent
visual language, but its templates target different controls, presenters, template
parts, dependency properties, and scroll-bar ownership, so they cannot be copied
wholesale.

The port reuses WCT visual tokens and metrics while preserving the shim structure:

- Added DataGridFluentTheme. It resolves current WinUI theme resources first and
  uses WCT-derived neutral/accent fallbacks when a key is unavailable.
- DataGrid background, outer border, grid lines, row backgrounds, alternating rows,
  text, hover, and selection now use Fluent brushes.
- Cell, column-header, and row-header minimum heights are 32px.
- Cells use 14px text and 12px horizontal padding.
- Column headers use 12px semibold secondary text and Fluent hover/pressed fills.
- Both manual and virtualized templates retain the exact overlay-frame geometry and
  bind chrome through DataGrid DPs, so user-provided brushes still work.
- Generic.xaml now matches the runtime template's overlay-frame structure.

This is intentionally a theme port, not a second DataGrid implementation. Existing
WPF editing, grouping, filtering, frozen-column, row-details, and virtualization
behavior remains owned by the current shim.

## DevFlow verification

Added datagrid.probe.fluent-theme. Its integration test verifies that outer and
horizontal/vertical grid-line brushes are not opaque black, header and body fills
are distinct, and cell/header minimum heights are at least 32px. The line-coordinate
regressions remain in the same suite, preventing appearance work from breaking the
bottom-line geometry.

Final DevFlow integration result after the scenario follow-up: 34 passed, 1 skipped,
0 failed. The skipped
frozen-column vertical-scroll case was pre-existing and remains unrelated to the
theme port.

## Follow-up: representative gallery scenarios and selection contrast

The first shared gallery still exposed implementation-shaped sample data. It was
reworked into domain scenarios without changing the APIs under test:

- Row Details is now a customer-order table whose details contain nested product
  line items. The outer and nested grids disable new-item placeholders.
- Variable Height is now a service-health list where one incident expands a real
  150px operational-context panel. Routine checks stay at normal row height, and
  the grid disables new-item placeholders.
- Grouped is now a 32-person directory across four countries with role and office
  columns, rather than two rows in one group. Expanding the data exposed and fixed
  a DevFlow probe that incorrectly assumed the selector's last invocation must be
  the first group.
- Frozen Edit is now an inventory sheet with SKU and Product frozen by default,
  followed by editable Warehouse and On hand columns across 40 rows. Existing
  DevFlow coverage verifies horizontal frozen coordinates, edit/commit, and resize.

Selection contrast was also corrected. WCT v7 paints SystemAccentColor through a
separate translucent selection layer; it does not replace the row surface with an
opaque accent brush or switch all foregrounds to white. The first port incorrectly
resolved AccentFillColorSecondaryBrush directly, which can be an opaque dark blue.
DataGridFluentTheme now extracts SystemAccentColor and creates a fixed low-alpha
tint, preserving readable themed foreground text and row-header glyphs. The new
datagrid.probe.fluent-selection regression asserts that the live selected-row fill
is translucent and distinct from both foreground brushes.

## Follow-up: null RowDetails templates rendered model type names

The Sample's manual Variable Height path exposed rows containing the text
DataGrid.TestScenarios.DataGridScenarios+VariableHeightRow. DevFlow inspection of
the shared scenario identified a row-details lifecycle bug: with
RowDetailsVisibilityMode.Visible, a selector returning null still left a visible
DataGridDetailsPresenter whose Content was the row item. WinUI then rendered the
item through ToString(), exposing its fully-qualified model type.

DataGridRow.BuildRowDetails now treats a null selector result as no details for that
row: it collapses the details host and clears the presenter/content. This is fixed in
the control rather than hidden with a model ToString override. New manual-path
DevFlow actions create the same non-virtualized grid used by the Sample and count
details/model-object content. Direct verification returned 40 rows, exactly 1 real
details row, 0 model-object content rows, and 0 matching type-name TextBlocks in the
DevFlow UI tree. Final suite: 35 passed, 1 skipped, 0 failed.

The grouped gallery data was subsequently refined so all 32 people have unique
names. Each eight-person country group now uses locally appropriate names and city
offices: Canadian names/cities, German names with native spelling, Japanese names,
and US names/cities. GroupStyle coverage remains unchanged at four groups of eight.

## Files added

- src/LeXtudio.Windows/System.Windows/Controls/DataGridFluentTheme.cs
- tests/DataGrid.TestScenarios/DataGrid.TestScenarios.csproj
- tests/DataGrid.TestScenarios/DataGridScenarios.cs
- docs/session123.md

## Main files updated

- src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs
- src/LeXtudio.Windows/System.Windows/Controls/DataGridCell.cs
- src/LeXtudio.Windows/System.Windows/Controls/DataGridRow.cs
- src/LeXtudio.Windows/System.Windows/Controls/Primitives/DataGridColumnHeader.cs
- src/LeXtudio.Windows/System.Windows/Controls/Primitives/DataGridRowHeader.cs
- src/LeXtudio.Windows/Themes/Generic.xaml
- src/LeXtudio.Windows.Sample/MainPage.cs
- tests/DataGrid.IntegrationTestHost/MainPage.cs
- tests/DataGrid.IntegrationTestHost/ScenarioGallery.cs
- tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs
- tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj
- src/WindowsShims.slnx
