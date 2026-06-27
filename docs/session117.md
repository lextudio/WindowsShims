# Session 117

Date: 2026-06-24

## Goal

Plan the next DataGrid migration phase for Roma:

1. Keep API behavior aligned with WPF.
2. Reuse upstream WPF DataGrid source as much as possible.
3. Move missing WinUI/Uno behavior into general WindowsShims binding/template/resource
   infrastructure instead of DataGrid-only local shims.
4. Reduce `#if HAS_UNO` branches only after tests pin the equivalent behavior.

## Current Position

Session 116 added the first Roma DevFlow DataGrid acceptance test and exposed a real
porting issue: WPF `DataGrid.AddAutoColumns()` waits for the WPF measure pipeline,
while the current Uno path builds the visual tree manually. The fix was intentionally
small and guarded for Uno.

That confirms the right testing split:

- WindowsShims unit tests protect API/bridge contracts.
- Roma DevFlow integration tests protect real ILSpy metadata workflows.
- Upstream WPF source edits should be narrow until the compatibility layer is strong
  enough to replace local workarounds.

## Main Compatibility Gaps

### 1. XAML type and resource semantics

Roma currently avoids the original ILSpy metadata XAML in places because the Uno path
does not fully support WPF resource syntax such as:

- `{x:Type ...}`
- `{x:Static ...}`
- `StaticResource` keys that are `Type` or `ComponentResourceKey`
- `DynamicResource`
- `Style.BasedOn` using type-keyed default styles

These gaps are not DataGrid-specific. They should become a general WPF resource bridge
inside WindowsShims, with DataGrid as the first high-value consumer.

### 2. Binding semantics

The metadata filters and row details rely on WPF binding behavior that is currently
either missing or replaced by custom C# factories:

- `RelativeSource FindAncestor`
- `RelativeSource TemplatedParent`
- `TemplateBinding`
- binding path `.`
- converter and `StringFormat` behavior
- `BindingExpression.UpdateTarget()`
- validation without committing source updates

The priority is not a complete WPF binding engine in one step. The practical target is
to implement the subset used by ILSpy metadata views and upstream DataGrid templates,
then expand under tests.

### 3. Template construction

Current local mechanisms include:

- `ShimDataTemplate`
- `DataGridExtensions.FilterControlTemplate`
- hand-built DataGrid row/cell/filter visuals
- cell style application that skips `Control.TemplateProperty`
- row-details handoff through `DataGridHelper.TransferProperty`

These are useful compatibility scaffolds, but the direction should be a generic WPF
template bridge:

- a `ControlTemplate`/`DataTemplate` representation that can carry target type,
  resource lookup, bindings, and a visual-tree factory;
- compatibility aliases for existing `ShimDataTemplate` and filter templates;
- enough `TemplateBinding` and templated-parent support that DataGrid can apply real
  WPF-style templates instead of special-casing every consumer.

### 4. Dependency property metadata and coercion

The upstream DataGrid source expects WPF property metadata behavior:

- metadata override lookup by owner type;
- inherited property values;
- coercion callbacks;
- property changed callbacks that run in WPF order;
- style setters and local values participating in the same precedence model.

Some of this already exists in WindowsShims, but DataGrid still uses local transfer and
sync code where the WPF property system would normally carry values through presenters,
rows, cells, and details presenters.

### 5. ItemsControl, presenter, and virtualization pipeline

The largest remaining difference is structural. The current Uno path builds important
parts of DataGrid with local methods such as `BuildShimVisualTree`, `BuildFilterRow`,
cell row construction, and row-details construction.

This area should be reduced last. It depends on the earlier layers:

- templates must be applyable;
- bindings must resolve ancestors and templated parents;
- styles/resources must load WPF-like keys;
- generated columns, row details, filters, and selection must stay covered by tests.

## Conditional Compilation Triage

### Move toward substrate

These are good candidates for elimination once the bridge exists:

- `ShimDataTemplate` as a DataGrid-only row-details solution.
- `FilterControlTemplate` as a DataGridExtensions-only template marker.
- `DataGridHelper.TransferProperty` paths that copy template-produced values manually.
- `DataGridCell.ApplyShimCellStyle` skipping `Control.TemplateProperty`.
- Roma `RomaMetadataStubs.cs` replacements for original metadata XAML.
- hand-coded resource/style fallback caused by missing `x:Type`, `x:Static`,
  `BasedOn`, `StaticResource`, and `DynamicResource`.
- binding fallbacks caused by missing `RelativeSource FindAncestor` and
  `TemplateBinding`.

### Keep as platform adapters for now

These should remain guarded until the rest of the stack is stronger:

- dispatcher/threading differences;
- focus and keyboard routing differences;
- pointer/mouse input differences;
- WinUI visual-parent lookup fallbacks;
- measure/arrange differences like the auto-column generation issue found in
  session 116;
- visibility and template lifecycle differences in `OnApplyTemplate`.

### Defer until the end

These are high-risk areas and should not be the first target for cleanup:

- `BuildShimVisualTree` and manual row/cell/header construction.
- column virtualization and realization branches in `DataGridColumnCollection`.
- `DataGridCellsPanel` virtualization behavior.
- replacing manual presenter orchestration with the full upstream WPF presenter stack.

## Execution Plan

### Phase 1: Guardrails and inventory

Add focused tests before replacing any shim:

- WindowsShims unit tests for type-keyed resources, `ComponentResourceKey`,
  `Style.BasedOn`, and template setter application.
- WindowsShims unit tests for `RelativeSource FindAncestor` and templated-parent
  binding resolution using small local controls.
- Roma DevFlow tests for metadata row details, especially nested DataGrids in
  `CustomDebugInformation`, COFF header, and optional header details.
- Roma DevFlow tests for metadata filters using the real filter row.

Deliverable: a failing-test map that says which local shim each substrate feature can
replace.

### Phase 2: Generic template bridge

Introduce or evolve a generic WPF template bridge:

- keep `ShimDataTemplate` as a compatibility wrapper at first;
- add a more general internal representation for WPF template factories;
- carry target type, resource scope, name scope, and templated parent;
- allow `Control.TemplateProperty` to be applied through the bridge;
- convert filter templates to use the same bridge metadata instead of a
  DataGridExtensions-only template class.

Deliverable: row details and filter row can use one template bridge, with old type names
still present for source compatibility.

### Phase 3: Resource and binding substrate

Implement the WPF features needed by ILSpy metadata views:

- resource lookup by `Type` and `ComponentResourceKey`;
- `StaticResource` and a minimal `DynamicResource` invalidation/update path;
- `Style.BasedOn` merge/application order;
- `RelativeSource FindAncestor`;
- `RelativeSource TemplatedParent`;
- `TemplateBinding` as a templated-parent binding;
- `BindingExpression.UpdateTarget()` for target refresh.

Deliverable: Roma can stop hand-translating the most common metadata XAML constructs.

### Phase 4: Replace Roma metadata stubs incrementally

