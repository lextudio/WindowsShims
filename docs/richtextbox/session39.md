### Session 39 - RemoveListMarkers Coverage on an Existing List

Status: complete.

Scope:

- Session 38's suggested next candidate: `RemoveListMarkers`
  (`ConvertListItemsToParagraphs`) coverage on an existing list item, using
  the non-`SelectAll` probe pattern established that session.

Implementation notes:

- `EditingCommands.RemoveListMarkers` is `internal` (unlike `ToggleBullets`/
  `ToggleNumbering`/`IncreaseIndentation`/`DecreaseIndentation`, which are
  `public`), so the probe resolves it via reflection
  (`typeof(EditingCommands).GetProperty("RemoveListMarkers", ...)`) rather
  than a direct alias reference.

DevFlow additions:

- `richtextbox.probe.select-first-list-item(start, length)` — selects a range
  inside the *first* `ListItem`'s `Run` (mirrors Session 38's
  `select-second-list-item`), needed because
  `TextRangeEditLists.ConvertListItemsToParagraphs` behaves differently
  depending on whether the target item has a `PreviousListItem`.
- `richtextbox.probe.remove-list-markers-command` — invokes
  `TextEditorLists.OnListCommand` with `EditingCommands.RemoveListMarkers`
  against whatever selection the caller already set up (no `SelectAll`).

Tests added (both explored first with a throwaway probe, then locked in with
real assertions once the resulting structure was understood — same
methodology as Session 38):

- `RemoveListMarkersCommand_OnFirstItemWithNoLeadingItem_ConvertsItToPlainParagraph` —
  removing markers from the first item of a two-item list (no
  `PreviousListItem` to merge into) converts just that item into a plain
  top-level `Paragraph`, leaving the second item as its own one-item `List`.
  Result: `blockCount: 2`, `firstBlockType: "Paragraph"`, text starts with
  `"one\n"` and still contains `"•\ttwo"`.
- `RemoveListMarkersCommand_OnSecondItemWithLeadingItem_MergesItAsExtraParagraphInFirstItem` —
  removing markers from the second item (which *does* have a leading item)
  merges its content into the first item as an additional, marker-less
  `Paragraph` block rather than creating a second top-level block. Result:
  `blockCount: 1`, `firstListItemCount: 1`,
  `firstListItemBlockTypes: "Paragraph,Paragraph"`, text is exactly
  `"•\tone\ntwo\n"`.

Both cases needed no product code changes — `ConvertListItemsToParagraphs`
already works correctly given Session 35's `LogicalTreeHelper` fix, matching
the pattern of Sessions 37-38 (list algorithms are sound; only construction
and test-probe setup needed fixing).

Regression sweep:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet test tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet run --project src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop -- --labels=Before
```

Result:

```text
RichTextBox.IntegrationTests: 85/85
DataGrid.IntegrationTests: 52/53 (1 pre-existing skip)
LeXtudio.Windows.Tests (NUnitLite): 233/233
```

Next session:

- List/ListItem editing on existing lists (construction, indent/outdent,
  marker removal) is now well covered. Good next candidates: `ToggleBullets`/
  `ToggleNumbering` on an *existing* list item (removing/changing markers on
  content that's already list-formatted, as opposed to the
  `List.Apply`-gated new-list-creation path tested in Session 37), or move on
  to M2's remaining visual-behavior coverage (layout/caret precision,
  hyperlink hit-test).
