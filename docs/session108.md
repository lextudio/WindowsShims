# Session 108

Date: 2026-06-14

## Goal

Begin R1 of the live-host milestone (session 107): stand up a
`PART_RowsPresenter` → `DataGridRowsPresenter` host behind a flag and confirm
host attachment + generator wiring. This session is the R1 **investigation**,
which surfaced two decisive architectural blockers.

## Findings (decisive)

### 1. No WinUI items-generation pipeline

The shim `ItemsControl` derives from `Microsoft.UI.Xaml.Controls.Control`, **not**
WinUI `ItemsControl` (see `ItemsControlSpine.cs` / `ContextMenuShims.cs`).
`ItemsSource` and `ItemsPanel` are plain stored DPs with no backing machinery.
Consequently:

- There is no `ItemsPresenter` realizing a panel, no automatic container
  generation, and nothing ever sets `Panel.IsItemsHost = true`.
- `DataGridRowsPresenter.OnIsItemsHostChanged` (which sets
  `DataGrid.InternalItemsHost = this`) is therefore never triggered by a
  pipeline. The presenter can only become the host via **manual attachment**
  (instantiate it, add to the tree, set `IsItemsHost = true` ourselves).

### 2. No row-generation engine

`DataGridRowsPresenter.MeasureOverride` delegates generation to
`base.MeasureOverride` — the WPF `VirtualizingPanel` / `VirtualizingStackPanel`
engine. In the shim, `VirtualizingPanel` (`VirtualizingPanelStubs.cs`) is a stub
with **no `MeasureOverride` and no generation**. So even if the presenter is
manually attached and WinUI calls its `MeasureOverride`, **no rows are
generated**.

(The cell side is fine: `DataGridCellsPanel` has its own fully-linked
measure/generate path from session 101. The asymmetry is that the rows presenter
relies on the un-ported virtualizing-panel base.)

## Implication for the milestone

The session-107 plan assumed the linked presenters could be wired as the live
host. In reality the milestone is blocked at two foundational, still-stubbed
layers — the items pipeline and the row-virtualization engine — so "wire the WPF
presenters as the live host" actually means **building a row item-hosting/
generation driver** (or porting `VirtualizingStackPanel`), on top of the manual
attachment. That is several large sub-milestones, not the modest R1 envisioned.

## Options

- **A — Hand-built row driver.** Manually attach `DataGridRowsPresenter`, set
  `IsItemsHost`, and drive row generation through `ItemContainerGenerator`
  ourselves (rather than via `VirtualizingPanel.MeasureOverride`); let WinUI
  layout call the linked `DataGridCellsPanel` measure for cells. Large, but skips
  porting `VirtualizingStackPanel`.
- **B — Port `VirtualizingPanel`/`VirtualizingStackPanel` generation.** Bring the
  real WPF row-virtualization engine in so the presenter generates as upstream
  intends. Very large.
- **C — Keep the shim render path as the pragmatic live host.** It works (probe
  green, ~48 assertions). Treat the linked presenters/panel as compiled
  behavioral substrate, exercised where reachable, and stop here on the
  live-host swap. Lowest risk; accepts that the on-screen host stays shim-driven.

## Recommendation

Given the cost (two foundational stubs to replace) versus the benefit (the shim
render path already produces correct, probe-verified output), **C** is the
pragmatic choice unless there is a specific need for true WPF virtualization.
A/B are viable but each is a multi-session effort with real regression risk to a
100+-session-green baseline.

## Verification

No source changes this session (investigation only); baseline unchanged from
session 106/107 (build 129 warnings/0 errors, 136 tests, probe `DONE failures=0`).

## Next blocker

Decision required: pursue A (hand-built row driver), B (port virtualizing
panel), or C (stop — shim stays the live host). Each A/B rung must remain
probe-gated and flag-guarded with the shim path default until parity.
