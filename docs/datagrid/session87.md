# DataGrid Port - Session 87

Date: 2026-06-13

## Goal

Clean one core DataGrid type by removing every remaining `#if !HAS_UNO` from
the linked WPF source for that type.

## Changes

### `DataGridColumnHeader.cs` is now guard-free

Removed all remaining conditional blocks from the linked upstream
`DataGridColumnHeader.cs`, including:

- static metadata override registration
- resize gripper hookup and helper methods
- upstream `NotifyPropertyChanged`
- style/content/height coercion callbacks
- automation invoke notification
- frozen-column clip coercion
- header mouse routing/reorder helpers
- resize helper properties

The file now has no `#if`/`#endif` directives.

### Shim support added for upstream paths

Added or extended narrow compatibility surfaces required by the unguarded WPF
code:

- `FrameworkObject` logical-parent no-op shim
- `AutomationProperties.IsOffscreenBehaviorProperty` and
  `IsOffscreenBehavior`
- `UIElementAutomationPeer.CreatePeerForElement`
- `Thumb` drag/double-click event accessors
- `DataGridColumnCollection.OnColumnResizeStarted/Completed`
- `DataGridColumnHeadersPresenter` header mouse routing hooks
- column-header resource keys in `SystemResourceKey`
- `FocusableProperty` metadata identity for `ButtonBase`

Moved the local header notification behavior into `DataGridHelper` so the
upstream `NotifyPropertyChanged` path can be used. The frozen-column branch
also calls `ApplyShimFrozenState()` because `CoerceValue` is a no-op in the
Uno shim.

## Guard counts after session 87

| File | `#if` count |
|---|---:|
| `DataGridColumnHeader.cs` | 0 |
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
