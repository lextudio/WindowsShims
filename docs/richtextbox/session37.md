### Session 37 - List/ListItem Construction Crash Found and Fixed; List Command Scope Documented

Status: complete.

Scope:

- Follow up on Session 35's recommendation to audit `TextElement.Parent`-dependent
  code paths now that `LogicalTreeHelper` correctly maintains `Parent`. Chose
  list handling (`TextEditorLists`, `TextRangeEditLists`) since
  `TextEditorLists.OnListCommand` reads `(List)parentListItem.Parent` directly,
  and `TextRangeEditLists.IndentListItems` gates on
  `firstListItem.Parent != lastListItem.Parent` / `firstListItem.Parent is List`
  — exactly the kind of check that used to silently no-op before Session 35.

Finding 1 — list command creation is intentionally unsupported:

- `TextEditorLists.OnListCommand` → `ToggleBullets`/`ToggleNumbering` (and
  `IncreaseIndentation`/`DecreaseIndentation` on a plain paragraph) route
  through `TextRangeEditLists.ConvertParagraphsToListItems` →
  `List.Apply(...)`, which has an explicit `#if HAS_UNO` guard:
  `throw new NotSupportedException("List.Apply requires the WPF text tree and
  is not available in UnoRichText yet.")`. This is a deliberate, already-documented
  scope boundary — the same pattern as Session 34's XAML/RTF serialization gap
  — not a bug to fix in this session.

Finding 2 — ListItem construction crashed (a real, previously-undiscovered bug):

- To test indent/outdent on an *already-existing* list (bypassing `List.Apply`
  entirely), added `RichTextBoxScenarios.BuildListDocument(...)`, constructing
  a `List`/`ListItem`/`Paragraph`/`Run` tree directly via public constructors.
  This alone crashed with `NullReferenceException` before any list command was
  invoked.
- Root cause: this shim's `TextElementCollection<T>.HydrateRunText` (a
  Uno-specific bridge that eagerly writes a `Run`'s `Run.TextProperty` value
  into the text tree on `Add`, since Uno's property system can't resolve
  WPF's lazy `DeferredRunTextReference` proxy) used
  `new TextRange(run.ContentStart, run.ContentEnd).Text` as its "is this Run
  already hydrated?" check. That getter goes through the full plain-text
  serializer (`TextRangeBase.GetTextInternal`), which — once the Run's
  Paragraph is added to a `ListItem`'s `Blocks` — invokes
  `PlainConvertListItemStart` to compute the list marker. That method calls
  `listItem.SiblingListItems`, which upstream WPF's `ListItem.SiblingListItems`
  itself returns `null` for whenever `ListItem.Parent == null` — true for any
  `ListItem` not yet added to a `List`, a normal, transient state while
  building one via `new ListItem(new Paragraph(new Run(text)))`.
  `PlainConvertListItemStart` then null-derefs calling
  `((IList)listItem.SiblingListItems).IndexOf(listItem)`.
- This is a shim-specific bug: real WPF's `Run.Text` getter never goes through
  `GetTextInternal`/list-marker computation at all (it just returns a cached
  string), so real WPF never hits this path from a plain text read. Only our
  own `HydrateRunText` bridge does, and only because it (unnecessarily) uses
  the heavyweight, list-aware `TextRange.Text` getter for a check that has
  nothing to do with list markers.

Product fix:

- `TextElementCollection.uno.cs`'s `HydrateRunText` now checks
  `run.ContentStart.GetOffsetToPosition(run.ContentEnd) > 0` instead of
  `new TextRange(run.ContentStart, run.ContentEnd).Text` — a raw symbol-count
  check that never invokes the plain-text/list-marker serializer, so it can't
  crash on an unparented `ListItem`.

DevFlow/test-infrastructure additions:

- `RichTextBoxScenarios.BuildListDocument(params string[] itemTexts)` — builds
  a `FlowDocument` containing a `List` with N `ListItem`s, bypassing
  `List.Apply`.
- `richtextbox.probe.set-list-document(firstItemText, secondItemText)` —
  loads a two-item list document into the RichTextBox.
- `richtextbox.probe.select-second-list-item(start, length)` — selects a
  range inside the second `ListItem`'s `Run`, for future list-command-on-existing-list
  test coverage.
- `richtextbox.probe.toggle-bullets-selection-command`,
  `toggle-numbering-selection-command`, `increase-indentation-selection-command`,
  `decrease-indentation-selection-command` — invoke the corresponding
  `TextEditorLists.OnListCommand` dispatch for the current selection.
- Snapshot additions: `firstBlockType`, `firstListMarkerStyle`,
  `firstListItemCount`, `firstListItemText`.

Tests added:

- `SetListDocument_BuildsListWithoutCrashing` — building and rendering a
  two-item list document succeeds; snapshot reports `firstBlockType: "List"`,
  `firstListMarkerStyle: "Disc"`, `firstListItemCount: 2`, and plain-text
  readback is `"•\tone\n•\ttwo\n"`.
- `ToggleListCommand_OnPlainParagraphs_FailsPredictablyUnderUno` (`ToggleBullets`,
  `ToggleNumbering`) — invoking either on a plain paragraph throws
  `NotSupportedException` predictably, matching the documented `HAS_UNO` gate.

Explicitly deferred (not chased further this session):

- `IncreaseIndentation`/`DecreaseIndentation` on an *existing* list item showed
  confusing behavior when exploratorily probed: the reported selection
  expanded to the entire document's text instead of nesting just the target
  item, and the snapshot's list-shape fields didn't change in an
  interpretable way. This needs either better indent-aware snapshot fields
  (e.g. nesting depth, parent-list-of-list-item) or a closer read of
  `TextRangeEditLists.IndentListItems`/`UnindentListItems` before writing
  assertions — left as a follow-up rather than guessing at the right
  behavior.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet test tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet run --project src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop -- --labels=Before
```

Result:

```text
LeXtudio.Windows: 0 errors
RichTextBox.IntegrationTests: 81/81
DataGrid.IntegrationTests: 52/53 (1 pre-existing skip)
LeXtudio.Windows.Tests (NUnitLite): 233/233
```

Next session:

- Investigate `TextRangeEditLists.IndentListItems`/`UnindentListItems`
  behavior on an existing (non-`List.Apply`-created) list closely enough to
  write correct assertions — likely needs list-nesting-aware snapshot fields
  first.
- Otherwise, continue with M2's remaining visual-behavior coverage
  (layout/caret precision, hyperlink hit-test) if a consumer needs it next.
