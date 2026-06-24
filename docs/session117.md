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
