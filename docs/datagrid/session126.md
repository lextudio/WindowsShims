# Session 126 — DataGrid automation peers bridged onto Uno 6.6 UIA

Date: 2026-08-12

## Goal

todo.md item 1 (Accessibility / UI Automation, "fully inert — largest
remaining gap"): make the linked WPF DataGrid's ~36 `ListenerExists`-gated
automation call sites real by bridging onto Uno 6.6's native Skia
accessibility stack.

## Findings

- Uno 6.6 (source at /Users/lextm/uno-tools/uno) ships a complete UIA
  implementation:
  - `Microsoft.UI.Xaml.Automation.Peers.*`: real `AutomationPeer` hierarchy
    with Core virtuals, `AutomationControlType` (incl. DataGrid=28,
    DataItem=29, Header=34), `PatternInterface`, provider interfaces
    (`Microsoft.UI.Xaml.Automation.Provider.*`).
  - Skia bridge: `AccessibilityRouter` installs
    `AutomationPeer.AutomationPeerListener`; per-window
    `SkiaAccessibilityBase` subclasses translate peers/events to native AX
    (macOS: `MacOSAccessibility`, VoiceOver bridge building a native
    UNOAccessibilityElement tree; owners resolved via
    `SkiaAccessibilityBase.TryGetPeerOwner`).
  - `TryGetPeerOwner` only resolves owners for
    `FrameworkElementAutomationPeer` and Uno `ItemAutomationPeer` — a plain
    peer is dropped from native routing.
- WPF's own peer files cannot be linked: base `AutomationPeer.cs` lives in
  PresentationCore (2546 lines, COM/UIA interop); the linked WPF
  `OnCreateAutomationPeer` overrides are split between content elements
  (FrameworkContentElement — untouched) and UIElements (only 4 active in the
  shim build: DataGrid/Row/Cell/ColumnHeader).
- The shim's WPF-shaped `OnCreateAutomationPeer` on `Control` conflicts by
  name with Uno's virtual; resolved by changing its return type to Uno's
  `AutomationPeer` and relying on C# 9 covariant overrides in the linked WPF
  files.

## Changes

- `src/LeXtudio.Windows/System.Windows/Controls/Control.cs`: bridge virtual —
  `protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer
  OnCreateAutomationPeer() => null!;`
- `src/LeXtudio.Windows/System.Windows/Automation/Peers/AutomationPeer.cs`:
  WPF-shaped base now extends Uno's FrameworkElementAutomationPeer; statics
  `ListenerExists` / `FromElement` / `CreatePeerForElement` and instance
  `RaiseAutomationEvent` / `RaisePropertyChangedEvent` forward to Uno
  (AutomationEvents enum values mirror UIA IDs 0..17 → plain cast).
  `AutomationProperty` now carries its Uno singleton.
- `AutomationProperties.cs`: `SelectionItemPatternIdentifiers.IsSelectedProperty`
  maps to Uno's singleton.
- `Peers/DataGridAutomationPeers.cs`: real peers — DataGridAutomationPeer
  (ISelectionProvider + IGridProvider; internal raise sites for row/cell
  invoke, cell-selected, selection-changed wired to element peers),
  DataGridItemAutomationPeer / DataGridCellItemAutomationPeer (route
  PropertyChanged through realized element peers),
  DataGridRowAutomationPeer (ISelectionItemProvider).
- `Peers/DataGridCellAutomationPeer.cs`: IValueProvider (text) + ISelectionItem
  + IGridItem; text via `CellAutomationHelper.ScanText`.
- `Peers/DataGridColumnHeaderAutomationPeer.cs`: Header + IInvokeProvider →
  `Owner.Invoke()`.
- `Peers/DataGridColumnHeadersPresenterAutomationPeer.cs` /
  `DataGridDetailsPresenterAutomationPeer.cs` /
  `DataGridRowHeaderAutomationPeer.cs`: control types Group/Group/Header.
- `tests/DataGrid.IntegrationTestHost/MainPage.cs`: probe
  `datagrid.probe.automation-events` — wires Uno's internal
  `IAutomationPeerListener` via reflection + DispatchProxy
  (`RecordingAutomationListener`), drives `grid.SelectedIndex = 1`, returns
  JSON of peer surfaces + recorded events.
- `tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs`:
  `Uia_DataGridExposesNativePeersAndSelectionEvents`.

## Result

63/63 DataGrid tests pass (62 prior + the new automation test). Notable
verified behaviors:
- ListenerExists is true while the test listener is wired (inside the probe).
- Grid peer control type DataGrid; row DataItem; cell DataItem with readable
  Value/Name; column-header peer Header with Invoke pattern.
- Selection/Grid patterns present; GetSelection returns the selected row.
- Selecting index 1 raises SelectionItemPatternOnElementAddedToSelection
  through the full WPF→shim→Uno→listener path.

## Open items

- ITableProvider intentionally not implemented: Uno's public
  `RowOrColumnMajor` enum is not present in the Uno.UI build referenced by the
  shim (the Generated declaration is `#if false`), so the interface can't be
  satisfied without it.
- IGridItemProvider Row/ColumnSpan fixed at 1 (no column/row spans in the
  grid).
- Cell `SetValue` throws NotSupported (editing through automation would need
  the cell's binding write-back machinery).
- Header drag-and-drop automation (item container pattern for reorder) not yet
  exposed.
