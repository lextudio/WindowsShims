### Session 63 - Table Arrow-Key Navigation

Status: completed.

Scope:

- Session 43 verified table construction and the document model reads back
  correctly. Cursor navigation across table cells via arrow keys has not
  been verified.
- WPF's `TextEditorTables` handles `TabForward`/`TabBackward` and arrow-key
  movement across `TableCell` boundaries. The upstream source is linked and
  compiled. Verify it works on the shim.
- Arrow keys at a cell boundary should move the caret to the next/previous
  cell. Up/Down arrows at the first/last cell should leave the table.
- Tab moves to the next cell (creating a new row if at the last cell).
  Shift+Tab moves to the previous cell.

Implementation:

- Create a table document via `set-table-document`.
- Use `set-caret-run-offset` or `select-text-range` to place the caret
  inside a specific cell.
- Use `key-down` to simulate arrow keys and verify caret position changed
  to the expected cell.
- If cell-boundary navigation doesn't work, investigate the
  `TextEditorTables` handler (`OnArrowKey` or similar) and fix any
  `#if HAS_UNO` guard or missing infrastructure.

Tests:

- `TableArrowKey_RightArrowMovesToNextCell`: create 2x2 table, place caret
  in cell (0,0), press Right, verify caret in cell (0,1).
- `TableArrowKey_DownArrowMovesToNextRow`: place caret in cell (0,0), press
  Down, verify caret in cell (1,0).
- `TableArrowKey_TabMovesToNextCellCreatesRowAtEnd`: Tab from last cell
  creates a new row.
- `TableArrowKey_LeftArrowAtFirstCellMovesBeforeTable`: Left arrow from
  cell (0,0) moves caret before the table.

Files modified:

- `src/LeXtudio.Windows/.../Documents/TextEditorTables.cs` — fix arrow-key
  navigation if broken.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- Document save/load round-trip.
