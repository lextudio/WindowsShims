# DataGrid Port - Session 63

Date: 2026-06-13

## Goal

Continue the "larger batch" cleanup by reducing the remaining column-base shim.
After sessions 58-60 linked every concrete/bound column body, the largest local
column holdout was `DataGridColumn.cs`.

## What Changed

- Linked upstream WPF `DataGridColumn.cs` into `LeXtudio.Windows.csproj`.
- Guarded the upstream class declaration with `#if HAS_UNO` so it can be
  composed with the local partial.
- Reduced local `DataGridColumn.cs` to Uno-only bridge helpers:
  `BuildCellContent`, `BuildEditingCellContent`,
  `SetValue(DependencyPropertyKey, object?)`, and a no-op `CoerceValue`.
- Removed the obsolete `DataGridBoundColumn.CoerceValue` local override and kept
  `BindingPath` plus `EffectiveSortMemberPath` in the bound-column partial.
- Added narrow compatibility contracts needed by the upstream base:
  `DataGridHelper` transfer/update/validation helpers,
  `DataGridColumnCollection` width-invalidation stubs, and
  `DoubleUtil.LessThan`.
- Added a minimal `DataGridHyperlinkColumn` placeholder so upstream
  `CreateDefaultColumn` compiles for `Uri` properties; real hyperlink
  navigation remains deferred.

## Runtime Fix

The probe caught one regression: header sorting stopped deriving the property
name from a bound column's binding path. WPF normally does this through
`SortMemberPath` coercion, but the Uno DP shim does not run those callbacks.

The fix is intentionally narrow: upstream `DataGrid.DefaultSort` now has a
`HAS_UNO` fallback to `DataGridBoundColumn.EffectiveSortMemberPath`, preserving
linked WPF sort ownership while restoring the existing binding-path behavior.

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --framework net10.0-desktop --no-restore
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --no-restore
dotnet run --project src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:

- Build: 0 errors (existing warnings only).
- Tests: 126 passed, 0 failed.
- Probe: `DONE failures=0`.

## Next Batch

1. `DataGridColumnCollection` width/display-index reuse. The current bridge
   still owns width invalidation/recompute stubs; linking or extracting more of
   the upstream collection logic is the next largest column-area cleanup.
2. If collection reuse proves too coupled to WPF layout internals, batch the
   next row/cell cleanup instead: upstream `DataGridCell` edit/validation
   members and `DataGridRow` container state, while preserving the existing
   shim visual tree.
