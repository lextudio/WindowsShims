### Session 40 - ToggleBullets/ToggleNumbering Coverage on an Existing List

Status: complete.

Scope:

- Session 39's suggested next candidate: `ToggleBullets`/`ToggleNumbering` on
  an *existing* list item (as opposed to new-list creation, which is
  `List.Apply`-gated and out of scope per Session 37).

Implementation notes:

- Read `TextEditorLists.ToggleBullets`/`ToggleNumbering` source first instead
  of exploring blind: for a top-level (non-nested) list,
  - if the item already has the matching marker style
    (`HasBulletMarker`/`HasNumericMarker`), the command calls
    `TextRangeEditLists.UnindentListItems`, which — for a top-level list —
    isolates the selected item(s) into their own list and then unwraps just
    that sublist into plain paragraphs (verified in Session 38/39 that this
    algorithm works correctly).
  - if the item has a *different* marker style, the command just sets
    `list.MarkerStyle` directly — a simple property change affecting the
    whole list, no structural change.
  - This let the two new tests be written directly from the source reading,
    without a throwaway exploratory probe first (unlike Sessions 38-39) —
    both passed on the first run.

Test-scenario/DevFlow additions:

- `RichTextBoxScenarios.BuildListDocument(TextMarkerStyle, params string[])`
  overload — lets tests build a `Decimal`-marker (numbered) list, not just
  the default `Disc` (bulleted) one.
- `richtextbox.probe.set-numbered-list-document(firstItemText, secondItemText)` —
  loads a two-item `Decimal`-marker list document.
- `richtextbox.probe.toggle-bullets-command` /
  `richtextbox.probe.toggle-numbering-command` — invoke the corresponding
  `TextEditorLists.OnListCommand` dispatch against whatever selection the
  caller already set up (no `SelectAll`, following the Session 38 pattern).

Tests added:

- `ToggleBulletsCommand_OnExistingNumberedList_ChangesMarkerStyleToDisc` —
  invoking `ToggleBullets` on an item in a `Decimal`-marker list changes the
  whole list's `MarkerStyle` to `Disc`, without changing item count or
  structure.
- `ToggleBulletsCommand_OnExistingBulletedSecondItem_RemovesItFromTheList` —
  invoking `ToggleBullets` on the (already bulleted) second item of a
  two-item `Disc` list removes just that item from the list, converting it to
  a plain paragraph and leaving the first item in its own one-item list
  (`blockCount: 2`, `firstListItemCount: 1`, text starts with `"•\tone\n"` and
  ends with `"two\n"`).

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 87/87
```

Next session:

- All list-editing candidates identified since Session 35's `Parent` fix
  (construction, indent/outdent, marker removal, bullet/numbering toggle) are
  now covered on existing lists, with only new-list *creation*
  (`List.Apply`) remaining an intentional, documented gap. Good next
  candidate: move to M2's remaining visual-behavior coverage (layout/caret
  precision, hyperlink hit-test), since the list/M3/M4 candidate lists are
  now largely exhausted.
