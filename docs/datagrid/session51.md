# DataGrid Port - Session 51

Date: 2026-06-12

## Goal

Continue the reuse work toward **editing** (and **selection**) — but safely.
Deep investigation showed those reuses are materially harder than sorting, so
this session lands the shared, low-risk prerequisite both need: WPF-correct
**command routing**.

## Investigation (why not a full editing migration this session)

- The WPF edit flow runs through routed commands: `BeginEdit()`/`CommitEdit()`
  execute `BeginEditCommand`/`CommitEditCommand` against the **cell**, and the
  class command bindings are scoped to **DataGrid**. The shim's
  `CommandBinding.AppliesTo` only did a direct `IsInstanceOfType` check, so the
  command never reached `OnExecutedBeginEdit`/`OnExecutedCommitEdit` — **command
  routing was the first blocker.**
- `OnExecutedCommitEdit` is clean: it orchestrates exactly the pieces the shim
  already wired (`cell.CommitEdit`, `OnCellEditEnding`, `OnRowEditEnding`, row
  commit) — so commit reuse is viable once routing + `CurrentCell` exist.
- `OnExecutedBeginEdit` is **heavy**: it pulls in `EditableItems` /
  `NewItemPlaceholder` / `AddNewItem`, `_selectedCells` region ops, and
  `SetCurrentCellToNewItem` — substrate that is only thinly shimmed. Driving it
  now would risk the working editing. Recorded as the remaining gap.

## What Changed

- `CommandBinding.AppliesTo` now walks the visual tree from the target: a
  binding owned by an ancestor type (e.g. `DataGrid`) applies when the command
  is executed against a descendant (e.g. a `DataGridCell`) — matching WPF's
  class-command routing. Direct-type and null-target behavior unchanged.
- This is the routing substrate that both editing-command reuse and
  selection-command reuse depend on; it is correct on its own and changes no
  existing behavior (the only `Execute(_, target)` caller, RichTextBox, uses a
  direct-type target).

## Verification

Build succeeded; 122 passed/0 failed; probe `DONE failures=0`. Probe proves a
`DataGrid`-scoped `RoutedCommand` executed against a descendant cell routes to
its handler ("class command routed to descendant cell = True"). Unit test
`ClassCommandBindingMatchesByTargetType` covers the direct-match / no-tree
cases. All prior editing/sort/selection probes still pass (no regression).

## Notes / honest limits

- This session wires the routing *capability*; it does not yet reroute the
  cell edit input through `grid.BeginEdit()`/`CommitEdit()`. Doing so requires
  (a) setting `CurrentCellContainer` when a cell edits, and (b) bridging the
  begin-edit substrate (`EditableItems`/placeholder/selected-cell regions) or
  guarding those branches.
- Selection-command reuse (`Selector` engine) depends on the same generator /
  items-host / selected-cell-region substrate.

## Next Session (editing reuse, staged)

1. Wire `CurrentCellContainer` (set on cell edit start) and confirm
   `grid.CommitEdit()` routes to `OnExecutedCommitEdit` driving `cell.CommitEdit`.
2. De-duplicate orchestration: once the WPF commit flow raises
   `CellEditEnding`/`RowEditEnding`, remove the shim's own raises.
3. Guard/bridge `OnExecutedBeginEdit`'s new-item-placeholder + selected-cell
   substrate, then route begin-edit through the command.
