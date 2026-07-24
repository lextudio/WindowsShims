### Session 47 - AcceptsTab / Tab-Key Behavior Coverage

Status: completed.

The upstream WPF command-routing path for Tab (`OnTabForward`/`OnTabBackward`)
checks `UIElement.IsKeyboardFocused`, which depends on `Keyboard.FocusedElement`.
In the current shim, `Keyboard.FocusedElement` always returns `null`, so
`IsKeyboardFocused` is always `false` and `OnTabForward` exits early.

This session worked around the gap by handling Tab directly inside
`RichTextBox.uno.cs`'s `OnKeyDown` override, bypassing the WPF command system
entirely:

- Removed `Key.Tab` from `GetNavigationCommand` (the command path doesn't work
  due to the `IsKeyboardFocused` check).
- Added inline Tab handling before `TextEditorTyping.OnKeyDown`: if `AcceptsTab`
  is true and no Shift modifier, calls `TextEditorTyping.DoTextInput` via
  reflection to insert `"\t"`, then marks `e.Handled = true` and updates caret.
- For RichTextBox (`AcceptsRichContent = true`), the `_FilterText` path inside
  `DoTextInput` bypasses the `AcceptsTab` check entirely, so `\t` passes
  through regardless of `AcceptsTab`'s value at the text-editor level.

Probes added:

- `richtextbox.probe.set-accepts-tab` — sets `AcceptsTab` on the current
  RichTextBox to true/false and returns state snapshot.

Tests added (all pass, 116/116):

- `AcceptsTab_WhenTrue_InsertsTabCharacter`: sets `AcceptsTab = true`,
  positions caret, sends Tab → `"a\tbc"`.
- `AcceptsTab_WhenFalse_DoesNotInsertTabCharacter`: sets `AcceptsTab = false`,
  Tab → no tab inserted.
- `AcceptsTab_ShiftTab_DoesNotInsertTabCharacter`: Shift+Tab with default
  `AcceptsTab` → no tab inserted.
- `AcceptsTab_AfterEnterInNewParagraph`: Enter, sets `AcceptsTab = true`,
  Tab, then `"def"` → second paragraph starts with `"\tdef"`.

Note on `AcceptsTab` default: WPF registers the DP with default `false` and
RichTextBox does not override the metadata. In practice the default style
likely sets it to `true`. The tests that need Tab insertion explicitly set
`AcceptsTab = true` to stay unambiguous.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet test tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Next session:

- Context menu integration (`TextEditorContextMenu.cs` is linked but
  untested).
