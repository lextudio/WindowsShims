# Session 88

Goal: continue reducing local shims by cleaning the next core type.

## Core type cleaned

- Removed the remaining preprocessor conditionals from linked WPF `DataGridRow.cs`.
- Kept the upstream row API as the owner for lifecycle, notification, selection, editing, details, and cell lookup behavior.
- Reduced the local `DataGridRow` partial to shim-specific visual hosting and helpers:
  - direct row cell construction,
  - row details/header materialization,
  - row style/background cache helpers,
  - direct-hosted cell notification and lookup fallback.

## Compatibility added

- Added missing WPF metadata/control shims needed by unguarded `DataGridRow`:
  - row binding group metadata,
  - focus/mouse/snaps DPs,
  - virtualizing panel cache-size DP,
  - dispatcher call compatibility,
  - automation peer/property shells,
  - binding group validation stub.
- Wired upstream row notifications back into shim-hosted cells and row style refresh.
- Preserved active shim rows from WPF recycling cleanup so direct-hosted rows are not assigned `DisconnectedItem`.

## Verification

- `rg -n "^#" ext/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/DataGridRow.cs` returns no matches.
- `dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly` passes.
- `dotnet test src/LeXtudio.Windows.Tests/ --nologo -v:minimal` passes: 136/136.
- `dotnet run --project src/LeXtudio.Windows.Sample/ --framework net10.0-desktop -- --probe --no-build` passes: `DONE failures=0`.
