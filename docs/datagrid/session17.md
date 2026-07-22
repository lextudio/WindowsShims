# DataGrid Port - Session 17

Date: 2026-06-11

## Goal

Consolidate the duplicate `BindingExpressionBase` shims, rebase the local
`DataGrid` shell onto the linked `MultiSelector` spine, and re-probe upstream
`DataGrid.cs` for the control-root catalog.

## What Changed

- Consolidated `BindingExpressionBase`: deleted the `System.Windows`
  EarlyBatch copy that shadowed the `System.Windows.Data` bridge via
  enclosing-namespace lookup; the bridge base now carries a virtual `Value`
  (read by document cloning in linked `TextContainer.cs`) overridden by the
  untargeted expression. Reverted the session-16 fork qualification in
  `Selector.cs`, shrinking the fork diff. (The other apparent consumer,
  `TextEditorDragDrop.cs`, turned out to be in a dead `#if !HAS_UNO` branch.)
- Rebased the local `DataGrid` shell from the shim `Control` onto linked
  `MultiSelector`; removed the duplicated `Items`, `NewItemInfo`, and
  `ItemInfoFromIndex` members now inherited from the spine.
- Probe-linked upstream `DataGrid.cs` (8,628 lines): 21 unique error sites,
  all concrete member-level errors (catalog in DATAGRID.md) — validation/
  binding types, header pair, `ItemNavigateArgs`, ten shim virtuals, two
  override-signature fork guards. Probe reverted.
- Added a rebase assertion to `SelectorSpineTests` (`DataGrid` is a
  `MultiSelector`; `Items` declared by the shim `ItemsControl`).

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 75 passed, 0 failed.

## Notes

The 21-site catalog has no error-type cascades behind it: the base chain
(`MultiSelector` → `Selector` → shim `ItemsControl`) fully resolves, so these
are the real remaining gaps. The control root is one enablement session away
if the validation/binding cluster is stubbed at the same fidelity as the
session-16 bridges.

## Next Session

1. Control-root enablement: WPF-shaped stubs for `ValidationRule`,
   `BindingGroup`, `IEditableCollectionView`, `PropertyGroupDescription`;
   minimal `DataGridColumnHeader`/`DataGridColumnHeadersPresenter` shells;
   `ItemNavigateArgs`; the ten shim virtuals; fork guards for
   `OnApplyTemplate` accessibility and `OnCreateAutomationPeer` return type.
   Then link `DataGrid.cs`, iterating on the member-level layer it reveals.
2. After the control root compiles: reconcile the local shell's
   `Columns`/`SelectedCells` surface with the upstream file (the local shell
   retires or becomes guarded partials).
3. Runtime sample with static items and explicit columns remains the
   milestone gate before behavior work (per the Test Plan).
