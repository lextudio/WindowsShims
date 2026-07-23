### Session 30 - Word Navigation Coverage

Status: complete.

Scope:

- Add DevFlow-driven integration coverage for WPF word navigation commands via
  the real Uno `RichTextBox.OnKeyDown(...)` forwarding path.
- Reuse explicit first-run caret placement and relative selection offsets from
  the previous navigation sessions.

Verified behavior:

- `Ctrl+Right` from inside the second word of `one two three` moves to the next
  word boundary reported by WPF word breaking.
- `Ctrl+Left` from the same position moves to the start boundary of the current
  word.
- `Ctrl+Shift+Right` and `Ctrl+Shift+Left` extend selection according to WPF
  `TextPointerBase.MoveToNextWordBoundary(...)` behavior.
- The assertions preserve WPF's actual word-boundary semantics rather than
  assuming a simple "current word end" model; from offset 5,
  `Ctrl+Right` lands at offset 8 and `Ctrl+Shift+Right` selects `w`.

Commands:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~KeyDown_ControlLeftRight|FullyQualifiedName~KeyDown_ControlShiftLeftRight"
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 4/4 targeted
Passed: 62/62 full RichTextBox integration suite
```

Next session:

- Cover Ctrl+Backspace/Ctrl+Delete word deletion, starting from explicit
  first-run caret offsets and asserting the resulting text plus caret position.
