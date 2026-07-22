# DataGrid Port - Session 86

Date: 2026-06-13

## Goal

Remove at least one remaining `#if !HAS_UNO` from a core DataGrid type while
preserving the current shim behavior.

## Changes

### Guard removed in `DataGridColumnHeader.cs`

Removed the guard around `OnCreateAutomationPeer()`.

### Shim additions

#### `AutomationPeer.cs`

Changed the WPF automation peer shim to derive from
`Microsoft.UI.Xaml.Automation.Peers.AutomationPeer`. This makes WPF peer
return types compatible with WinUI's `OnCreateAutomationPeer` override.

#### `DataGridColumnHeaderAutomationPeer.cs`

Added a minimal `DataGridColumnHeaderAutomationPeer` shim that stores the
owning header. Automation listener checks still return false, so automation
event paths remain effectively unreachable on Uno.

## Guard counts after session 86

| File | `#if` count |
|---|---:|
| `DataGridColumnHeader.cs` | 11 |
| `DataGridRow.cs` | 19 |
| `DataGridCell.cs` | 20 |

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj
dotnet test src/LeXtudio.Windows.Tests/
dotnet run --project src/LeXtudio.Windows.Sample/ --framework net10.0-desktop -- --probe
```

Results:
- Build: 0 errors
- Tests: Passed 136 / Failed 0
- Probe: `DONE failures=0`
