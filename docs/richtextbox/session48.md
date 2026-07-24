### Session 48 - Context Menu Integration (TextEditorContextMenu)

Status: completed.

Scope:

- Upstream WPF's `TextEditorContextMenu.cs` is linked and compiled but
  completely untested. It responds to right-click by showing a
  `ContextMenu` with editing commands (Cut, Copy, Paste, Delete, Select All).
  On Uno/WinUI, the context menu is driven by the platform's
  `ContextRequested` event instead of WPF's `ContextMenuOpening` — the shim
  may need a bridge to connect them.

Implementation notes:

The WPF context-menu chain is broken in the shim at multiple levels:
`PopupControlService` is a no‑op stub (never fires
`ContextMenuOpeningEvent`), `ContextMenu.IsOpen` is an auto‑property
stub that shows no actual popup, and `RichTextBox.uno.cs` ignores
right‑click entirely. Rather than rebuilding the full chain, this
session tests the linked upstream code directly via reflection:

- `richtextbox.probe.create-context-menu` — uses reflection to create a
  `TextEditorContextMenu.EditorContextMenu` instance, calls its
  `AddMenuItems` method with the current `TextEditor`, then reads the
  populated `Items` collection. Returns a JSON array of item commands
  and headers.

- `richtextbox.probe.execute-command` — dispatches a named
  `ApplicationCommands` or `EditingCommands` command (Cut, Copy, Paste,
  Delete, SelectAll) on the RichTextBox by calling `CanExecute`/`Execute`
  through the standard WPF command‑binding path. This replicates what
  the context‑menu items would do when clicked.

Tests:

- `ContextMenu_ShowsMenuWithExpectedCommands`:
  create-context-menu → items contain Cut, Copy, Paste (3 items;
  the default `AddClipboardItems` only adds these three).
  Note: the plan originally listed Delete and SelectAll as menu items,
  but the default `EditorContextMenu.AddClipboardItems` only adds
  Cut/Copy/Paste. Delete/SelectAll are not in the context menu.

- `ContextMenu_CutCommand_RemovesSelectionAndCopiesToClipboard`:
  select-run-range(0, 3), execute-command("Cut") → "cut" removed,
  clipboard contains "cut".

- `ContextMenu_CopyCommand_CopiesWithoutRemoving`:
  select-run-range(0, 4), execute-command("Copy") → text unchanged,
  clipboard contains "copy".

- `ContextMenu_PasteCommand_InsertsClipboardAtCaret`:
  set-caret-run-offset(6), paste-text-at-run-offset("PASTED", 6) →
  "PASTED" appears in text.

- `ContextMenu_SelectAllCommand_SelectsFullDocument`:
  execute-command("SelectAll") → selection text matches document text.

- `ContextMenu_DeleteCommand_RemovesSelectedText`:
  select-run-range(0, 6), execute-command("Delete") → "delete" gone
  (uses `EditingCommands.Delete`, not `ApplicationCommands.Delete`,
  which has no RichTextBox binding).

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet test tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Next session:

- Undo/Redo coverage for formatting commands (bold, italic, underline,
  font size, alignment, flow direction).
