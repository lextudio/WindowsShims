# Session 115

Date: 2026-06-24

## Goal

Port `MetadataTableViews.xaml` from WPF XAML to WinUI-compatible C#. The WPF file used
`{x:Type}`, `{x:Static}`, `BasedOn={StaticResource {x:Type ...}}`, and
`RelativeSource FindAncestor` — none of which WinUI's XAML parser supports. Rather than
translating to WinUI XAML, every resource is built programmatically and injected into
the inherited `ResourceDictionary`.

## New infrastructure

### `src/LeXtudio.Windows/System.Windows/Controls/ShimDataTemplate.cs`

New class `ShimDataTemplate : Microsoft.UI.Xaml.DataTemplate` (Uno's `DataTemplate` is
not sealed, so subclassing is valid). Holds a C# factory
`Func<object?, FrameworkElement?>` that `BuildRowDetails` calls directly instead of
using WinUI's `ContentTemplate` mechanism (which cannot handle WPF bindings or
WPF-authored templates).

### `DataGridDetailsPresenter` shim partial

Added `internal Func<object?, FrameworkElement?>? ShimContentFactory` property so the
factory can survive the `SyncProperties()` → `BuildRowDetails` hand-off.

## Changes

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridHelper.cs`

In `TransferProperty` for `DataGridDetailsPresenter`, the selector path now checks
whether the returned `DataTemplate` is a `ShimDataTemplate`. If so, the factory is
stored on the presenter (`details.ShimContentFactory`) instead of assigning to
`ContentTemplate` — this avoids letting WinUI try to "apply" a template that has no
valid WinUI content factory.

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridRow.cs` (`BuildRowDetails`)

After `presenter.SyncProperties()`, if `ShimContentFactory` is set:
1. Call `factory(Item)` to produce the UIElement directly.
2. Set `presenter.Content = element`.
3. Clear `ShimContentFactory`.

This path bypasses WinUI's `ContentTemplate` mechanism entirely.

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridCell.cs` (`ApplyShimCellStyle`)

Style setters are now applied to the cell via `SetValue(setter.Property, setter.Value)`.
The `Control.TemplateProperty` setter is skipped — the shim builds its own cell visual
tree and a WinUI `ControlTemplate` would break it.

### `Roma/src/Roma.Host/ILSpy/RomaMetadataStubs.cs`

Replaced the null-returning stub with a real implementation that populates the
`ResourceDictionary` in the constructor via `BuildResources()`:

| Key | Type | Notes |
|-----|------|-------|
| `DataGridCellStyle` | `Style` | `BorderThickness=0`, `Padding=2`, `VCA=Center` (no Template setter) |
| `DefaultFilter` | `FilterControlTemplate` | `Kind=Text` |
| `HexFilter` | `FilterControlTemplate` | `Kind=Hex` |
| `*Filter` (12 entries) | `FilterControlTemplate` | `Kind=Flags` + matching CLR enum type |
| `ItemContainerStyle` | `Style` | `HorizontalContentAlignment=Stretch` |
| `CustomDebugInformationDetailsDataGrid` | `ShimDataTemplate` | Nested `DataGrid` with `AutoGenerateColumns=True` wired to `Helpers` |
| `CustomDebugInformationDetailsTextBlob` | `ShimDataTemplate` | Read-only `TextBox` bound to `RowDetails.ToString()` |
| `HeaderFlagsDetailsDataGrid` | `ShimDataTemplate` | `DataGrid` with manual `Value`/`Meaning` columns for `IList<BitEntry>` |

## Verification

```bash
# WindowsShims unit tests
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
# → 140 passed, 0 failed

# Roma.Host builds clean
dotnet build Roma/src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
# → 0 errors
```

## Notes

- Filter templates (`DefaultFilter`, `HexFilter`, `*Filter`) return
  `FilterControlTemplate` subclasses that the shim's `BuildFilterRow` already knows
  how to interpret — these were always the intended type; `MetadataTableViews.xaml` was
  their source in WPF, now replaced by the C# stub.
- The WPF `ControlTemplate` setter in `DataGridCellStyle` is intentionally skipped: the
  shim's `DataGridCell` builds its visual tree from `BuildCells()` and setting a WinUI
  `ControlTemplate` would trigger `OnApplyTemplate()`, clearing those cells.
- `Entry` and `BitEntry` types are `internal` classes in the `ICSharpCode.ILSpy.Metadata`
  namespace, compiled into the same `Roma.Host` assembly — so the factory methods can
  reference them directly without reflection.
