### Session 51 - Keyboard Line-Break Edge Cases (Shift+Enter and Ctrl+Enter)

Status: completed.

Scope:

- `EditingCommands.EnterParagraphBreak` (plain Enter) is tested
  (`KeyDown_Enter_InsertsParagraphBreak`). `EditingCommands.EnterLineBreak`
  (Shift+Enter) is mapped in `GetNavigationCommand()` but had **no test**.
  Added test verifying that Shift+Enter inserts a `LineBreak` element.

- Ctrl+Enter bypasses `AcceptsReturn` in WPF. Added a handler in `OnKeyDown`
  that directly dispatches `EnterParagraphBreak` or inserts a new paragraph
  when `AcceptsReturn` is false.

Implementation:

- **Shift+Enter** — existing mapping `(Key.Return, true, _)` →
  `EnterLineBreak` in `GetNavigationCommand` already worked. Added test
  verifying that the inline tree contains `LineBreak` after the key event.

- **Ctrl+Enter handler** — added in the Ctrl+modifier block of `OnKeyDown`
  (after the clipboard-command switch). When `CanExecute` succeeds (i.e.,
  `AcceptsReturn` is true), the `EnterParagraphBreak` command is dispatched
  normally. When `CanExecute` fails (`AcceptsReturn` is false), a new
  `Paragraph` is inserted directly via `SiblingBlocks.InsertAfter`, mirroring
  the `#if HAS_UNO` path in `TextEditorTyping.HandleEnterBreakForRichText`.

- **`set-accepts-return` probe** — added to `MainPage.cs` (mirrors the
  existing `set-accepts-tab` probe) so tests can toggle `AcceptsReturn`.

Tests:

- `KeyDown_ShiftEnter_InsertsLineBreak`: create-plain("a"), caret at end,
  Shift+Enter → inline tree contains `LineBreak`, text contains "a\n".
- `KeyDown_CtrlEnter_InsertsParagraphBreak`: create-plain("abc"), caret at
  offset 1, Ctrl+Enter → `BlockCount >= 2`.
- `KeyDown_Enter_WhenAcceptsReturnFalse_DoesNotInsertBreak`: AcceptsReturn
  set false, plain Enter → no new paragraph (`BlockCount == 1`).
- `KeyDown_CtrlEnter_BypassesAcceptsReturn`: AcceptsReturn set false,
  Ctrl+Enter → paragraph break still inserted (`BlockCount >= 2`).

All 4 tests pass (total: 140/140).

Files modified:

- `src/LeXtudio.Windows/.../RichTextBox.uno.cs` — Ctrl+Enter handler in
  `OnKeyDown` with `AcceptsReturn` bypass.
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` — `set-accepts-return`
  probe.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — 4 new
  tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
```
Build succeeded — 0 errors.

```text
dotnet build tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj -p:BuildRichTextBoxIntegrationTestHost=false
dotnet run --project tests/RichTextBox.IntegrationTestHost -f net10.0-desktop &
tests/RichTextBox.IntegrationTests/bin/Debug/net10.0/RichTextBox.IntegrationTests
```
Results: 140 passed, 0 skipped, 0 failed.

Next session:

- Real OS-level drag end-to-end via mouse pointer synthesis infrastructure.