Do not delete `RomaMetadataStubs.cs` in one step. Replace resource groups one at a time:

1. default DataGrid cell style;
2. default filter template;
3. hex filter template;
4. flags filter templates;
5. row-details templates;
6. nested details DataGrid styles.

Each replacement should have a Roma DevFlow test proving that the corresponding table,
filter, or details view still works.

Deliverable: Roma uses more original ILSpy metadata resources and fewer C# local
builders.

### Phase 5: Reduce DataGrid-specific rendering code

Only after phases 2-4 are stable, start reducing the local DataGrid visual pipeline:

- move manual value transfer back into property/template binding where possible;
- remove cell style template skips;
- compare upstream presenter code branch-by-branch against the local Uno path;
- migrate low-risk presenter behavior first;
- defer virtualization until non-virtualized behavior is fully covered.

Deliverable: fewer `HAS_UNO` branches in linked WPF DataGrid files, with Roma and
WindowsShims tests proving equivalent behavior.

## Immediate Next Steps

1. Add WindowsShims tests for resource/style substrate:
   - type-keyed `StaticResource`;
   - `ComponentResourceKey`;
   - `Style.BasedOn`;
   - template setter application.
2. Add WindowsShims tests for binding substrate:
   - `RelativeSource FindAncestor`;
   - `RelativeSource TemplatedParent`;
   - `TemplateBinding`.
3. Add Roma DevFlow row-details probe/test:
   - open a metadata table with details;
   - select a row;
   - assert nested DataGrid exists and has rows/columns.
4. Start a small generic template bridge behind existing `ShimDataTemplate`, preserving
   current public surface.

The first implementation target should be substrate tests plus a compatibility bridge
that keeps existing shims working. Removing conditions and local shims comes after the
tests prove that equivalent behavior is now supplied by the generic layer.

## First Execution

Started Phase 1 with a narrow substrate bridge pass.

### WindowsShims changes

Added a generic template bridge surface:

- `System.Windows.Controls.IWpfTemplateBridge`
- `System.Windows.Controls.WpfTemplateBridge`

Updated existing DataGrid shims to sit on that shared surface:

- `ShimDataTemplate` now implements `IWpfTemplateBridge` while preserving its existing
  `Factory` property and public constructor.
- `ControlTemplate` now derives from `WpfTemplateBridge`.
- `DataGridExtensions.FilterControlTemplate` now carries a template target type through
  the shared bridge while preserving `Kind` and `FlagsType`.

Added lightweight WPF binding/resource entry points:

- `System.Windows.Data.RelativeSource`
- `System.Windows.Data.RelativeSourceMode`
- `Binding.RelativeSource`
- Windows App SDK-only `StaticResourceExtension`, `DynamicResourceExtension`,
  `TemplateBindingExtension`, and `TemplateBindingExpression`

The resource/template-binding extension types are intentionally compiled only for
`WINDOWS_APP_SDK`. The Uno desktop target already brings some `System.Windows.*`
resource extension types from `Uno.Xaml`; defining duplicate public types there creates
downstream ambiguity.

### Tests

Added `WpfSubstrateBridgeTests.cs` and registered it in the test project.

Covered:

- `RelativeSource FindAncestor` state.
- WPF `RelativeSource` singleton modes.
- Template bridge shape for `ShimDataTemplate`.
- Template bridge target type on `FilterControlTemplate`.
- Windows App SDK-only resource and template-binding extension contracts.

The `ShimDataTemplate` test is reflection-based because constructing WinUI
`DataTemplate` in the headless desktop test runner requires an initialized Uno
dispatcher.

## First Execution Verification

Command:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 152 passed
- 0 failed
- 0 skipped

The existing nullable warning in `DataGridCellInfoTests.cs` remains.

## Next Execution Candidate

Add the Roma DevFlow row-details probe/test before replacing any metadata XAML stub:

1. Open a metadata table with known row details.
2. Select a row that should produce details.
3. Assert the details presenter reports a nested DataGrid with rows and columns.
4. Use that test to protect the first conversion from `ShimDataTemplate`-only row
   details toward the generic template bridge.

## Second Execution

Added the first Roma row-details acceptance guard.

### Roma changes

Enabled PE header row-details template selection on the Uno path:

- `CoffHeaderTreeNode` now assigns `CharacteristicsDataTemplateSelector` under
  `ROMA_UNO` as well.
- `OptionalHeaderTreeNode` now assigns the same selector for `DLL Characteristics`
  under `ROMA_UNO` as well.
- `CharacteristicsDataTemplateSelector` now overrides WinUI/Uno
  `SelectTemplateCore(...)` instead of WPF-only `SelectTemplate(...)`.

This removes a small but real DataGrid row-details condition from the Roma metadata
path. The selector still returns the existing `HeaderFlagsDetailsDataGrid`
`ShimDataTemplate`, so behavior is preserved while the generic template bridge work
continues underneath it.

Added a new DevFlow probe:

- `roma.probe.metadata-header-row-details`

The probe:

1. Opens an assembly if needed.
2. Selects `COFF Header` or `Optional Header`.
3. Locates the row that should show flag details.
4. Calls the real `RowDetailsTemplateSelector`.
5. Invokes the current `ShimDataTemplate` factory on the UI thread.
6. Reports the nested row-details DataGrid row/column counts.

Added a Roma integration test:

- `MetadataHeader_RowDetailsRendersNestedDataGrid`

It opens `typeof(System.Net.Http.HttpClient).Assembly.Location`, selects `COFF Header`,
and asserts:

- header content is a DataGrid;
- rows and columns are present;
- row-details selector is active;
- selector returns `ShimDataTemplate`;
- nested row-details DataGrid is produced;
- nested rows and columns are present.

## Second Execution Verification

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused integration tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid"
```

Result:

- 2 passed
- 0 failed
- 0 skipped

## Next Execution Candidate

The next substrate step should replace the probe's direct `ShimDataTemplate.Factory`
call with the shared `IWpfTemplateBridge.LoadContent(...)` path once Roma references
the updated WindowsShims package/project shape. After that, move
`CustomDebugInformationDetailsTemplateSelector` to the same WinUI-compatible selector
shape and add a DevFlow test for custom debug information when a stable input assembly
with rows is identified.

## Third Execution

Connected the generic template bridge to the actual DataGrid row-details handoff.

### WindowsShims changes

Updated the row-details path to recognize `IWpfTemplateBridge` instead of only
`ShimDataTemplate`:

- `DataGridDetailsPresenter` now has `ShimTemplateBridge`.
- `DataGridHelper.TransferProperty` stores any selected template implementing
  `IWpfTemplateBridge` on the presenter.
- `DataGridRow.BuildRowDetails` now calls `IWpfTemplateBridge.LoadContent(item)` and
  only falls back to the old `ShimContentFactory` path for compatibility.

`ShimContentFactory` remains for now so existing reflection tests and older callers stay
stable while the bridge is rolled through more template types.

Updated `DataGridRomaMetadataSurfaceTests` to pin the new
`DataGridDetailsPresenter.ShimTemplateBridge` contract.

### Roma changes

Updated `roma.probe.metadata-header-row-details` to call
`IWpfTemplateBridge.LoadContent(...)` instead of directly calling
`ShimDataTemplate.Factory(...)`.

This means the Roma PE-header row-details integration test now proves the generic
template bridge path works end to end for the current `HeaderFlagsDetailsDataGrid`
resource.

## Third Execution Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 152 passed
- 0 failed
- 0 skipped

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused integration tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid"
```

