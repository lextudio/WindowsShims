# DataGrid Port - Session 23

Date: 2026-06-12

## Goal

Stand up the runtime sample head that gates DataGrid behavior work (ladder
step 15) and characterize what the linked control root does at runtime.

## What Changed

- New sample head `src/LeXtudio.Windows.Sample` (Uno.Sdk, net10.0-desktop,
  Skia desktop hosts), added to `WindowsShims.slnx`:
  - `Program.cs`: standard `UnoPlatformHostBuilder` bootstrap with a
    `--probe` flag that runs the probe and exits with 0/1 (headless gate).
  - `App.cs`: code-only application; merges `XamlControlsResources`; in
    probe mode enqueues an exit after the first layout pass.
  - `MainPage.cs`: step-by-step runtime probe over the linked WPF
    `DataGrid` — each step isolated, reported to the console (`[probe] …`)
    and to an on-screen pass/fail list next to the hosted grid.
- Note for app heads: inside a `LeXtudio.Windows.*` namespace,
  `Windows.Foundation.*` must be written `global::Windows.Foundation.*`
  (the `LeXtudio.Windows` prefix captures the lookup).

## Probe Results

All steps pass on net10.0-desktop (macOS Skia host):

| Step | Result |
| --- | --- |
| construct DataGrid (static + instance ctor) | ok |
| add explicit text columns (3× `DataGridTextColumn` with bindings) | ok |
| populate `Items` directly (3 records) | ok |
| read selection/command surface (`SelectedCells`, `DeleteCommand`, `SelectAllCommand`) | ok |
| toggle public options (`CanUserAddRows`, `CanUserDeleteRows`, `AutoGenerateColumns`, `FrozenColumnCount`) | ok |
| attach to visual tree (Border child) | ok |
| measure pass (800×600) | ok |

The WPF static constructor — dozens of DP registrations, class command
bindings, class event handlers through the shim `CommandManager`/
`EventManager` — runs to completion on Uno, as does the instance
constructor and the column-collection wiring.

Characterization of the inert control after layout:

- `DesiredSize = 0,0`, visual children = 0 — the shim `ItemsControl` derives
  from `Microsoft.UI.Xaml.FrameworkElement` (not `Control`), so there is no
  template pipeline and nothing renders.
- `ItemContainerGenerator.ContainerFromIndex(0)` = null — no row container
  generation.

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
cd WindowsShims/src/LeXtudio.Windows.Sample && dotnet run --framework net10.0-desktop -- --probe
```

Result: build succeeded; tests passed with 104 passed, 0 failed; probe
exited 0 with all steps ok.

## Notes

The compile-and-survive milestone holds at runtime, which de-risks the link:
no shim stub throws on the construct/populate/measure path. The two missing
pillars are now precisely identified and ordered:

1. **Row container generation** — `ContainerFromItemInfo`/
   `ItemContainerGenerator` must produce `DataGridRow` instances for items
   (the upstream control root already calls `PrepareRow` on them).
2. **A visual pipeline** — either rebase the shim `ItemsControl` onto a
   WinUI `Control`/`ItemsControl` so templates apply, or give the shim a
   code-built default visual (column headers presenter + rows presenter)
   measured/arranged manually. The rebase direction needs a probe of its
   own: the WinUI `Control` base brings name clashes (`IsEnabled`,
   `Template`, focus members) that the spine currently defines itself.

## Next Session

1. Probe the `ItemsControl` rebase onto a WinUI base type (likely
   `Microsoft.UI.Xaml.Controls.Control`) and catalog the member clashes; the
   alternative is a code-built visual tree inside the shim.
2. Implement minimal row generation: `ContainerFromItemInfo` returning real
   `DataGridRow`s, wired through `PrepareRow`, tracked by
   `ContainerTracking`.
3. Extend the sample probe to assert the first visible artifact (non-zero
   DesiredSize, a generated row) and keep `--probe` as the headless gate.
