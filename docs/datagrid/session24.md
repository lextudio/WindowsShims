# DataGrid Port - Session 24

Date: 2026-06-12

## Goal

Decide the visual-pipeline direction (ladder step 15, first rung): probe
whether the shim `ItemsControl` can rebase from `Microsoft.UI.Xaml.
FrameworkElement` onto a WinUI `Control` base — unlocking the template
pipeline — versus hand-building a visual tree in code.

## Outcome

The rebase onto WinUI `Control` is **landed**. It was nearly free: clean
compile, all tests green, runtime probe unchanged. The "code-built visual
tree" alternative is no longer needed.

## What Changed

- `ContextMenuShims.cs`: the foundational `public partial class ItemsControl`
  base changed from `Microsoft.UI.Xaml.FrameworkElement` to
  `Microsoft.UI.Xaml.Controls.Control`. Because the chain is `DataGrid
  (linked) → MultiSelector (linked) → Selector (linked) → ItemsControl
  (shim)`, this single change rebases the entire control tower onto a
  templated WinUI control.
- `ItemsControlSpine.cs`: removed the spine's now-redundant `IsEnabled`/
  `IsEnabledProperty`, `DefaultStyleKeyProperty`, and `IsTabStopProperty`
  declarations — these were added in earlier sessions only because the
  `FrameworkElement` base lacked them. They now come from WinUI `Control`,
  and `DataGrid`'s static-ctor `OverrideMetadata` calls resolve to the
  Control DPs through the existing no-op shim extension. `IsEnabled` is now
  the *real* WinUI property, so the WPF logic reads true control state.
- `DataGridControlRootLinkTests.cs`: two regression guards —
  `ItemsControlSpineRebasesOntoWinUiControl` (the tower derives from WinUI
  `Control`) and `IsEnabledComesFromWinUiControlAfterRebase` (IsEnabled is
  inherited, not re-declared on the shim).

## Probe: the clash catalog

Changed the base to `Control`, rebuilt, and read the compiler output:

- **0 errors.**
- **4 hiding warnings (CS0108)**, all on the spine's redundant DPs that
  WinUI `Control`/`UIElement` already define: `IsEnabledProperty`,
  `IsEnabled`, `DefaultStyleKeyProperty`, `IsTabStopProperty`. Removing the
  four spine declarations cleared every one.
- The `ItemContainerStyle`/`ItemsPanel`/`ItemsSource`/`AlternationCount`/
  `IsTextSearchEnabled`/`ItemBindingGroup` DPs on the spine do **not** clash
  — they are `ItemsControl`-level concepts and the base is `Control`, not
  WinUI `ItemsControl`.

The earlier session-23 worry (that the `Control` base would bring `IsEnabled`/
`Template`/focus-member clashes requiring its own probe) turned out to be a
4-line cleanup, not a blocker.

## Verification

```bash
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; tests passed with 106 passed, 0 failed (104 prior +
2 rebase guards); probe steps all ok; reported base is now
`Microsoft.UI.Xaml.Controls.Control`.

## Notes

- The control still renders nothing (`DesiredSize=0,0`, zero visual
  children) because no default style/template is applied yet — but that is
  now reachable through the standard WinUI mechanism (`DefaultStyleKey` +
  `Template` + `OnApplyTemplate`/`GetTemplateChild`) rather than a bespoke
  hand-built tree.
- `DefaultStyleKeyProperty.OverrideMetadata(…, typeof(DataGrid))` in the
  WPF static ctor is a no-op under the shim, so `DefaultStyleKey` is not
  actually set to `typeof(DataGrid)` yet. The template rung must set
  `DefaultStyleKey` explicitly (in code or via a provided style) so WinUI
  can resolve a default template.

## Next Session

1. Give the DataGrid a minimal default template so something renders: set
   `DefaultStyleKey`/`Template` and host a rows panel + column-headers
   presenter. Decide between a code-built `ControlTemplate` in
   `OnApplyTemplate` and a XAML default style in a resource dictionary.
2. Implement minimal row container generation:
   `ItemContainerGenerator.ContainerFromIndex`/`ContainerFromItemInfo`
   returning real `DataGridRow` instances wired through `PrepareRow` and
   tracked by `ContainerTracking`.
3. Extend the sample probe to assert the first visible artifact (non-zero
   `DesiredSize`, at least one generated `DataGridRow`).
