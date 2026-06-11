# DataGrid Port - Session 10

Date: 2026-06-10

## Goal

Probe `DataGridComboBoxColumn` and decide whether its binding/item-source
surface can be local-shelled without bringing in selector item-container
behavior.

## What Changed

- Confirmed the combo box column does not need selector item-container
  porting: Uno `ComboBox` already exposes `ItemsSource`, `DisplayMemberPath`,
  `SelectedValuePath`, `SelectedItem`, `SelectedValue`, and `Text`.
- Added a local `DataGridComboBoxColumn` shell deriving from `DataGridColumn`
  (matching WPF inheritance) with `SelectedItemBinding`,
  `SelectedValueBinding`, and `TextBinding` CLR properties and the WPF
  effective-binding precedence.
- Wired effective binding into `ClipboardContentBinding` fallback and one-way
  read-only coercion, mirroring the bound-column behavior.
- Added `ItemsSource`/`DisplayMemberPath`/`SelectedValuePath` and
  element/editing style dependency properties; display and editing generation
  both create a Uno `ComboBox` with bindings and column properties applied.
- Added `RefreshCellContent` handling that rebinds or re-syncs the matching
  combo box property on column property changes.
- Added `DataGridComboBoxColumnTests` reflection coverage and included it in
  the explicit test project compile list.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 34 passed, 0 failed.

## Notes

The upstream `DataGridComboBoxColumn` remains deferred as a direct source
link. Its deferred behavior hangs off the missing owner/edit pipeline:
`TextBlockComboBox` styling through `ComponentResourceKey`, `OnInput`
drop-down opening (F4/Alt+Up/Alt+Down), flow-direction cache/restore on
cancel/commit, and `SortMemberPath` coercion from the effective binding
(current WPF-style coercion shims are no-ops).

## Next Session

1. Probe `DataGridHyperlinkColumn` to catalog how much navigation/routed
   command surface it actually needs; decide between a local shell over Uno
   `HyperlinkButton` and continued deferral.
2. If hyperlink stays blocked, start the control-shell milestone (session
   ladder step 11): decide between a linked `DataGrid.cs` with guarded
   internals and a short-lived local shell over Uno `ListView`/`Grid`.
3. Row/edit event args remain queued behind `DataGridRow` and the edit
   pipeline.
