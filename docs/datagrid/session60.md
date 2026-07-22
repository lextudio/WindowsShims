# DataGrid Port - Session 60

Date: 2026-06-13

## Goal

Batch session: reuse the remaining concrete columns from WPF — replace the local
`DataGridCheckBoxColumn` (105 lines), `DataGridComboBoxColumn` (324 lines), and
`DataGridTemplateColumn` (130 lines) shims with the linked upstream files.

## Result

All three linked; the local shims are deleted. Combined with session 59 (Text)
and 58 (Bound), **every DataGrid column class now reuses the real WPF body**;
only the minimal base `DataGridColumn` and `DataGridColumnCollection` remain
local shims.

## Root unlock: TwoWay-by-default binding bridge

The decisive blocker was write-back. The local CheckBox/Combo shims did *manual*
reflection write-back because WinUI bindings default to OneWay, while WPF's
`IsChecked`/`SelectedValue`/`Text`/`TextBox.Text` are `BindsTwoWayByDefault`.
Rather than per-column hacks, fixed it once at the binding layer:
`BindingOperations.SetBinding` now promotes a `Default`-mode binding to `TwoWay`
when the target is a known two-way-by-default property (`TextBox.Text`,
`ToggleButton.IsChecked`, `Selector.SelectedValue/SelectedItem`,
`ComboBox.Text`). This mirrors WPF metadata and makes `ApplyBinding` write-back
work for every editable column — present and future. **Verified**: both the
checkbox-toggle and combo-selection probe steps write back to the POCO.

## Gaps resolved (each verified by rebuild)

Shimmed (clean, reusable surface):
- `KeyStates` enum, `KeyEventArgs.KeyStates`/`SystemKey`, `Key.System` — column
  `OnInput` edit-trigger logic.
- `UIElement.InputHitTest` extension (returns null → no hit).
- `KeyboardNavigation.IsTabStopProperty`, `SystemResourceKey.DataGridComboBox…StyleKey`.
- `FrameworkContentElement.Style`.
- Base `DataGridColumn.CanUserSortProperty` + `OnCoerceCanUserSort` (Template needs them).

Fork-guarded under `#if HAS_UNO` (WinUI sealed types / no equivalent):
- CheckBox `DefaultElementStyle`/`DefaultEditingElementStyle` getters →
  `new Style(typeof(CheckBox))` (theme lookup, `Style(type, basedOn)` ctor, and
  `UIElement.FocusableProperty` are unavailable; matches the old local behavior).
- Combo `CancelCellEdit`/`CommitCellEdit` `EditableTextBoxSite` branches (no WinUI
  equivalent; body only no-op FlowDirection caching).
- Combo `ItemsSource`/`DisplayMemberPath`/`SelectedValuePath` DPs: `AddOwner` of the
  WinUI `ComboBox`/`Selector` DPs threw `InvalidCastException` when set on a column
  (not an ItemsControl); re-`Register`ed as independent DPs (SyncColumnProperty
  copies them onto the generated ComboBox).
- The 5 Font-DP callback casts pattern from session 59 (n/a here).

## Verification

```
dotnet build  → 0 errors
dotnet run … --probe  → DONE failures=0  (33 steps)
dotnet test  → 125 passed, 0 failed
```

Probe steps proving the reused bodies: `checkbox column renders + toggles write
back`, `combobox column renders + selection writes back` (both now via the
upstream `ApplyBinding` path + TwoWay bridge), plus all text/edit/sort steps.

The deleted local Combo shim is preserved at `/tmp/DataGridComboBoxColumn_local_backup.cs`
(and recoverable from git) in case a future regression needs the manual write-back.

## Net effect

- Deleted: 4 concrete column shims (~606 lines across sessions 59–60).
- Added: ~80 lines of reusable substrate (input args, helpers, binding bridge),
  most of it general WPF surface rather than DataGrid-specific.

## Next Batch

1. Consider linking the base `DataGridColumn.cs` (largest remaining column shim) —
   high value but the heaviest deps (headers, widths, display-index, freezing).
2. The Selector selection-engine reuse behind command routing.
3. `AlternatingRowBackground` / `AlternationCount` striping.
