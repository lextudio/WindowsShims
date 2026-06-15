# Session 110

Date: 2026-06-14

## Goal

Scope/catalog (no code) the WinUI-native virtualization track chosen after
session 109: give the shim render host a WinUI virtualizing panel instead of
porting WPF `VirtualizingStackPanel`.

## Why WinUI-native (recap)

Porting WPF `VirtualizingStackPanel` (session 109) requires reimplementing the
WPF `Panel`/`ItemsControl` items-hosting core on WinUI for behavior WinUI already
provides. On Uno the idiomatic way to virtualize is to host the rows in a WinUI
virtualizing control (`ItemsRepeater`, or `ListView`'s `ItemsStackPanel`) that
realizes only visible containers. The existing `DataGridRow`/cell shim content and
behavior are kept; only the **host** changes.

## Current render host (recap)

`DataGrid.cs` builds a code `ControlTemplate`: `Border > ScrollViewer >
StackPanel x:Name=PART_ShimRowsHost`. `BuildShimVisualTree()` clears it and adds a
header row (child 0) plus one `DataGridRow` per item, calling `PrepareRow`,
building cells, `ItemContainerGenerator.RegisterContainer(item, row)`, and
applying style/selection — i.e. **every row is materialized**.

## Recommendation: `ItemsRepeater`

`ItemsRepeater` (Microsoft.UI.Xaml.Controls) is the better fit than `ListView`:
it virtualizes and recycles but adds no built-in selection/container chrome (the
shim already owns selection, cells, editing). It lives inside the existing
`ScrollViewer`. (R1 must confirm it is available in this Uno target.)

## Rung plan (probe-gated, behind a flag; shim render-all stays default)

1. **R1 — Template split + host.** Move the header row OUT of the scrolling area
   (a fixed header host above; standard DataGrid layout), and make the rows area
   an `ItemsRepeater` inside the `ScrollViewer`. Confirm `ItemsRepeater` is
   available and lays out under Uno. Flag off by default.
2. **R2 — Element factory + realize/recycle.** Feed the repeater `OrderedItems`;
   an element factory yields a `DataGridRow`. Move per-row setup
   (`PrepareRow`, cell build, container register, style) to `ElementPrepared`
   and teardown to `ElementClearing`.
3. **R3 — Generator registry vs partial realization.** Register only realized
   rows; ensure the linked/shim consumers of `ContainerFromItem` tolerate `null`
   for virtualized rows (most already null-check: editing, add/remove reactivity,
   current cell).
4. **R4 — Selection/editing/current-cell on realize.** Re-apply selection
   highlight and editing/current-cell state when a row is (re)realized, since
   recycled containers lose per-item visual state.
5. **R5 — Auto/Star width under partial realization.** Today
   `OnAutoWidthLayoutUpdated` measures all realized cells for a uniform per-column
   width. With virtualization only visible rows exist; recompute from header +
   visible cells (accept approximation, or measure-on-demand). Keep the probe's
   width assertions (49/60/357px) green.
6. **R6 — Feature parity + flip default.** Verify sorting, reactivity,
   placeholder/add-new, row details, row headers, grid lines, and all ~48 probe
   steps survive realize/recycle; then make the virtualizing host the default.

## Risks

- **Recycling state bleed**: a recycled `DataGridRow` must fully reset per-item
  state (selection, edit, validation, cell content) on `ElementPrepared`.
- **Width with partial realization** (R5) is the trickiest behavior change.
- **Probe**: most probe data sets are small enough that all rows fit the viewport
  (all realized), so virtualization may be transparent to most steps — but R5 and
  any "row grows host" assertions need attention.
- **Uno `ItemsRepeater` parity**: confirm scroll/measure/recycling behave under
  the Uno target (R1 gate).

## Verification

No source changes this session (catalog only); baseline unchanged
(build 129 warnings/0 errors, 136 tests, probe `DONE failures=0`).

## Next blocker

Begin **R1**: template split (fixed header + `ItemsRepeater` rows host) behind a
flag, confirm `ItemsRepeater` availability/layout under Uno, shim render-all
still default.
