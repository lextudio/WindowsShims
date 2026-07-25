### Session 66 - Flowing Document Pagination / Printing

Status: completed.

Scope:

- `FlowDocumentPaginator` is a stub (`IsPageCountValid => false`,
  `PageCount => 0`, `GetPage` not implemented). RichTextBox doesn't paginate,
  so content simply overflows the visible area.
- Basic pagination: split the `FlorencePage` lines into pages at a fixed
  page height, implement `FlowDocumentPaginator.GetPage`, and wire up
  `DocumentPaginator.PageCount` / `PageSize`.

Implementation:

- In `FlorenceLayoutEngine.Format`, accept an optional `pageHeight`
  parameter. When set, emit one `FlorencePage` per page (break at line
  boundaries before exceeding the page height).
- Implement `FlowDocumentPaginator.GetPage(int pageNumber)` to return a
  `DocumentPage` built from the corresponding `FlorencePage`.
- Wire `RichTextBox`'s internal `DocumentPaginator` (used by
  `FlowDocument.PageSize` / `PageCount`) to the real Florence paginator.

Tests:

- `FlowDocument_PageCount_ReflectsContentHeight`: create a tall document,
  verify `pageCount` is proportional to content height.
- `FlowDocument_GetPage_ReturnsPageWithContent`: get a specific page,
  verify it contains the expected text range.
- `FlowDocument_PageSize_AdjustsPageCount`: change `PageSize`, verify
  `PageCount` changes accordingly.

Files modified:

- `src/LeXtudio.Windows/MS.Internal/Florence/FlorenceEngine.cs` — add
  pagination parameter.
- `src/LeXtudio.Windows/.../Documents/FlowDocumentPaginator.cs` — implement
  `GetPage`, `PageCount`, `IsPageCountValid`.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- InlineUIContainer / BlockUIContainer embedding.