Result:

- 2 passed
- 0 failed
- 0 skipped

## Next Execution Candidate

Move `CustomDebugInformationDetailsTemplateSelector` out of `#if !ROMA_UNO`, convert it
to the WinUI-compatible selector shape, and add a probe that can report whether the
selected assembly has `CustomDebugInformation` rows. If the runtime assembly does not
have stable rows, use a small fixture assembly with portable debug metadata as the test
input.

## Fourth Execution

Extended the row-details acceptance coverage to another real ILSpy metadata table.

### Roma changes

Enabled `CustomDebugInformationDetailsTemplateSelector` on the Uno path:

- removed the `#if !ROMA_UNO` exclusion around the selector hookup;
- converted the selector to WinUI-compatible `SelectTemplateCore(...)` overrides;
- preserved the existing WPF selection logic in a shared helper method.

Added a new DevFlow probe:

- `roma.probe.metadata-custom-debug-row-details`

The probe opens the integration test assembly, selects the
`CustomDebugInformation` metadata table, locates a row with details, invokes the real
row-details selector, and renders the selected template through
`IWpfTemplateBridge.LoadContent(...)`.

Added a Roma integration test:

- `MetadataCustomDebugInformation_RowDetailsRenders`

It asserts that the table renders as a DataGrid, the row-details selector is active,
the selector returns a `ShimDataTemplate`, and the details content renders as either a
nested grid or text content.

### Verification

Roma focused custom-debug test:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter MetadataCustomDebugInformation_RowDetailsRenders
```

Result:

- 1 passed
- 0 failed
- 0 skipped

Roma combined metadata test run:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 2 passed
- 1 failed
- 0 skipped

The combined run failed in `roma.probe.clear` with a DevFlow `timeout`. The failed
header row-details test passed when rerun alone, so the remaining issue appears to be
probe cleanup timing rather than DataGrid rendering behavior.

## Next Slice

1. Add `OptionalHeaderTreeNode` row-details coverage to match the COFF and custom debug
   paths.
2. Add unit tests for `TemplateBinding` and `RelativeSource TemplatedParent` behavior,
   then wire them through `WpfTemplateBridge`.
3. Replace the first Roma metadata resource group from `RomaMetadataStubs.cs` with the
   original ILSpy resource once the needed binding/resource subset is covered.

## Fifth Execution

Closed the remaining PE header row-details coverage gap and stabilized the focused
metadata test run.

### Roma changes

Added a second header row-details integration test:

- `MetadataOptionalHeader_RowDetailsRendersNestedDataGrid`

It reuses `roma.probe.metadata-header-row-details` with `"Optional Header"` and asserts
the `DLL Characteristics` row selects `CharacteristicsDataTemplateSelector`, returns a
`ShimDataTemplate`, and renders a nested details DataGrid through the shared
`IWpfTemplateBridge` path.

The first focused run passed, proving the `OptionalHeaderTreeNode` selector hookup is
active on the Uno path.

Also increased the DevFlow probe UI-dispatch wait in `RunOnUi(...)` from 8 seconds to
30 seconds. The metadata tests were otherwise passing but combined runs could fail when
`roma.probe.clear` sat behind expensive UI work on the dispatcher. This is a test
diagnostics stability fix; it does not change Roma user-facing behavior.

### Verification

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused optional-header test:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter MetadataOptionalHeader_RowDetailsRendersNestedDataGrid
```

Result:

- 1 passed
- 0 failed
- 0 skipped

Roma combined metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Add WindowsShims behavior tests for `TemplateBinding` and
   `RelativeSource TemplatedParent`.
2. Wire templated-parent context through `WpfTemplateBridge.LoadContent(...)`.
3. Use that bridge to start applying one original ILSpy metadata template/resource group
   instead of the corresponding `RomaMetadataStubs.cs` local factory.

## Sixth Execution

Started the templated-parent substrate needed for real WPF-style template reuse.

### WindowsShims changes

Extended the generic template bridge:

- `IWpfTemplateBridge` now exposes
  `LoadContent(object? dataContext, DependencyObject? templatedParent)`.
- `WpfTemplateBridge` can store factories that receive both data context and templated
  parent.
- `ShimDataTemplate` keeps its existing factory behavior and ignores templated parent
  for compatibility.
- `DataGridRow.BuildRowDetails(...)` now passes the `DataGridDetailsPresenter` as the
  templated parent when rendering a bridged row-details template.

Added a small template-binding substrate:

- `System.Windows.WpfTemplateBinding.Apply(...)`

The helper copies a dependency-property value from the templated parent to a template
child. This is deliberately smaller than a full binding engine, but gives hand-built
template factories a common path for WPF `TemplateBinding`-style behavior.

### Tests

Extended `WpfSubstrateBridgeTests` to cover:

- both `IWpfTemplateBridge.LoadContent(...)` overloads;
- propagation of the templated-parent argument through a bridge factory;
- existence of the `WpfTemplateBinding.Apply(...)` copy helper.

The templated-parent propagation test avoids constructing live Uno UI objects because
the headless test runner has no initialized dispatcher.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 154 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma combined metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Add a real bridged template factory in Roma metadata stubs that uses
   `WpfTemplateBinding.Apply(...)` instead of manually setting at least one child
   property.
2. Expand `WpfTemplateBinding` to handle converter and fallback values, matching the
   existing `TemplateBindingExtension` properties.
3. Start replacing one metadata details template with a shape closer to the original
   ILSpy XAML while preserving the DevFlow row-details tests.

## Seventh Execution

Converted all current Roma metadata row-details template stubs to the templated-parent
bridge shape in one pass.

### WindowsShims changes

Extended `ShimDataTemplate` so it can carry both old and new factory forms:

- existing `Factory: Func<object?, FrameworkElement?>` remains for compatibility;
- new `TemplatedParentFactory:
  Func<object?, DependencyObject?, FrameworkElement?>` backs the
  templated-parent bridge path;
- a new public constructor accepts the templated-parent factory directly;
- `IWpfTemplateBridge.LoadContent(dataContext, templatedParent)` now invokes
  `TemplatedParentFactory`.

The old constructor still works and adapts to the new factory internally, so existing
callers are not forced to migrate at once.

### Roma changes

Updated all three metadata row-details resources in `RomaMetadataStubs.cs` to use the
new two-argument factory shape:

- `CustomDebugInformationDetailsDataGrid`;
- `CustomDebugInformationDetailsTextBlob`;
- `HeaderFlagsDetailsDataGrid`.

