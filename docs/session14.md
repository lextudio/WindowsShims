# DataGrid Port - Session 14

Date: 2026-06-10

## Goal

Price the selector spine (rung 4) with a probe of `Selector.cs` +
`MultiSelector.cs`, then land the rung-5/6 leaves from the session-12 catalog.

## What Changed

- Probe-linked `Selector.cs` and `MultiSelector.cs`: only 16 unique
  first-order unresolved contracts. The wall is the WPF data-engine internals
  (`BindingExpression`, `BindingExpressionUncommonField`,
  `DynamicValueConverter`, `MS.Internal.Data`, `MS.Internal.KnownBoxes`) plus
  seven missing virtuals on the shim `ItemsControl`, not raw `ItemsControl`
  member surface. Probe reverted; catalog recorded in DATAGRID.md.
- Linked six upstream files: `DragStartedEventArgs.cs`,
  `DragDeltaEventArgs.cs`, `DragCompletedEventArgs.cs` (with handler
  delegates), `ResourceKey.cs`, `ComponentResourceKey.cs`, and
  `ContainerTracking.cs`.
- Added supporting shims: a minimal `Thumb` shell carrying the three drag
  routed-event identities, `MarkupExtension`, `ComponentResourceKeyConverter`,
  and two SR strings (`ChangingTypeNotAllowed`, `ChangingIdNotAllowed`).
- Added local bridges: `MS.Internal.UncommonField<>`
  (`ConditionalWeakTable`-backed) and a `FocusNavigationDirection` enum shim.
- Added `DataGridSpineLeafTests` covering drag-args construction and routed
  event identity, `ComponentResourceKey` equality, `ContainerTracking`
  storage, enum order, and `UncommonField` argument validation.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 61 passed, 0 failed.

## Notes

The spine probe is encouraging but the 16-contract catalog hides cascaded
member errors. The binding-engine cluster (`BindingExpression`,
`DynamicValueConverter`, `MS.Internal.Data`) is the real decision point: WPF's
files are deeply coupled to the WPF property engine, so the realistic path is
narrow local bridges shaped like the session-3 `Binding` facade rather than
source links. The seven `ItemsControl` virtuals can be added to the shim
directly.

## Next Session

1. Add the seven missing virtuals to the shim `ItemsControl` and bridge the
   small spine leaves (`KnownBoxes`, `AttachedPropertyBrowsableForChildren`,
   `BuildInfo`).
2. Decide the `BindingExpression`/`MS.Internal.Data` strategy: minimal local
   bridge types shaped like WPF (likely) versus deferring the
   `SelectedValue`/value-binding paths with guarded stubs.
3. Re-probe the spine after those land to expose the next error layer, then
   attempt the real `Selector.cs` + `MultiSelector.cs` link.
