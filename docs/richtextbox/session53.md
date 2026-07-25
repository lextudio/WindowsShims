### Session 53 - List Creation (`List.Apply`)

Status: completed.

Scope:

- Sessions 37-40 built comprehensive coverage for editing an **existing** list
  (indentation, marker removal, bullet/number toggling). All of those work
  correctly and are tested.

- `List.Apply` (the WPF method that converts a set of plain paragraphs into
  a new `List`) is the remaining gap — it currently throws
  `NotSupportedException` when called under `HAS_UNO`. This means there is no
  way to create a list from scratch through the normal WPF editing command
  path (`EditingCommands.ToggleBullets` on a plain paragraph → calls
  `ListCommands.ConvertParagraphsToListItems` → calls `List.Apply`).

- The toggle-bullets/numbering tests in session 40 bypass this by using
  `set-list-document` / `set-numbered-list-document` probes that construct the
  list directly without going through `List.Apply`. This session makes the
  real command path work.

Implementation:

- Investigate what `List.Apply` does in upstream WPF: it creates a new `List`
  element, wraps selected paragraphs into `ListItem` children, and inserts the
  `List` into the document's `BlockCollection` at the correct position. The
  `NotSupportedException` comment says "requires the WPF text tree" — the
  question is whether this shim's `LogicalTreeHelper` and `TextContainer`/
  `TextPointer` infrastructure now satisfy that requirement (session 35's
  `LogicalTreeHelper` fix and the real `BlockCollection`/`TextContainer`
  already in place may be sufficient).

- Likely approach: remove the `throw NotSupportedException` guard in
  `List.Apply` (ext/wpf copy) and see if the existing WPF implementation
  works on this shim. If it crashes, fix the specific gap (likely a
  `TextContainer` API or tree-walk assumption that differs under `HAS_UNO`).

- Risk: `List.Apply` may depend on internal WPF tree mutation APIs that the
  shim's `TextContainer` does not fully replicate. If the issue is
  fundamental, document the limitation and add a shim-specific
  `ConvertParagraphsToList` helper that constructs the list manually (similar
  to what the `set-list-document` probe already does, but generalized for
  the command path).

Tests:

- `ToggleBulletsCommand_OnPlainParagraph_CreatesNewList`: existing test
  `ToggleListCommand_OnPlainParagraphs_FailsPredictablyUnderUno` currently
  verifies the `NotSupportedException`. Change its expectation to succeed,
  or add a new test that expects list creation to work.

- `ToggleNumberingCommand_OnPlainParagraph_CreatesNewNumberedList`: same
  pattern, but with `ToggleNumbering`.

- `ListApply_RoundTripsThroughDocumentModel`: create list via `List.Apply`,
  verify `BlockCollection` contains a `List` with correct `ListItem` children
  and text content.

Files modified:

- `ext/wpf/src/.../Documents/List.cs` — remove or modify the
  `NotSupportedException` guard in `List.Apply`.
- Possibly `src/LeXtudio.Windows/.../Documents/` — add shim-specific list
  construction helper if the WPF path is not viable.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` —
  update expectations.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Next session:

- IME composition visual underline/highlight.