Updated the Roma DevFlow probes to call:

```csharp
bridge.LoadContent(item, grid)
```

instead of the old one-argument overload. The real DataGrid row-details path already
passes the `DataGridDetailsPresenter` as templated parent from
`DataGridRow.BuildRowDetails(...)`.

### Tests

Extended `WpfSubstrateBridgeTests` to pin the new `ShimDataTemplate`
templated-parent constructor and `TemplatedParentFactory` property. The test remains
reflection-based because live `DataTemplate` construction requires an initialized Uno
dispatcher.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 155 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma combined metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Expand `WpfTemplateBinding.Apply(...)` to support converter, converter parameter,
   target-null, and fallback semantics.
2. Replace the direct `RowDetails` reads in metadata factories with a shared
   WPF-style binding evaluator for the `RowDetails` path.
3. After that, start deleting equivalent hand-coded template logic from
   `RomaMetadataStubs.cs` resource by resource.

## Eighth Execution

Took a larger step toward reusable WPF binding/template behavior by adding a real
binding evaluator and moving Roma metadata stubs onto it.

### WindowsShims changes

Added:

- `System.Windows.Data.BindingEvaluator`

The evaluator currently supports:

- empty path and `.` returning the source object;
- dotted public property paths such as `RowDetails.Name`;
- `Binding.Source`;
- `FallbackValue` when a path cannot be resolved;
- `TargetNullValue` when a resolved property is null;
- `IValueConverter`;
- `ConverterParameter`;
- `ConverterCulture`;
- `StringFormat`.

This is intentionally stronger than the previous template-only bridge. It gives
programmatic templates a WPF-shaped way to evaluate the same bindings found in the
original ILSpy XAML.

Added `BindingEvaluatorTests` covering property paths, `.`/empty paths, fallback,
target-null, converter, and `StringFormat`.

### Roma changes

Replaced direct `RowDetails` property access in all metadata row-details factories with
`BindingEvaluator.Evaluate(..., new Binding("RowDetails"))`:

- `BuildCustomDebugInfoDataGrid`;
- `BuildCustomDebugInfoTextBlob`;
- `BuildHeaderFlagsDataGrid`.

Updated Roma DevFlow probes to use the same evaluator for probe-side property reads:

- `Member`;
- `RowDetails`;
- `Kind`.

This removes another local reflection/property-read path and moves both runtime stubs
and tests toward the same binding semantics.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 155 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma combined metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Let `WpfTemplateBinding.Apply(...)` consume a `TemplateBindingExtension`, including
   converter and converter parameter.
2. Add a small template-factory builder that wires `BindingEvaluator` and
   `WpfTemplateBinding` together, so Roma metadata stubs describe bindings instead of
   imperative reads.
3. Convert `CustomDebugInformationDetailsTextBlob` first because it is a compact
   one-control template and maps directly to the original XAML binding.

## Ninth Execution

Moved from binding value evaluation to binding assignment so template factories can
describe data flow closer to XAML.

### WindowsShims changes

Extended `BindingEvaluator` with:

```csharp
BindingEvaluator.Apply(target, propertyName, dataContext, binding)
```

The method:

- resolves the binding using the existing evaluator;
- locates a writable public property on the target;
- coerces primitive/enum values where needed;
- writes the property value.

Added tests for:

- applying a binding to a writable property;
- string-to-int coercion;
- missing target property failure.

### Roma changes

Updated metadata row-details factories so control properties are assigned through
bindings instead of direct imperative values:

- `BuildCustomDebugInfoDataGrid` now applies `ItemsSource <- {Binding RowDetails}`;
- `BuildCustomDebugInfoTextBlob` now applies `Text <- {Binding RowDetails}`;
- `BuildHeaderFlagsDataGrid` now applies `ItemsSource <- {Binding RowDetails}`.

The factories still construct the controls in C#, but data transfer now uses a shared
WPF-shaped binding assignment path. This makes the remaining stub code much closer to
the original metadata XAML.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 155 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma combined metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Add a `WpfTemplateFactory` helper that constructs controls and applies a list of
   binding assignments in one declaration.
2. Convert all three Roma row-details factories to use that helper.
3. After that, move filter template creation toward the same declarative resource shape.

## Tenth Execution

Introduced a reusable template factory builder and moved all current Roma metadata
row-details factories onto it.

### WindowsShims changes

Added:

- `System.Windows.Controls.WpfTemplateFactory`;
- `System.Windows.Controls.BindingAssignment`.

`WpfTemplateFactory.Create<T>(...)` constructs a framework element, runs an optional
initializer, then applies a list of binding assignments through `BindingEvaluator`.
This gives programmatic templates a compact declaration shape:

```csharp
WpfTemplateFactory.Create<TextBox>(
    item,
    textBox => { ... },
    BindingAssignment.To(nameof(TextBox.Text), new Binding("RowDetails")));
```

Added `WpfTemplateFactoryTests` to cover `BindingAssignment` behavior and pin the
factory surface without constructing live Uno controls in the headless unit runner.

### Roma changes

Converted all three metadata row-details factories to `WpfTemplateFactory`:

- `BuildCustomDebugInfoDataGrid`;
- `BuildCustomDebugInfoTextBlob`;
- `BuildHeaderFlagsDataGrid`.

The factories still contain control initialization where the original XAML had element
attributes and child column declarations, but binding application is now declarative and
centralized.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 155 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma combined metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Add first-class declarative column definitions for `WpfTemplateFactory` so
   `HeaderFlagsDetailsDataGrid` stops hand-adding columns in an initializer.
2. Move `DataGridCellStyle` and `ItemContainerStyle` creation toward shared style
   helpers, preparing for `Style.BasedOn` and type-keyed resource work.
3. Convert filter resource creation into declarative records so the remaining
   `RomaMetadataStubs.cs` resources are data descriptions rather than ad hoc code.

## Eleventh Execution

Pushed `WpfTemplateFactory` further from "control construction helper" toward a
small declarative DataGrid substrate.

### WindowsShims changes

Added:

- `System.Windows.Controls.DataGridColumnSpec`;
- `System.Windows.Controls.DataGridColumnKind`;
- `WpfTemplateFactory.ApplyColumns(...)`.

`DataGridColumnSpec.Text(...)` and `DataGridColumnSpec.CheckBox(...)` now describe
the common WPF `DataGridTextColumn` / `DataGridCheckBoxColumn` declarations that
Roma needs for metadata row-details templates. `ApplyColumns` materializes those
specs into real DataGrid columns.

Extended `WpfTemplateFactoryTests` to pin:

- text column kind, header, binding, and read-only defaults;
- checkbox column kind, header, binding, and read-only defaults.

### Roma changes

Updated `BuildHeaderFlagsDataGrid` so its columns are now declared through
`DataGridColumnSpec` instead of hand-created inside the initializer:

```csharp
WpfTemplateFactory.ApplyColumns(
    grid,
    DataGridColumnSpec.CheckBox("Value", new Binding("Value")),
    DataGridColumnSpec.Text("Meaning", new Binding("Meaning")));
```

