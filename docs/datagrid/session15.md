# DataGrid Port - Session 15

Date: 2026-06-11

## Goal

Clear the session-14 spine catalog (shim `ItemsControl` virtuals, small
leaves, binding-engine bridges), then re-probe `Selector.cs`/`MultiSelector.cs`
to expose the next error layer.

## What Changed

- Added seven no-op WPF-shaped virtuals to the shim `ItemsControl`
  (`OnInitialized`, `OnIsKeyboardFocusWithinChanged`, `OnItemsChanged`,
  `OnItemsSourceChanged`, `PrepareContainerForItemOverride`,
  `ClearContainerForItemOverride`, `AdjustItemInfoOverride`).
- Linked WindowsBase `KnownBoxes.cs` and deleted the local shim whose
  static-class shape broke `using MS.Internal.KnownBoxes;` (the session-14
  CS0138).
- Added the binding-engine bridges: `BindingExpressionBase` with the
  `DisconnectedItem` sentinel, an untargeted `BindingExpression` that
  evaluates dotted CLR property paths by reflection
  (`Activate`/`Deactivate`/`Value`, `ParentBinding`),
  `BindingExpressionUncommonField`, a `DynamicValueConverter` over
  component-model converters with the `UnsetValue` failure contract, and
  `Binding.XPath` storage.
- Added `AttachedPropertyBrowsableForChildrenAttribute` (flat designer shim,
  following the existing for-type attribute pattern) and
  `MS.Internal.PresentationFramework.BuildInfo` constants.
- Re-probed the spine: layer 2 is 79 unique / 302 total errors (catalog in
  DATAGRID.md). Probe reverted.
- Added `BindingExpressionBridgeTests` (path evaluation, deactivation,
  converter success/failure contracts, sentinel identity, shim virtuals,
  linked `BooleanBoxes` caching).

## Verification

Commands:

```bash
dotnet test WindowsShims/src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj --framework net10.0-desktop --no-restore
dotnet build WindowsShims/src/WindowsShims.slnx --framework net10.0-desktop --no-restore
```

Result: build succeeded; tests passed with 70 passed, 0 failed.

## Notes

Key discovery: `ext/wpf` is a patched fork that uses `#if !HAS_UNO` guards
inside upstream files (`Window.cs`, `AdornerLayer`, `TextBoxBase` per git
history). This is how `Window.cs` compiles despite referencing `BuildInfo`
and Win32 internals. Fork-patching is an established third mechanism.

The layer-2 catalog shows the spine's hard core is the WPF property engine
(`SetCurrentValueInternal`, `GetValueEntry`/`LookupEntry`,
`EffectiveValueEntry`, `DependencyPropertyKey` set paths) plus
`ItemCollection`-as-`CollectionView` currency. Neither has an Uno equivalent,
so bridging alone cannot make `Selector.cs` compile cleanly.

## Next Session

1. Decide the spine mechanism: fork-patch `Selector.cs`/`MultiSelector.cs`
   with `#if !HAS_UNO` guards around the property-engine, automation, XML,
   and currency clusters (keeps upstream as source of truth, matches the
   RichTextBox-era precedent) versus a WPF-shaped local `Selector` spine
   exposing only what `DataGrid.cs` consumes. Lean fork-patch given the
   precedent, but size the guard count first on `MultiSelector.cs` alone —
   it is 102 lines and a natural pilot.
2. If fork-patching: grow `ItemCollection` toward basic currency
   (`CurrentItem`, `MoveCurrentToPosition`, `IsEmpty`) and add the missing SR
   strings as needed.
3. Headers/presenters and validation/binding-group rungs remain after the
   spine.
