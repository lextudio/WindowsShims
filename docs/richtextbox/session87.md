### Session 87 - RTF Round-Trip for List MarkerStyle / StartIndex

Status: completed.

Scope:

- Sessions 81-86 closed character-, paragraph-, and inline-level RTF round-trips.
  This session makes list semantics survive `DataFormats.Rtf`: the bullet/number
  marker style and a non-default numbering start index are preserved.

Root cause found:

- The RTF chain handled list formatting on both sides — `XamlToRtfWriter` parses
  `<List MarkerStyle>` / `<List StartIndex>` (`XAMarkerStyle`/`XAStartIndex`,
  which write the RTF list level and `\pnstart`) and `RtfToXamlReader` restores
  them as `<List MarkerStyle="..." StartIndex="...">` attributes
  (`AppendXamlPrefixListProperties`). The shim's `XamlReader.ParseList` only read
  the `<ListItem>` children, so the attributes were silently dropped and a
  bulleted/numbered list came back unmarked.

Notes / decisions:

- `MarkerStyle` maps through WPF's `TextMarkerStyle` (the reader's
  `MarkerStyleToString` returns exactly those enum names, e.g. `Disc`,
  `Decimal`, `UpperRoman`), so `ParseList` uses `Enum.TryParse<TextMarkerStyle>`.
- `StartIndex` is parsed with `int.TryParse`; the RTF writer emits `\pnstart`
  and the reader only emits `StartIndex` when it differs from the default (`1`).
- The snapshot gained `firstListStartIndex` so the numbering start is observable.

Tests:

- Integration (2 new, 216/216 total):
  - `SaveLoad_Rtf_RoundTripsBulletListMarker` — a `MarkerStyle="Disc"` two-item
    list survives as `Disc` (item count preserved).
  - `SaveLoad_Rtf_RoundTripsNumberedListMarkerAndStart` — a
    `MarkerStyle="Decimal" StartIndex="3"` two-item list survives as `Decimal`
    starting at `3`.
- Model tests: 234/234 (`LeXtudio.Windows.Tests`).

Files modified:

- `src/LeXtudio.Windows/System.Windows/Markup/XamlReader.cs` (`ParseList` applies `MarkerStyle` and `StartIndex`)
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` (`firstListStartIndex` snapshot field)
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` (2 new tests + `FirstListStartIndex` helper)
- `docs/richtextbox/index.md`, `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` (counts/status)

Result:

- 216/216 RichTextBox integration tests pass.
- 234/234 model tests pass.
- RTF save/load now preserves list marker style and numbering start index.