This removes another local template shim from Roma and puts the reusable shape in
WindowsShims where other migrated WPF templates can share it.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 155 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma metadata integration tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Add a shared style factory for `Style`, setters, and type-targeted style
   declarations so Roma's remaining `DataGridCellStyle` and `ItemContainerStyle`
   code can move out of local factories.
2. Extend that style substrate toward WPF-specific concepts that Uno lacks or only
   partially exposes, starting with `BasedOn` and type-keyed resource lookup.
3. Convert filter resources in `RomaMetadataStubs.cs` into declarative resource
   specs, keeping resource construction in WindowsShims and leaving Roma with data
   descriptions.

## Twelfth Execution

Moved the first style/resource layer out of Roma and into WindowsShims.

### WindowsShims changes

Added:

- `System.Windows.Controls.WpfStyleFactory`;
- `System.Windows.Controls.SetterSpec`;
- `WpfStyleFactoryTests`.

`WpfStyleFactory.Create(...)` now materializes a WinUI `Style` from a target type
and a declarative list of setter specs. This gives Roma and future WPF template
ports a single place to grow WPF style semantics such as `BasedOn`, type-keyed
style lookup, and style resource expansion.

Also fixed the test project coverage gap: the newly added binding/template/style
test files are now explicitly included in `LeXtudio.Windows.Tests.csproj`, which
uses `EnableDefaultCompileItems=false`.

### Roma changes

Converted the remaining local style construction in `RomaMetadataStubs.cs`:

- `DataGridCellStyle`;
- `ItemContainerStyle`.

Both resources now declare setter specs and let WindowsShims build the actual
`Style` objects. This removes another ad hoc Roma-side shim and keeps the
DataGrid support moving toward reusable WPF substrate code.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 169 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma metadata integration tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Add declarative resource specs for `FilterControlTemplate` and flags filters so
   `RomaMetadataStubs.cs` can describe filters as data instead of constructing
   each resource directly.
2. Extend `WpfStyleFactory` with `BasedOn` support and resource-key helpers, then
   look for upstream ILSpy styles that can stop carrying `ROMA_UNO` exclusions.
3. Revisit `DataGridColumnSpec.CreateColumn` and `WpfStyleFactory.Create` test
   coverage through DevFlow/Roma probes because those paths require a live Uno
   dispatcher and cannot be fully exercised by headless WindowsShims unit tests.

## Thirteenth Execution

Took a larger step and introduced a unified resource-spec layer instead of adding
one-off helpers for each remaining Roma metadata resource type.

### WindowsShims changes

Added:

- `System.Windows.Controls.WpfResourceFactory`;
- `System.Windows.Controls.WpfResourceSpec`;
- `WpfResourceFactoryTests`.

`WpfResourceSpec` now covers:

- simple value resources;
- `DataGridExtensions.FilterControlTemplate` resources;
- flags filter templates;
- style resources backed by `WpfStyleFactory`;
- `ShimDataTemplate` resources backed by template bridge factories.

This gives future WPF `ResourceDictionary` ports a single declaration shape:

```csharp
WpfResourceFactory.CreateMany(
    WpfResourceSpec.TextFilter("DefaultFilter"),
    WpfResourceSpec.Style("DataGridCellStyle", typeof(DataGridCell), ...),
    WpfResourceSpec.DataTemplate("HeaderFlagsDetailsDataGrid", BuildHeaderFlagsDataGrid));
```

Headless WindowsShims tests intentionally verify the declaration/API layer and
avoid materializing Uno `Style`, `DataTemplate`, or WPF DataGrid column instances
that require a live dispatcher. Roma integration tests cover those materialized
paths.

### Roma changes

Rewrote `MetadataTableViews.BuildResources()` as one declarative resource list.

Removed direct Roma-side construction for:

- `DefaultFilter`;
- `HexFilter`;
- all flags filters;
- `DataGridCellStyle`;
- `ItemContainerStyle`;
- all row-details `ShimDataTemplate` resources.

Also removed the local `Flags(...)` helper. The remaining Roma code now mostly
describes ILSpy-specific resource keys and data types; WindowsShims owns the
resource construction substrate.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 173 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma metadata integration tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Push `WpfResourceFactory` one layer higher: add a helper that populates a
   `ResourceDictionary` directly from specs so Roma's constructor no longer loops
   over tuples.
2. Add `BasedOn` and resource-key references to `WpfStyleFactory` / `WpfResourceSpec`
   so upstream WPF styles can retain more of their original structure.
3. Start mapping upstream `MetadataTableViews.xaml` resource entries into specs
   mechanically, with the long-term goal of replacing hand-written Roma stubs by a
   reusable WPF resource translation layer.

## Fourteenth Execution

Raised the resource substrate one level higher and added the first style inheritance
surface.

### WindowsShims changes

Extended `WpfResourceFactory` with:

- `Populate(ResourceDictionary, params WpfResourceSpec[])`;
- direct dictionary assignment from specs.

Extended `WpfStyleFactory` with:

- `Create(Type, Style? basedOn, params SetterSpec[])`;
- `StyleSpec`;
- `WpfStyleFactory.Style(...)`;
- `WpfStyleFactory.BasedOn(...)`;
- `WpfResourceSpec.Style(string, StyleSpec)`.

This means resource dictionary ports can now stay at the spec layer and style
declarations can preserve a WPF-like inheritance shape instead of flattening every
setter up front.

### Roma changes

Changed `MetadataTableViews` so the constructor delegates resource population to
WindowsShims:

```csharp
WpfResourceFactory.Populate(this, BuildResources());
```

`BuildResources()` now returns `WpfResourceSpec[]` directly. Roma no longer exposes
the intermediate `(key, value)` materialization loop.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 175 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma metadata integration tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

1. Add resource-key based style inheritance so `BasedOn="{StaticResource ...}"`
   can be represented before the referenced style is materialized.
2. Add a `WpfResourceDictionaryBuilder` or equivalent ordered resolver if resource
   specs need cross-resource lookups.
3. Start translating a concrete upstream `MetadataTableViews.xaml` style/template
   fragment into specs to identify the next missing WPF XAML construct.

## Fifteenth Execution

Tried direct upstream XAML reuse for the metadata resource dictionary.

### WindowsShims changes

Added `System.Windows.Controls.WpfXamlResourceTranslator`, a small XAML-to-resource
translator that parses a WPF `ResourceDictionary` XML document and emits
`WpfResourceSpec` entries for the constructs we can safely reuse today:

- `<Style x:Key=... TargetType=...>` with simple `<Setter Property=... Value=...>`;
- text filter `<ControlTemplate>`;
- hex filter `<ControlTemplate>`;
- flags filter `<ControlTemplate>` with `FlagsType="{x:Type ...}"`.

Unsupported resources, especially current DataTemplates, are intentionally left
for fallback specs. This lets Roma consume the real upstream XAML file while
keeping the existing C# row-details factories as a bridge.

