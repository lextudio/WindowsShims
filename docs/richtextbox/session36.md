### Session 36 - Real Character-Received Input Path Coverage

Status: complete.

Scope:

- Close the last open M3 item noted repeatedly since Session 9: cover typing
  (including selection replacement) through the real Uno input path
  (`RichTextBox.OnCharacterReceived`) instead of only calling
  `TextEditorTyping.OnTextInput(...)` directly.

Implementation notes:

- `Microsoft.UI.Xaml.Input.CharacterReceivedRoutedEventArgs` has an internal
  constructor `(char, CorePhysicalKeyStatus)`, found by a throwaway DevFlow
  probe that reflected its constructor list before writing the real one (same
  approach used for `KeyRoutedEventArgs` in Session 7).
- `RichTextBox.OnCharacterReceived(...)` is protected, so the probe resolves it
  by reflection the same way `OnKeyDown`/`OnKeyUp` are invoked.

DevFlow additions:

- `richtextbox.probe.character-received(text)` — focuses the current
  RichTextBox and, for each character in `text`, constructs a real
  `CharacterReceivedRoutedEventArgs` and invokes
  `RichTextBox.OnCharacterReceived(...)`, which forwards into
  `TextEditorTyping.OnTextInput(...)` exactly as production Uno input would.

Tests added:

- `CharacterReceived_MutatesDocumentThroughRealUnoInputPath` — typing `"abc"`
  through the real character-received path inserts it into the document.
- `CharacterReceived_ReplacesSelectedText` — selecting all of `"old text"`
  (via `select-run-range`) then typing `"new"` through the real
  character-received path replaces the selection, leaving `"new"` and
  removing `"old text"`.

Verified behavior:

- Both cases pass through the actual production input entry point rather than
  the lower-level `TextEditorTyping.OnTextInput` shortcut used by earlier
  sessions' `text-input-event` probe.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 78/78
```

Next session:

- With M3 and M4's "Done when" bars both met, and the Session 35 foundational
  `Parent` fix in place, good candidates are: an audit pass over list/table
  editing code paths (`TextEditorLists`, `TextRangeEditTables`) that may now
  behave correctly for the first time given working `TextElement.Parent`
  chains, or moving into M2's remaining visual-behavior coverage (layout/caret
  precision, hyperlink hit-test) if a consumer needs it next.
