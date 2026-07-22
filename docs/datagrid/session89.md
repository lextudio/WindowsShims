# Session 89

Goal: reduce `#if !HAS_UNO` guard count in the linked WPF `DataGridCell.cs`.

## Changes made

### `DataGridCell.upstream.cs` guard count: 18 → 17

- **Removed `OnCreateAutomationPeer` guard** (was pair at old line 73-78):
  Added `DataGridCellAutomationPeer` shim at
  `src/LeXtudio.Windows/System.Windows/Automation/Peers/DataGridCellAutomationPeer.cs`
  so the upstream `OnCreateAutomationPeer()` override compiles under Uno.

- **Restructured `CellsPresenter` guard** (was standalone `#if HAS_UNO/#else/#endif` block, now an inline branch inside the getter):
  Consolidates the two-branch property into a single property body with an inline guard,
  which is more readable without reducing the pair count.

- **Data section restructured**:
  `_tracker` and `_syncingIsSelected` moved out of `#if !HAS_UNO` in the Data region
  (they are used by unguarded code: the instance ctor and `SyncIsSelected`).
  Removed both fields from the local `DataGridCell.cs` shim partial — now declared only in the upstream.
  Only `_owner`, `ColumnWidthStepSize`, and `ModifierMask` remain guarded.

- **`_syncingIsSelected` guard at OnIsSelectedChanged kept**:
  Attempted to unguard the `CellIsSelectedChanged` call (which uses `_syncingIsSelected`),
  but the shim's `IsUpdatingSelectedCells` bracket is not maintained tightly enough —
  programmatic `IsSelected` changes outside a proper update session cause
  `OnSelectedCellsChanged` to throw `DataGrid_CannotSelectCell` when `SelectionUnit=FullRow`.
  The guard is preserved to avoid the regression.

## Verification

- `rg -c "^#if|^#endif" ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/DataGridCell.cs` → 34 (17 pairs, was 18)
- `dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly` passes.
- `dotnet test src/LeXtudio.Windows.Tests/ --nologo -v:minimal` passes: 136/136.
- `dotnet run --project src/LeXtudio.Windows.Sample/ --framework net10.0-desktop -- --probe --no-build` passes: `DONE failures=0`.
