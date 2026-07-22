# Session 106

Date: 2026-06-14

## Goal

The "hard but worthy" session: bridge the WPF property-transfer/coercion engine
so the remaining `DataGridHelper` Property-Helpers region can link.

## Investigation (the important finding)

A real `CoerceValue(dp)` is mechanically feasible — `FrameworkPropertyMetadata`
already retains the `CoerceValueCallback`, and WinUI `dp.GetMetadata(type)`
returns that instance, so `CoerceValue` could look up and invoke it. **But
globally activating it is unsafe**, for two concrete reasons:

1. **Blast radius.** There are 21 active `CoerceValue(...)` call sites across the
   linked DataGrid code, coercing `Width`, `ActualWidth`, `IsFrozen`,
   `Visibility`, `CanUserAddRows`, `ItemContainerStyle`, `Clip`, etc. All are
   currently no-ops on Uno. A real `CoerceValue` would fire all 21 dormant
   `OnCoerce*` callbacks at once — several (width, frozen, placeholder
   visibility) duplicate and would fight the shim's hand-rolled equivalents that
   the probe asserts (49/60/357 px widths).
2. **Manual-visual wall.** Even scoped to the style/content transfer properties,
   the upstream flow is `TransferProperty` → `CoerceValue` → `OnCoerceCellStyle`
   → `GetCoercedTransferPropertyValue` → `SetValue(Style)`, after which **WPF
   applies the style**. The shim applies styles manually (`ApplyShimCellStyle`
   etc.) because Uno does not auto-apply setters to these controls. So a real
   coercion would set the DP but not produce the visual — the local
   `TransferProperty` exists precisely to do the visual application directly.

Conclusion: full functional activation is a large DP-driven-visual rewiring with
a 21-callback blast radius — not a safe incremental step. The safe, real progress
is to migrate the engine **computation** to the linked upstream now, leaving
`CoerceValue` a no-op and `TransferProperty` local, so the engine is genuine WPF
source ready for later **per-property** activation.

## What changed

### `DataGridHelper_upstream.cs`

- Un-guarded the transfer-engine computation: both `GetCoercedTransferProperty-
  Value` overloads, `IsPropertyTransferEnabled`, and the
  `_propertyTransferEnabledMap` field. These are now the real WPF source. They
  resolve over the session-105 `GetValueSource` bridge.
- Kept `TransferProperty` and its only helper `GetPropertyTransferEnabledMap-
  ForObject` under `#if !HAS_UNO` (the local partial owns `TransferProperty`,
  which drives the shim visuals directly; upstream's drives `CoerceValue`).
- The Binding region remains `#if !HAS_UNO`.

### `DataGridHelper.cs` (local shim)

- Removed the local `GetCoercedTransferPropertyValue` (both overloads, returned
  `baseValue`) and `IsPropertyTransferEnabled` (returned `true`). Behavior is
  unchanged: these are only reachable through the `OnCoerce*` callbacks, which
  never fire while `CoerceValue` is a no-op, and with the map empty the upstream
  `GetCoercedTransferPropertyValue` returns `baseValue` exactly as the stub did.
- Kept the local visual `TransferProperty`.

## Why this rung

The property-transfer engine is now the real WPF implementation rather than
stubs, correct and ready. The only thing standing between it and live operation
is a per-property `CoerceValue` activation plus DP-driven visual application —
deliberately deferred because doing it globally would destabilize the
100+-session-green width/frozen/style behavior.

## Verification

- `dotnet build … LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly`
- `dotnet test … LeXtudio.Windows.Tests/ --nologo -v:minimal`
- `dotnet run --project … LeXtudio.Windows.Sample … -- --probe`

Results:

- Build succeeded (129 warnings, 0 errors)
- Tests passed: 136 / 136
- Probe passed with `DONE failures=0`

## Next blocker

Per-property coercion activation, smallest-blast-radius first:

- Pick one transfer property (e.g. `DataGridColumn.CellStyle`), make
  `CoerceValue` invoke its callback for that property only, and wire the
  resulting `SetValue` to the shim's visual apply (`ApplyShimCellStyle`) via a
  property-changed callback. Verify the style probe steps stay green, then widen
  one property at a time.
- The width/frozen coerce callbacks should stay dormant until the shim's parallel
  width logic is retired — otherwise they conflict.
