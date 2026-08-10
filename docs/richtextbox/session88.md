### Session 88 - RTF Round-Trip Coverage for Hyperlink NavigateUri + Nested Lists

Status: completed.

Scope:

- Sessions 81-87 established the RTF formatting round-trip surface. This session
  is a coverage pass that verifies two previously untested paths end-to-end and
  pins them against regression: hyperlink `NavigateUri` and nested-list content.

Findings:

- **Hyperlink `NavigateUri` already round-trips.** The whole chain was wired but
  untested: `XamlToRtfWriter` emits `{\field{\*\fldinst { HYPERLINK "..."}}}`,
  `RtfToXamlReader` parses the field back to `<Hyperlink NavigateUri="...">`
  (`AppendXamlPrefixHyperlinkProperties`), and the shim's `ParseHyperlink` reads
  the attribute. No production change needed — only the missing test coverage.
- **Nested lists already round-trip.** Both list levels and all text (a nested
  item inside the first item plus a second sibling item) survive RTF save/load.
  Verified structurally (`nestedListMarkerStyle`/`nestedListItemCount`) and by
  full-document text. No production change needed.

Notes / decisions:

- The snapshot gained `firstHyperlinkNavigateUri`, computed by walking the first
  paragraph's inline tree for the first `Hyperlink` (`FindHyperlinkUri`). This
  required no new test-host probe — only a snapshot field.
- A suspected "nested-list text loss" turned out to be a diagnostic artifact:
  `TextRange.Text` contains `\n` between list items, so a line-based grep of the
  assertion message truncated the output after the first item. The full text was
  intact.

Tests:

- Integration (2 new, 218/218 total):
  - `SaveLoad_Rtf_RoundTripsHyperlinkNavigateUri` — the URI survives the
    `\field`/`HYPERLINK` round-trip exactly (`FirstHyperlinkNavigateUri`).
  - `SaveLoad_Rtf_RoundTripsNestedListText` — alpha, nested, and beta all
    survive; both list levels and their marker styles are preserved.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Files modified:

- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` (`firstHyperlinkNavigateUri` snapshot field + `FindFirstHyperlinkNavigateUri`/`FindHyperlinkUri` helpers)
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` (2 new tests + `FirstHyperlinkNavigateUri` helper)
- `docs/richtextbox/index.md`, `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` (counts/status)

Result:

- 218/218 RichTextBox integration tests pass.
- 234/234 model tests pass.
- Hyperlink `NavigateUri` and nested-list content are verified (and pinned) as
  surviving RTF save/load.
