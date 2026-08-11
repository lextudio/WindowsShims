### Session 103 - Pagination Carries Table/Paragraph Boxes Across Pages

Status: completed.

Scope:

- `FormatPages` (used by `FlowDocumentPaginator`) split lines into pages but
  dropped cell boxes, paragraph borders, and background fills, and left each
  line's absolute layout Y untouched — page 2+ content rendered at
  out-of-page coordinates and no box visuals appeared at all.

Findings:

- Pages were built by re-homing lines into fresh `FlorencePage` objects, but
  the re-base used the *next* page's start Y (the `pageStartY` was advanced
  before flushing), producing negative line Ys, and the page's box lists were
  never populated.
- WPF splits a box that crosses a page boundary into per-page segments; the
  fix mirrors that.

Changes:

- `FlorenceEngine.cs` (`FormatPages`):
  - Lines are re-based to page coordinates (Y and baseline minus the page's
    start Y) using the *current* page's start; the start advances only after
    a page is flushed.
  - Page Y ranges are recorded per page; `DistributeBox` assigns each cell
    box, paragraph border, and background fill to every page it overlaps,
    re-basing Y and splitting boxes that cross a boundary (heights preserved).
- `MainPage.cs`: new `get-page-layout` probe reporting per-page line Y
  ranges and cell box bounds from the paginator.

Tests:

- Integration (1 new, 235/235 total):
  - `FlowDocument_PaginationCarriesTableBoxesAcrossPages` — a 30-row bordered
    table paginates over 640x100 pages; every page's line Y range is within
    the page, every page carries at least one cell box, and the total box
    height across pages equals the 30 rows' heights (boundary splits
    preserve the total).
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Result:

- 235/235 RichTextBox integration tests pass; 234/234 model tests pass.
- Paginated output now carries table cell boxes, paragraph borders, and
  background fills, with per-page coordinates and boundary-split boxes.
