### Session 58 - AcceptsReturn / AcceptsTab Hidden-State Edge Cases

Status: completed.

Scope:

- Session 47 covered `AcceptsTab` for tab-key behavior. Session 51 covered
  `AcceptsReturn` for Shift+Enter/Ctrl+Enter line-break bypass. Missing:
  how `AcceptsReturn=false` affects the **visible** interaction — does the
  Enter key simply do nothing, or does it produce a visual cue (beep/block)?
  Does `AcceptsTab=false` also suppress programmatic tab insertion via
  editing commands?
- The `TextBoxBase.AcceptsReturn` property gates `EnterParagraphBreak` in
  `TextEditorParagraphs`. When false, the command is simply not executed.
  The `RichTextBox.OnKeyDown` override (session 51's fix) already checks
  `AcceptsReturn` before dispatching Enter/Shift+Enter/Ctrl+Enter — verify
  that the `AcceptsReturn=false` path also suppresses programmatic calls
  to `EditingCommands.EnterParagraphBreak.Execute`.
- Confirm that `AcceptsReturn=false` + `TextWrapping="NoWrap"` interaction
  doesn't crash or produce a stuck caret.

Implementation:

- Review `RichTextBox.uno.cs` and `TextBoxBase.cs` for all `AcceptsReturn`
  and `AcceptsTab` guard points.
- Add a `set-accepts-return` probe (may already exist from session 51) and
  `set-accepts-tab` probe.
- No deep production-code changes expected — this session is primarily test
  coverage and documentation of the current behavior.

Tests:

- `AcceptsReturnFalse_EnterKey_DoesNotInsertParagraphBreak`: set
  `AcceptsReturn=false`, press Enter, verify no new paragraph created.
- `AcceptsReturnFalse_CtrlEnter_StillInsertsParagraphBreak`: set
  `AcceptsReturn=false`, press Ctrl+Enter, verify paragraph break inserted
  (the bypass from session 51).
- `AcceptsTabFalse_TabKey_DoesNotInsertTab`: set `AcceptsTab=false`, press
  Tab, verify no tab character inserted.
- `AcceptsTabFalse_ProgrammaticTabCommand_StillWorks`: set
  `AcceptsTab=false`, invoke `EditingCommands.TabForward` programmatically,
  verify tab inserted (gate only applies to raw key input, not commands).

Files modified:

- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` — add
  `set-accepts-tab` probe if not already present.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- Mixed FlowDirection edge cases.
