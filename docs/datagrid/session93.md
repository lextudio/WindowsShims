# DataGrid Port - Session 93

Goal: continue reducing DataGrid gaps without forcing the large
`DataGridCellsPanel` layout rewrite.

## Choice

The next small reuse target was the WPF column-header drag visual pair:

- `DataGridColumnDropSeparator`
- `DataGridColumnFloatingHeader`

They are dependencies of `DataGridColumnHeadersPresenter`. The current Uno
render path still uses shim-native header drag/reorder with a lightweight
`Border` drop indicator, but linking these types removes two concrete blockers
before a future `DataGridColumnHeadersPresenter` attempt.

## Changes

- Linked upstream WPF `DataGridColumnDropSeparator.cs`.
- Linked upstream WPF `DataGridColumnFloatingHeader.cs`.
- Marked both classes `partial`.
- Changed `DataGridColumnFloatingHeader.OnApplyTemplate` to `protected override`
  for WinUI compatibility.
- Qualified `BrushMappingMode.Absolute` to avoid ambiguity with WinUI's enum.
- Added a minimal `System.Windows.Media.VisualBrush` shim that is assignable to
  WinUI `Canvas.Background`.
- Added `System.Windows.Media.BrushMappingMode`.
- Added `VisualTreeHelper.GetOffset` compatibility returning a zero offset.

## Still Deferred

- The WPF floating header visual is only a compatibility shell; it does not
  clone rendered pixels under Uno.
- The current live reorder behavior still uses the shim-native pointer state
  machine and `Border` drop indicator.
- `DataGridColumnHeadersPresenter` remains the next header-side reuse target,
  but it is a larger architecture step because the current grid manually builds
  headers.

## Verification

```
dotnet build WindowsShims/src/LeXtudio.Windows/LeXtudio.Windows.csproj --nologo -v:minimal /clp:ErrorsOnly
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/ --nologo -v:minimal
dotnet run --project WindowsShims/src/LeXtudio.Windows.Sample/LeXtudio.Windows.Sample.csproj --framework net10.0-desktop -- --probe
```

Results:
- Build: 0 errors.
- Tests: 136/136 passed.
- Probe: `DONE failures=0`.
