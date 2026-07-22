# Session 107

Date: 2026-06-14

## Goal

Catalog session (no code changes) for the live-host milestone: map the current
shim render path against the WPF item-hosting target, and lay out an
incremental, probe-gated rung sequence with its risks. This de-risks the largest
remaining DataGrid milestone before any structural change.

## Current live render path (shim)

- **Template**: a code-built `ControlTemplate` (via `XamlReader`, in
  `DataGrid.cs`) whose body is `Border > ScrollViewer > StackPanel
  x:Name=PART_ShimRowsHost`.
- **Render**: `DataGrid.BuildShimVisualTree()` runs on apply-template and on every
  Items/Columns change. It clears `PART_ShimRowsHost`, then:
  - `BuildHeaderRow()` — manually creates a header row (not the linked
    `DataGridColumnHeadersPresenter`).
  - one `DataGridRow` per item via `row.PrepareRow(item, this)`, manually applying
    style/background/selection and `ItemContainerGenerator.RegisterContainer(item,
    row)`.
  - each `DataGridRow` uses its own code-built template (`PART_CellsHost`
    StackPanel) and builds its cells directly.
- **Generator**: `ItemContainerGenerator` is a **manual registry**
  (`RegisterContainer`/`ResetContainers`) on the live path. Its WPF-shaped
  sequential surface (`GenerateNext` → `CreateContainerForItem`) exists
  (sessions 99-101) but is **not** what the live render uses.

## WPF item-hosting target

- **Template**: `ItemsPresenter`/`PART_RowsPresenter` hosting a
  `DataGridRowsPresenter` (linked, session 92) as the items host
  (`VirtualizingPanel`).
- **Row generation**: `ItemContainerGenerator.GenerateNext` →
  `DataGrid.GetContainerForItemOverride()` (linked, returns `DataGridRow`) →
  `PrepareContainerForItemOverride`. The rows presenter realizes containers
  through the generator, not a manual loop.
- **Cell layout**: each `DataGridRow` template hosts a `DataGridCellsPresenter`
  whose items host is the now-fully-linked `DataGridCellsPanel` (session 101),
  which measures/arranges/generates cells via its generator.
- **Headers**: `DataGridColumnHeadersPresenter` (linked, session 94) as the live
  header host.

## Gap analysis / rung sequence (proposed, probe-gated, behind a flag)

The shim path must stay the default until the WPF path reaches probe parity, so
the recommended approach is a build/runtime flag selecting host strategy, flipped
only when green.

1. **R1 — Rows-presenter host**: add a WPF-shaped template variant with
   `PART_RowsPresenter` → `DataGridRowsPresenter`; confirm it attaches as
   `InternalItemsHost` and the generator is wired to the panel.
2. **R2 — Drive generation**: make the live path realize `DataGridRow`s through
   `ItemContainerGenerator.GenerateNext` → `GetContainerForItemOverride` instead
   of `RegisterContainer`. Verify Uno actually invokes the generator/panel
   callbacks (the key unknown — Uno's items pipeline differs from WPF's).
3. **R3 — Cells via presenter/panel**: row template hosts `DataGridCellsPresenter`
   + `DataGridCellsPanel`; cells generated/measured by the linked panel
   (`MeasureOverride`/`ArrangeOverride`).
4. **R4 — Headers**: `DataGridColumnHeadersPresenter` replaces `BuildHeaderRow`.
5. **R5 — Reconcile behavior**: re-home selection, editing, placeholder/add-new,
   styles, grid lines, sorting visuals, row details onto the generated
   containers (today all live inside `BuildShimVisualTree`).
6. **R6 — Scroll/measure**: ensure measure/arrange + `BringIndexIntoView`/
   `IScrollInfo` work (session 101 deferred the scroll plumbing).

## Risks

- **Uno generation pipeline**: the biggest unknown is whether Uno drives
  `ItemContainerGenerator`/`VirtualizingPanel` callbacks the way the linked WPF
  presenters expect. If not, R2/R3 may need a bridge or stay shim-driven.
- **Probe surface**: ~48 probe steps assert selection, editing, validation,
  sorting, widths (49/60/357px), styles, grid lines, headers, reorder,
  reactivity. Every rung must keep these green — hence the flag + parity gate.
- **Scope**: this is multi-session; each rung is its own session with the probe
  as the gate. Do not flip the default host until full parity.

## Verification

No source changes this session (catalog only); build/tests/probe unchanged from
session 106 (build 129 warnings/0 errors, 136 tests, probe `DONE failures=0`).

## Next blocker

Begin **R1**: author the WPF-shaped `PART_RowsPresenter`/`DataGridRowsPresenter`
template variant behind a flag and confirm host attachment + generator wiring,
with the shim path still default.