Also changed `SetterSpec` so XAML-translated setters can store a property name and
resolve the actual dependency property lazily when the style is materialized. This
avoids headless unit tests triggering Uno `Control` static initialization while
still allowing Roma's live UI path to create real setters.

Added `WpfXamlResourceTranslatorTests` covering:

- style + text filter + flags filter translation;
- fallback resources for unsupported XAML entries.

### Roma changes

Copied upstream `ext/ilspy/ILSpy/Metadata/MetadataTableViews.xaml` into the Roma
output as content:

```xml
<Content Include="..\..\ext\ilspy\ILSpy\Metadata\MetadataTableViews.xaml"
         Link="ILSpy\Metadata\MetadataTableViews.xaml"
         CopyToOutputDirectory="PreserveNewest" />
```

Changed `MetadataTableViews.BuildResources()` to prefer the copied upstream XAML:

```csharp
var xamlPath = Path.Combine(AppContext.BaseDirectory, "ILSpy", "Metadata", "MetadataTableViews.xaml");
if (File.Exists(xamlPath))
{
    return WpfXamlResourceTranslator.TranslateResourceDictionary(
        File.ReadAllText(xamlPath),
        ResolveMetadataXamlType,
        fallbackResources);
}
```

The fallback path still exists for robustness, but the normal debug/build output
now reuses upstream `MetadataTableViews.xaml` for filters and simple styles.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 177 passed
- 0 failed
- 0 skipped
- existing nullable warning in `DataGridCellInfoTests.cs` remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma metadata integration tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

Output file check:

```bash
ls -l src/Roma.Host/bin/Debug/net10.0-desktop/ILSpy/Metadata/MetadataTableViews.xaml
```

Result:

- upstream XAML is present in the Roma output directory.

## Next Slice

1. Translate `StaticResource` / `DynamicResource` / `BasedOn` into deferred resource
   references so more WPF styles can stay structurally intact.
2. Add diagnostics that report which upstream XAML resources were translated and
   which fell back, making gaps visible in integration tests.
3. Start translating simple DataTemplates into `ShimDataTemplate` factories by
   recognizing a small element subset such as `DataGrid`, `TextBox`, `Grid`, and
   binding attributes.

## Sixteenth Execution

Closed the high-value Roma metadata DataTemplate fallback slice.

### WindowsShims changes

Extended `WpfXamlResourceTranslator` so simple upstream metadata
`DataTemplate` entries with a `DataGrid` root are translated into
`ShimDataTemplate` resources:

- `ItemsSource="{Binding ...}"` becomes a `BindingAssignment`;
- simple bool, enum, and numeric attributes are applied through the existing
  template factory;
- markup-extension values such as `StaticResource` are skipped for now instead
  of being incorrectly assigned as strings.

The existing `TextBox` DataTemplate path remains in place. The translator unit test
now pins both `TextBox` and `DataGrid` DataTemplates as translated resources with no
fallback for the simple case.

### Roma changes

Removed the three local row-details fallback factories from `RomaMetadataStubs.cs`:

- `CustomDebugInformationDetailsDataGrid`;
- `CustomDebugInformationDetailsTextBlob`;
- `HeaderFlagsDetailsDataGrid`.

Roma now relies on the copied upstream `MetadataTableViews.xaml` for these row-details
templates. The resource translation integration test was tightened so those three
keys must appear in `TranslatedKeys` and must not appear in `FallbackKeys`.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 180 passed
- 0 failed
- 0 skipped
- existing warnings remain

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused metadata/resource tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 4 passed
- 0 failed
- 0 skipped

## Next Slice

The quick Roma metadata reuse win is now mostly harvested. The next useful small slice
is to translate the remaining simple upstream metadata resource entry,
`byteWidthConverter`, or to add resource-reference application for skipped
`StaticResource` style setters. Larger DataGrid visual-pipeline reuse should still wait
until more template/style substrate is covered.

## Seventeenth Execution

Started the small follow-up resource translation slice for the last simple upstream
metadata resource.

### WindowsShims changes

Extended `WpfXamlResourceTranslator` so a keyed object resource such as:

```xml
<local:ByteWidthConverter x:Key="byteWidthConverter" />
```

can be translated through the existing type resolver. The translator now:

- asks the resolver for the element local name and a `local:`-prefixed form;
- prefers a static `Instance` field/property when present;
- falls back to a parameterless constructor, including non-public constructors.

Added a unit test that pins the static `Instance` path, matching the upstream
`ByteWidthConverter` shape.

### Roma changes

Updated the metadata XAML type resolver so `ByteWidthConverter` resolves to the
upstream ILSpy converter type, and tightened the integration assertion so
`byteWidthConverter` is expected in translated resources rather than skipped resources.

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 181 passed
- 0 failed
- 0 skipped
- existing warnings remain

UnoEdit native asset blocker:

The Roma build then hit the existing macOS native asset copy issue in `UnoEdit`.
The UnoEdit copy item was updated to point at the sibling `TextCore.Uno` checkout
and only include the dylib when it exists. A direct UnoEdit build passed after this
change.

Roma build/integration status:

Roma.Host rebuild is currently blocked by existing local project state unrelated to
the translator change:

- `Roma.Host.csproj` has an existing `UseNuGetPackages=false` change;
- the source-generator path reports `UseStudio` missing in one build mode;
- the package-mode override exposes missing `System.Windows.Threading` /
  `System.Windows.Media` shim references in `TomsToolbox.Wpf.Uno`.

Because Roma.Host could not be rebuilt, the `metadata-xaml-resources` integration probe
still ran against the previous host output and did not observe the new
`byteWidthConverter` translation yet. The WindowsShims translator behavior itself is
covered by the new unit test.

## Eighteenth Execution

Completed the remaining metadata resource translation verification and added one more
small resource-reference bridge.

### WindowsShims changes

Added a resource-aware `WpfResourceSpec.DataTemplate(...)` overload. Template factories
can now capture the `ResourceDictionary` used by `WpfResourceFactory.Populate(...)`.

Updated `WpfXamlResourceTranslator` so simple `StaticResource` attributes on translated
`DataGrid` DataTemplates are resolved when the template is materialized. This covers
the upstream metadata template attribute:

```xml
CellStyle="{StaticResource DataGridCellStyle}"
```

Simple scalar attributes and `ItemsSource` binding continue to use the existing
template factory path. Unsupported markup extensions are still skipped rather than
misassigned.

### Roma verification

After the local TomsToolbox/Roma build state was fixed, the metadata resource probe
confirmed the previous `byteWidthConverter` translation change.

Focused Roma metadata tests passed after the resource-reference bridge:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 5 passed
- 0 failed
- 0 skipped

### Verification

WindowsShims:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 182 passed
- 0 failed
- 0 skipped
- existing nullable warning remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

## Next Slice

The upstream `MetadataTableViews.xaml` path is now largely harvested for Roma. The
remaining quick wins are likely diagnostics and cleanup: make the translation report
assert the expected skipped set precisely, then consider whether the leftover
`DataGridCellStyle` `Setter.Value` `ControlTemplate` should stay skipped or become the
next explicit substrate feature.

