# DataGrid Port - Session 40

Date: 2026-06-12

## Goal

Multi-row selection (Ctrl toggle, Shift range) in `Extended` selection mode.

## What Changed

- `DataGrid` keeps a shim multi-selection set `_shimSelectedItems` plus an
  anchor `_shimAnchorItem`. `ShimSelectedItems` exposes it read-only.
- `HandleShimRowClicked(row, modifiers)`:
  - Extended + Ctrl → toggle the row's item in the set (becomes anchor).
  - Extended + Shift → range from anchor to clicked in display order
    (replaces the set).
  - else → single-select (replaces the set, sets anchor).
  - `_shimSelectedItem` tracks the primary; `ApplyRowSelectionVisuals`
    reflects set membership onto every generated row's `IsSelected`.
- `DataGridRow.OnPointerPressed` passes `e.KeyModifiers`.
- Rebuild retention re-applies `IsSelected` for all set members; the cleanup
  pass prunes items that left the collection and repoints the primary.
- Cell-mode selection clears the row set.
- Probe step (Ctrl adds → 2; Shift ranges r1..r2; plain click resets → 1);
  tests `MultiSelectSurfaceExists` and a fix to `ShimSelectionSurfaceExists`
  for the now-overloaded `HandleShimRowClicked`. 118 tests; 21 probe steps;
  failures=0.

## Verification

Build succeeded; 118 passed/0 failed; probe `DONE failures=0` —
"after Ctrl: count=2 …", "after Shift: r0=False, r1=True, r2=True".

## Notes / honest limits

- Selection is shim-side: the WPF `Selector.SelectedItems` collection is not
  populated (consumers read `ShimSelectedItems`); `SelectionChanged` not
  raised; `SelectedItem` only tracks the primary.
- `DataGridSelectionMode.Single` forces single-select (Ctrl/Shift ignored);
  no Ctrl+Shift combined semantics, no keyboard-driven range (Shift+Arrow).
- Anchor is the last Ctrl/plain click; WPF's richer anchor rules aren't
  replicated.

## Next Session

1. Shift+Arrow keyboard range selection; populate WPF `SelectedItems` if
   feasible (else keep `ShimSelectedItems`).
2. `Auto` column width (post-realization measure pass).
3. Commit-on-blur for editing; honor `DataGrid.IsReadOnly`.
