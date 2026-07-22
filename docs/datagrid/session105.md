# Session 105

Date: 2026-06-14

## Goal

Begin bridging the WPF property-value-source substrate so the `DataGridHelper`
Property Helpers region can start linking. Concretely: make the existing
`DependencyPropertyHelper.GetValueSource` bridge accurate and migrate
`IsDefaultValue` to the linked upstream.

## Background

The Property Helpers / Binding regions of `DataGridHelper` are coupled to WPF's
value-precedence engine (`DependencyPropertyHelper.GetValueSource`,
`BaseValueSource`) and to coercion callbacks. Uno's `CoerceValue` is a no-op and
the bridge cannot observe styles/inheritance, so the full property-transfer
engine (`GetCoercedTransferPropertyValue`, `TransferProperty`,
`IsPropertyTransferEnabled`) stays local. But the **value-source query** itself
has a faithful Uno equivalent via `ReadLocalValue`, which unblocks the leaf
`IsDefaultValue`.

## What changed

### `PropertySystem.cs`

- Expanded `BaseValueSource` from a 2-member stub (`Local`, `Inherited`) to the
  full WPF enum (`Unknown … Local`, same member order/values), so precedence
  comparisons in linked code are meaningful and `Default` exists.
- Fixed `DependencyPropertyHelper.GetValueSource` (previously hardcoded to
  `Local`) to report `Default` vs `Local` from `ReadLocalValue` — the only two
  levels the Uno bridge can observe. This also makes the existing linked caller
  `DataGridRow.PersistAttachedItemValue` (BaseValueSource == Local) accurate
  rather than always-true.

### `DataGridHelper_upstream.cs`

- Un-guarded `IsDefaultValue` (it now resolves correctly through the fixed
  `GetValueSource`). The rest of the Property Helpers region — the
  property-transfer/coercion engine — stays `#if !HAS_UNO`.

### `DataGridHelper.cs` (local shim)

- Removed the local `IsDefaultValue` (was `ReadLocalValue == UnsetValue`,
  behavior-identical to the now-upstream version). The local `SyncColumnProperty`
  that calls it now resolves to the upstream method.

## Why this rung

This is the first real step into the property-engine substrate: it makes the
value-source bridge truthful and links the one Property Helpers member that
depends only on value-source (not on coercion callbacks). The transfer/coercion
methods remain blocked because Uno's `CoerceValue` does not invoke WPF coercion
delegates, and the local `TransferProperty` carries the real Uno render behavior.

## Verification

- `dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly`
- `dotnet test WindowsShims/src/LeXtudio.Windows.Tests/ --nologo -v:minimal`
- `dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe`

Results:

- Build succeeded (129 warnings, 0 errors)
- Tests passed: 136 / 136
- Probe passed with `DONE failures=0` (style/grid-line steps exercise
  `IsDefaultValue` via `SyncColumnProperty`; widths/`DesiredSize` unchanged)

## Next blocker

The property-transfer/coercion engine is the remaining hard substrate:

- Uno `CoerceValue` is a no-op, so the WPF `TransferProperty` →
  `CoerceValue` → `GetCoercedTransferPropertyValue` flow cannot drive the live
  render; the local `TransferProperty` reimplements that behavior directly and
  must stay until Uno coercion callbacks (or an equivalent) are bridged.
- `GetCoercedTransferPropertyValue` / `IsPropertyTransferEnabled` /
  `GetPropertyTransferEnabledMapForObject` could be migrated as dead-but-correct
  once the map semantics are confirmed harmless on Uno — a possible follow-up.
- The Binding region (`GetBindingExpression`, `ValidateWithoutUpdate`,
  `BindingExpressionBelongsToElement`, flow-direction caching) needs the WPF
  binding-expression internals.