## Nineteenth Execution

Tightened the upstream metadata XAML translation guard.

### Roma changes

Updated `MetadataTableViews_ReportsUpstreamXamlResourceTranslation` so it now asserts
the exact translated resource list from upstream `MetadataTableViews.xaml`:

- `DataGridCellStyle`;
- `DefaultFilter` / `HexFilter`;
- all flags filter templates;
- `ItemContainerStyle`;
- `byteWidthConverter`;
- all three row-details `DataTemplate` resources.

The test now also asserts that both `FallbackKeys` and `SkippedKeys` are empty. This
locks in that every top-level resource in the upstream metadata resource dictionary is
handled by the WindowsShims translator.

### Verification

Roma resource translation test:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter MetadataTableViews_ReportsUpstreamXamlResourceTranslation
```

Result:

- 1 passed
- 0 failed
- 0 skipped

Roma focused metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 5 passed
- 0 failed
- 0 skipped

## Next Slice

The remaining local difference in this resource file is semantic depth, not top-level
coverage: `DataGridCellStyle` is translated as a style with simple setters, but its
nested `Setter.Value` `ControlTemplate` is still not represented. The next worthwhile
substrate slice would be explicit support for style setter element values, starting
with `ControlTemplate` metadata rather than a full visual tree.

## Twentieth Execution

Added style setter element-value translation for the first real upstream case:
`DataGridCellStyle` now preserves a nested `Setter.Value` `ControlTemplate` descriptor.

### WindowsShims changes

Updated `WpfXamlResourceTranslator.TranslateStyle` so a setter can be translated from
either a `Value` attribute or a child `Setter.Value` element. The first supported child
element form is:

```xml
<Setter Property="Template">
  <Setter.Value>
    <ControlTemplate TargetType="{x:Type DataGridCell}" />
  </Setter.Value>
</Setter>
```

The translator now creates a `System.Windows.Controls.ControlTemplate`, resolves its
`TargetType`, and keeps the original XML element as template metadata. This is still a
metadata bridge, not a full visual-tree compiler, but it is enough to represent the
upstream resource shape without treating that setter as lost.

`WpfResourceSpec` also now carries an optional `Descriptor`, so tests and downstream
diagnostics can inspect the translated `StyleSpec` without creating the runtime resource
first.

### Verification

WindowsShims tests:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 182 passed
- 0 failed
- 0 skipped
- existing nullable warning remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 5 passed
- 0 failed
- 0 skipped

## Next Slice

This completes the quick-win coverage for all top-level upstream
`MetadataTableViews.xaml` resources and the known nested `DataGridCellStyle` template
setter metadata. The next useful code-reuse slice is probably to point another WPF XAML
resource file at the same translator and let the unsupported constructs surface from
tests, rather than expanding the translator speculatively.

## Twenty-First Execution

Removed the translator's string-only resource-key assumption.

### WindowsShims changes

`WpfResourceSpec.Key` is now an `object`, matching WPF `ResourceDictionary` semantics
more closely. This keeps existing string-keyed resources working while opening the path
for type-keyed resources such as implicit:

```xml
<DataTemplate DataType="{x:Type local:SomeViewModel}">
```

`WpfXamlResourceTranslator` now resolves an implicit `DataTemplate` key from `DataType`
when `x:Key` is absent. Translation reports still expose readable string keys, using the
type full name for type-keyed resources.

This does not yet translate the full `FlagsTooltip.xaml` visual tree, but it removes the
first substrate blocker for metadata resources that use WPF's type-keyed template model.

### Verification

WindowsShims tests:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 183 passed
- 0 failed
- 0 skipped
- existing warnings remain

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused metadata tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 5 passed
- 0 failed
- 0 skipped

## Next Slice

The next concrete target should be resource extraction from non-`ResourceDictionary`
roots, starting with `Control.Resources` in `FlagsTooltip.xaml`. After that, the
remaining blocker is translating its simple StackPanel/TextBlock/ListBox/CheckBox
template trees into a usable Uno `ShimDataTemplate`.

## Twenty-Second Execution

Extended upstream XAML resource extraction beyond `ResourceDictionary` roots and wired
`FlagsTooltip.xaml` into the Roma diagnostics loop.

### WindowsShims changes

`WpfXamlResourceTranslator.TranslateResourceDictionary` now extracts resource elements
from either:

- a `ResourceDictionary` root; or
- a non-dictionary root's matching `RootType.Resources` child, such as
  `Control.Resources`.

This keeps existing callers/API names stable while allowing WPF control XAML files to
feed their local resource dictionaries into the same translation substrate.

Added coverage for a `Control` root containing both a keyed object resource and an
implicit type-keyed `DataTemplate`.

### Roma changes

Linked upstream `ILSpy/Metadata/FlagsTooltip.xaml` as output `Content`, without enabling
`InitializeComponent`. The XAML is now available as translator input beside
`MetadataTableViews.xaml`.

Added a DevFlow probe and integration test for the real upstream `FlagsTooltip.xaml`.
The current report is intentionally partial:

- translated: `nullVisConv`;
- skipped: `ICSharpCode.ILSpy.Metadata.MultipleChoiceGroup`,
  `ICSharpCode.ILSpy.Metadata.SingleChoiceGroup`;
- fallback: empty.

This confirms the file is now in the reuse pipeline, and the remaining unsupported
surface is the two typed template visual trees.

### Verification

WindowsShims tests:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 184 passed
- 0 failed
- 0 skipped
- existing warnings remain

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused metadata/resource tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|FlagsTooltip_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 6 passed
- 0 failed
- 0 skipped

## Next Slice

Translate the first simple non-TextBox `DataTemplate` visual tree shape needed by
`FlagsTooltip.xaml`: `StackPanel` with `TextBlock` children and simple bindings. Once
that works, the multiple-choice template adds the next nested shape:
`ListBox.ItemTemplate` with `CheckBox`.

## Twenty-Third Execution

Translated the first `FlagsTooltip.xaml` typed template visual tree.

### WindowsShims changes

Added a deliberately small `DataTemplate` element builder for this upstream shape:

- root `StackPanel`;
- child `TextBlock` elements;
- simple property assignment such as `Orientation`;
- `TextBlock.Text` bindings, including dotted paths such as `SelectedFlag.Name`.

Unsupported descendants still cause the template to remain skipped. This keeps
`MultipleChoiceGroup` out until `ListBox.ItemTemplate` and `CheckBox` support are added,
while allowing the simpler `SingleChoiceGroup` template to translate now.

### Roma changes

Updated the real `FlagsTooltip.xaml` translation guard. The current upstream report is:

- translated: `nullVisConv`, `ICSharpCode.ILSpy.Metadata.SingleChoiceGroup`;
- skipped: `ICSharpCode.ILSpy.Metadata.MultipleChoiceGroup`;
- fallback: empty.

### Verification

WindowsShims tests:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 185 passed
- 0 failed
- 0 skipped
- existing nullable warning remains

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused metadata/resource tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|FlagsTooltip_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 6 passed
- 0 failed
- 0 skipped

## Next Slice

Finish `FlagsTooltip.xaml` by translating the remaining `MultipleChoiceGroup`
template. The missing constructs are `ListBox`, `ListBox.ItemTemplate`, and `CheckBox`
with simple bindings.

## Twenty-Fourth Execution

Finished the current `FlagsTooltip.xaml` resource translation coverage.

### WindowsShims changes

Extended the small `DataTemplate` element builder with the remaining upstream shape:

- `ListBox` with `ItemsSource` binding;
- `ListBox.ItemTemplate`;
- nested `DataTemplate`;
- `CheckBox` with `Content` and `IsChecked` bindings.

This is still intentionally scoped to the metadata tooltip resource shape. General WPF
layout, attached properties, brushes, margins, and triggers are not treated as a full
XAML compiler here; unsupported values are ignored when the target WinUI property cannot
accept them.

### Roma changes

Updated the real `FlagsTooltip.xaml` translation guard. The current upstream report is:

- translated: `nullVisConv`,
  `ICSharpCode.ILSpy.Metadata.MultipleChoiceGroup`,
  `ICSharpCode.ILSpy.Metadata.SingleChoiceGroup`;
- skipped: empty;
- fallback: empty.

This means all resource entries inside upstream `FlagsTooltip.xaml` are now in the
translator pipeline.

One caveat: Uno currently emits a `Uno0001` warning for
`Microsoft.UI.Xaml.Controls.ListBox` in the generated template factory. That is acceptable
for the translation/report milestone, but if the tooltip UI is exercised visually we may
want to map this specific WPF `ListBox` shape onto a supported Uno control such as
`ListView`/`ItemsControl`.

### Verification

WindowsShims tests:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 186 passed
- 0 failed
- 0 skipped
- existing warnings remain, plus the expected Uno `ListBox` implementation warning

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused metadata/resource tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|FlagsTooltip_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 6 passed
- 0 failed
- 0 skipped

## Next Slice

The next reuse target should be a different small upstream XAML file, or a visual probe
for the translated `FlagsTooltip` templates. If visual tooltip rendering matters next,
replace the internal `ListBox` emission with a Uno-supported items control before
asserting rendered output.

## Twenty-Fifth Execution

Closed the `FlagsTooltip.xaml` `ListBox` caveat.

### WindowsShims changes

The translator still recognizes upstream WPF `<ListBox>` in `FlagsTooltip.xaml`, but now
emits a WinUI `ListView` for the generated template factory. This preserves the WPF XAML
resource shape while avoiding Uno's `Microsoft.UI.Xaml.Controls.ListBox` implementation
warning.

The existing `ListBox.ItemTemplate` and `CheckBox` translation path remains unchanged:

- `ItemsSource` binding is applied to the generated `ListView`;
- nested item `DataTemplate` is represented by `ShimDataTemplate`;
- `CheckBox.Content` and `CheckBox.IsChecked` are bound from each item.

### Verification

WindowsShims tests:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 186 passed
- 0 failed
- 0 skipped
- the previous Uno `ListBox` implementation warning is gone

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 warnings
- 0 errors

Roma focused metadata/resource tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "MetadataTableViews_ReportsUpstreamXamlResourceTranslation|FlagsTooltip_ReportsUpstreamXamlResourceTranslation|MetadataTable_RendersDataGrid|MetadataHeader_RowDetailsRendersNestedDataGrid|MetadataOptionalHeader_RowDetailsRendersNestedDataGrid|MetadataCustomDebugInformation_RowDetailsRenders"
```

