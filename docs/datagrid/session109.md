# Session 109

Date: 2026-06-14

## Goal

Begin option B (port `VirtualizingStackPanel`) chosen after session 108. This
session is the probe/feasibility assessment, which surfaced a decisive escalation
of the cost.

## Findings

### Size and structure

`VirtualizingStackPanel.cs` is **13,052 lines** — among the largest WPF files —
declaring `VirtualizingStackPanel : VirtualizingPanel, IScrollInfo, IStackMeasure`.
Its regions include a full `IScrollInfo` implementation, a ~5,000-line
`MeasureOverride Helpers` region, delayed-cleanup virtualization, hierarchical
virtualization, and a ~1,000-line `ScrollTracer` diagnostic subsystem. It depends
on `MS.Internal.Controls`, `MS.Utility`, and `IStackMeasure`.

### Decisive blocker: it overrides WPF `Panel` items-hosting internals

Even the small shared base, `VirtualizingPanel.cs` (533 lines), cannot be linked:
it declares

- `internal override void GenerateChildren()`
- `internal override bool OnItemsChangedInternal(...)`
- `internal override void OnClearChildrenInternal()`

These **override WPF `Panel`'s internal items-hosting hooks**. In this project,
`Panel` resolves to **WinUI `Microsoft.UI.Xaml.Controls.Panel`** (there is no WPF
`Panel` shim), which has none of those virtuals. So upstream `VirtualizingPanel`
— and therefore `VirtualizingStackPanel` and `DataGridRowsPresenter`'s generation
— is built on the WPF `Panel`/`ItemsControl` generation substrate that WinUI
replaces with its own, incompatible system.

### Implication

Option B is not "port one 13k-line file." It is **reimplement the WPF
`Panel`/`ItemsControl` items-hosting core on WinUI** (a WPF `Panel` base with
`GenerateChildren`/generator-host wiring, plus the `ItemsControl` pipeline from
the session-108 finding), and only then the 13k-line virtualization engine on
top. This is an open-ended foundational sub-project — far larger than the DataGrid
control-root link — with substantial regression risk to a 100+-session-green
baseline, to replace a shim render path that already produces correct,
probe-verified output.

## Recommendation (firm)

Stop the live-host swap and adopt **option C**: keep the shim render path as the
live host and treat the linked presenters/`DataGridCellsPanel` as compiled
behavioral substrate (exercised where reachable). A/B do not pay for themselves:
the deliverable would be behavior identical to today's working grid, bought with a
foundational reimplementation of WinUI-incompatible WPF infrastructure.

The genuinely valuable reuse work on this control is essentially complete: the
control root, columns, presenters, cells panel, and most of `DataGridHelper` are
linked upstream; the remaining local pieces are deliberate Uno divergences
(`TransferProperty`/width/frozen logic, `FindParent`) dictated by the WinUI
hosting model.

## Verification

No source changes this session (assessment only); baseline unchanged
(build 129 warnings/0 errors, 136 tests, probe `DONE failures=0`).

## Next blocker

User decision: accept C (stop; pick a new track or pause), or explicitly commit
to the open-ended WPF items-hosting reimplementation despite identical end-user
behavior.
