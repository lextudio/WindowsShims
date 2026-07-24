### Session 50 - Ctrl+A SelectAll and Clipboard Keyboard Shortcuts

Status: completed.

Scope:

- Ctrl+A (SelectAll) is a standard editing shortcut, but it has never been
  tested through the key-down path. The existing tests use
  `SelectAll()` programmatically or `key-down-select-all-modifiers` (which
  already selects all then applies a command) — but the OnKeyDown routing
  for Ctrl+A specifically is not verified.

- Ctrl+C/Ctrl+X/Ctrl+V clipboard shortcuts are similarly untested via the
  keyboard path. The clipboard probes (`copy-run-range`, `cut-run-range`,
  `paste-text-at-run-offset`) bypass `OnKeyDown` entirely. While clipboard
  access through the OS clipboard via key events has edge cases on Uno
  (platform clipboard bridging), the OnKeyDown → `ApplicationCommands.Copy`
  routing should at minimum be verified not to crash and to reach the same
  `Clipboard` API the direct probes call.

Implementation:

- `KeyDown` handling for Ctrl+A/C/X/V was **missing** in the shim's
  `RichTextBox.OnKeyDown(KeyRoutedEventArgs)`.  The existing path explicitly
  handled Ctrl+Z/Y (undo/redo) and Ctrl+B/I/U (formatting) via a switch, but
  Ctrl+A (SelectAll), Ctrl+C (Copy), Ctrl+X (Cut), and Ctrl+V (Paste) fell
  through to `TextEditorTyping.OnKeyDown` without any command routing.  Added
  a `clipboardCommand` switch after the formatting-command block that
  dispatches `ApplicationCommands.SelectAll/Copy/Cut/Paste` when the
  corresponding key is pressed with the Control modifier.

- macOS Cmd key support: `ParseVirtualKeyModifiers` now accepts `"Cmd"` or
  `"Command"` as aliases for `VirtualKeyModifiers.Windows`, and
  `ToWpfModifiers` maps `Windows` → `Control` so that WPF command key-gestures
  resolve correctly on macOS.

- Tests use `"Cmd"` as the modifier to make the macOS intent explicit.  On
  Windows, `"Control"` achieves the same effect.

Tests:

- `KeyDown_ControlA_SelectsAllDocumentText`: create-plain("select all"),
  key-down-modifiers("A", "Cmd") → selection text trimmed matches original.
- `KeyDown_ControlA_Twice_KeepsAllSelected`: repeat Ctrl+A → still selected.
- `KeyDown_ControlC_CopiesSelectionToClipboard`: Ctrl+A then Ctrl+C →
  clipboard matches document text.
- `KeyDown_ControlX_CutsSelectionToClipboard`: Ctrl+A then Ctrl+X →
  text removed from document, on clipboard.
- `KeyDown_ControlV_PastesClipboardAtCaret`: seed clipboard, then Ctrl+V →
  pasted text appears in document.
- `KeyDown_ControlAThenShiftLeft_ShrinksSelectionFromRight`: Ctrl+A, then
  Shift+Left → selection end moves left by one character (paragraph break).

Files modified:
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs` — Cmd modifier support
- `src/LeXtudio.Windows/.../RichTextBox.uno.cs` — Ctrl+A/C/X/V handlers

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Results: 136 passed, 0 skipped, 0 failed.

Next session:

- Remaining gaps: keyboard edge cases (Shift+Enter line break, Ctrl+Enter),
  real OS-level drag end-to-end via mouse pointer synthesis infrastructure.