Result:

- 6 passed
- 0 failed
- 0 skipped

## Next Slice

The `FlagsTooltip.xaml` resource extraction/translation path is now complete for the
current upstream file. The next worthwhile reuse target should be another small upstream
resource file, preferably one with a diagnostic guard like the metadata files, rather
than expanding the template builder speculatively.

## Twenty-Sixth Execution

Started the next small upstream resource target: ILSpy resource-table XAML.

### WindowsShims changes

Extended `WpfXamlResourceTranslator` so it can translate resource blocks nested below
the XAML root, such as `UserControl > Grid > Grid.Resources`. This is required by:

- `ILSpy/Controls/ResourceStringTable.xaml`
- `ILSpy/Controls/ResourceObjectTable.xaml`

Added support for the resource shapes used by those files:

- `AlternationConverter` with nested `SolidColorBrush` entries;
- keyed `SolidColorBrush` as a deferred resource value;
- style setter values that are WPF `Binding` markup extensions;
- style setter element values for `ContextMenu`/`MenuItem`;
- delayed materialization for brush and context-menu objects so translator tests do not
  need a live Uno dispatcher.

Added a minimal `System.Windows.Controls.AlternationConverter` shim and lightweight
descriptor records (`SolidColorBrushSpec`, `AlternationConverterSpec`,
`ContextMenuSpec`, `MenuItemSpec`) so translated resources can be inspected without
constructing WinUI controls.

### Roma changes

Copied the upstream resource-table XAML files to output:

- `ILSpy/Controls/ResourceStringTable.xaml`
- `ILSpy/Controls/ResourceObjectTable.xaml`

Added `roma.probe.resource-tables-xaml-resources`, which translates both upstream files
and reports translated/fallback/skipped keys. Added the corresponding integration test
`ResourceTables_ReportUpstreamXamlResourceTranslation`.

Both files now translate:

- `BackgroundConverter`
- `alternatingWithBinding`

with empty fallback and skipped lists.

This does not yet replace `RomaResourceTables.cs`; it proves that the shared upstream
resource block is now reusable and gives the next slice a guarded entry point.

### Verification

WindowsShims tests:

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result:

- 187 passed
- 0 failed
- 0 skipped

Roma build:

```bash
dotnet build src/Roma.Host/Roma.Host.csproj -f net10.0-desktop --no-restore
```

Result:

- 0 errors
- existing warnings remain

Roma focused XAML translation tests:

```bash
dotnet test tests/Roma.IntegrationTests/Roma.IntegrationTests.csproj --filter "ResourceTables_ReportUpstreamXamlResourceTranslation|FlagsTooltip_ReportsUpstreamXamlResourceTranslation|MetadataTableViews_ReportsUpstreamXamlResourceTranslation"
```

Result:

- 3 passed
- 0 failed
- 0 skipped

## Next Slice

The next practical step is deciding whether to consume the translated
`alternatingWithBinding` style inside the current `ResourceTableView<TRow>` implementation,
or whether to start replacing `RomaResourceTables.cs` with a thinner adapter around the
upstream `ResourceStringTable.xaml`/`ResourceObjectTable.xaml` shape. Directly linking the
upstream `.xaml.cs` is still not a quick win because it depends on WPF
`ICollectionView`, command bindings, clipboard commands, and named XAML controls.
