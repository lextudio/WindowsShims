# Session 114

Date: 2026-06-24

## Goal

Close two DataGrid gaps that Roma/ILSpy actually exercises:

1. **Row details `RowDetailsTemplateSelector`** — three ILSpy tree nodes
   (`CustomDebugInformationTableTreeNode`, `CoffHeaderTreeNode`,
   `OptionalHeaderTreeNode`) set `RowDetailsTemplateSelector`; the shim only
   handled `RowDetailsTemplate`, so the details section never appeared.

2. **`AutoGenerateColumns` ordering verification** — ILSpy's `PrepareDataGrid`
   uses `AutoGenerateColumns=true` everywhere; confirmed the chain is wired and
   functionally correct (columns generated before final `BuildShimVisualTree`).

## Changes

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridHelper.cs`

In `TransferProperty` for `DataGridDetailsPresenter`, the `ContentTemplateSelector`
branch was a no-op comment. Replaced with active resolution:

```csharp
if (dp == ContentPresenter.ContentTemplateProperty
    || dp == ContentPresenter.ContentTemplateSelectorProperty)
{
    var selector = detailsRow?.DetailsTemplateSelector
                   ?? detailsRow?.DataGridOwner?.RowDetailsTemplateSelector;
    if (selector != null)
        details.ContentTemplate = selector.SelectTemplate(
            details.Content ?? detailsRow?.Item, details);
    else
        details.ContentTemplate = detailsRow?.DetailsTemplate
            ?? detailsRow?.DataGridOwner?.RowDetailsTemplate;
}
```

Both `ContentTemplateProperty` and `ContentTemplateSelectorProperty` now resolve
through the same code path; the selector takes priority over the template.

### `src/LeXtudio.Windows/System.Windows/Controls/DataGridRow.cs`

`BuildRowDetails` gated on `owner.RowDetailsTemplate != null`; extended to also
cover the selector case:

```csharp
if (visibility == Visibility.Visible
    && (owner.RowDetailsTemplate != null || owner.RowDetailsTemplateSelector != null))
```

Without this change, the `DataGridDetailsPresenter` was never created even when
only a selector was set, so `TransferProperty` was never reached.

## Verification

```bash
dotnet test src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop --no-restore
```

Result: 140 passed, 0 failed, 0 skipped.

## Notes

- **Style setter application** remains deferred: `MetadataTableViews.Instance`
  (Roma's `RomaMetadataStubs.cs`) returns null for all keys, so `CellStyle` is
  null in practice until `MetadataTableViews.xaml` is ported to WinUI XAML.
- **`AutoGenerateColumns` chain** — confirmed end-to-end:
  `ItemsSource.set` → `SyncItemsFromSource` (Items populated) →
  `DataGrid.OnItemsSourceChanged` (upstream) → `RegenerateAutoColumns` →
  `AddAutoColumns` → fires `AutoGeneratingColumn` per property → column added
  to `Columns` → `Columns.CollectionChanged` → `BuildShimVisualTree`. The N+1
  rebuild cost (once per item batch + once per column) is acceptable given no
  virtualization.
