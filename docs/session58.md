# DataGrid Port - Session 58

Date: 2026-06-13

## Goal

Maximize WPF code reuse / reduce local shims: replace the hand-written local
`DataGridBoundColumn` shim with the **linked upstream `DataGridBoundColumn.cs`**,
so the real WPF binding/style/clipboard/refresh body runs instead of a parallel
reimplementation.

## Why this target (not the selection engine)

Surveyed the candidates first:
- The Selector selection engine (`MakeFullRowSelection`, `SelectionChange`,
  `_selectedItems`, `BeginUpdateSelectedItems`…) is a very large subsystem with
  deep `Selector` base dependencies — too big/risky for one session.
- The column classes are **all local shims** (only `IProvideDataGridColumn.cs`
  was linked from upstream; an earlier read mistook substring matches for links).
  `DataGridBoundColumn` is the base of every bound column (Text/CheckBox/Combo),
  its upstream body (230 lines) is nearly identical to the local shim, and it
  depends only on `DataGridHelper` (local) + `BindingOperations` + coercion.
  High value, contained blast radius.

## What Changed

- **Linked** `ext/.../DataGridBoundColumn.cs` into the project; fork-patched its
  class declaration to `public abstract partial class DataGridBoundColumn` under
  `#if HAS_UNO` (same convention as `DataGrid.cs`).
- **Deleted** the 169-line local `DataGridBoundColumn.cs` body; replaced with a
  29-line partial keeping only the Uno-specific `BindingPath` helper and a
  `CoerceValue` override.
- **Moved** the `DataGridHelper` static class (which used to live inside the
  local Bound shim) into its own `DataGridHelper.cs`.
- **Coercion bridge**: the shim DP system does not invoke registered
  `CoerceValueCallback`s, but upstream `DataGridBoundColumn`'s `Binding` setter
  calls `CoerceValue(IsReadOnlyProperty)` and `CoerceValue(SortMemberPathProperty)`.
  Added a `protected internal virtual void CoerceValue(DependencyProperty)` to
  the local `DataGridColumn` base (handles `IsReadOnly` via `OnCoerceIsReadOnly`)
  and an override in the Bound partial that derives `SortMemberPath` from the
  binding path — faithfully reproducing upstream `OnCoerceSortMemberPath`.

Net: the real upstream `Binding` setter, `ApplyBinding`, `ApplyStyle`,
`ElementStyle`/`EditingElementStyle` DPs, `OnCoerceIsReadOnly`,
`ClipboardContentBinding`, and `RefreshCellContent` now run from WPF source;
the local shim shrank from 169 → 29 lines.

## Verification (confirmed, not assumed)

```
dotnet build  → 0 errors
dotnet run … --probe  → DONE failures=0  (33 steps)
dotnet test  → 124 passed, 0 failed  (+1 BoundColumnBodyIsReusedFromUpstream)
```

Key probe steps that exercise the reused body:
- **header click sorts** — passes only if `SortMemberPath` coerced to `"Age"`
  from the binding (the upstream-equivalent coercion, via the new CoerceValue
  bridge). Probe asserts `SortDescriptions[0].PropertyName == "Age"`.
- **cell editing / read-only coercion** — `OnCoerceIsReadOnly` + `ApplyBinding`
  from upstream.
- **checkbox / combobox write-back** — derived columns still resolve
  `Binding`/`BindingPath` through the reused base.

Unit test `BoundColumnBodyIsReusedFromUpstream` asserts the upstream-only
surface (`ElementStyle`, `ElementStyleProperty`, `ApplyBinding`, `ApplyStyle`)
plus the local partial's `BindingPath` and `CoerceValue` override are present.
(DependencyObject instances need the UI thread, so the coercion *behavior* is
verified by the probe, not the headless unit harness.)

## Next Batch (continue reuse)

1. Link upstream `DataGridTextColumn.cs` (gains the real Font*/Foreground DPs +
   key handling), reducing that local shim the same way.
2. Then `DataGridCheckBoxColumn.cs` / `DataGridComboBoxColumn.cs` (largest column
   shims) — assess their write-back vs upstream `GenerateElement`/binding paths.
3. Longer-horizon: stage the Selector selection-engine reuse behind the now-
   working command routing.
