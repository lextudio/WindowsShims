### Session 49 - Undo/Redo Coverage for Formatting Commands

Status: completed (with documented limitation).

Scope:

- The existing `UndoRedo_RestoresTextInputMutation` test proves undo/redo
  works for text-insertion via `Undo`/`Redo` commands. But formatting
  commands (bold, italic, underline, font size, font family, foreground,
  background, alignment, flow direction) all push their own undo units — each
  should individually support undo/redo to restore not just text content but
  also property values.

- This session adds undo/redo tests for each formatting category, verifying
  that the formatting property reverts to its pre-application value on undo
  and reapplies on redo.

- **Key finding:** formatting undo was fundamentally unsupported in the current
  shim because `TextElement.OnPropertyChanged` (which records `PropertyUndoUnit`
  via `TextTreeUndo.CreatePropertyUndoUnit`) is compiled out with
  `#if !HAS_UNO`, so property changes on `TextElement` never create undo units.

- **Fix applied:** added `#if HAS_UNO` hooks in two places:

  1. `TextRangeEdit.SetPropertyValue` — calls
     `element.NotifyTypographicPropertyChanged(...)` and
     `TextTreeUndo.CreatePropertyUndoUnit(element, args)` after every property
     value change on a `TextElement` (Run, Span, Paragraph, etc.). This records
     the initial undo unit when a formatting command applies its change.

  2. `TextContainer.SetValue` — records the property undo unit before/after
     calling `textElement.SetValue()`. This ensures that when `TextTreeProperty-
     UndoUnit.DoCore()` restores the old value during Undo, the counter-change
     is also recorded as a new undo unit for Redo.

  Both hooks create a `DependencyPropertyChangedEventArgs` with `OldValue`
  captured before the `SetValue` call and `OldValueSource = Local`, matching
  what WPF's `TextElement.OnPropertyChanged` would have produced.

  Files modified:
  - `ext/wpf/src/.../Documents/TextRangeEdit.cs` (SetPropertyValue)
  - `ext/wpf/src/.../Documents/TextContainer.cs` (SetValue)

- **All 8 formatting undo tests now pass** — the skip attributes were removed
  and the alignment test was fixed to use `AlignCenter` (not the no-op `Align-
  Left` which is the default).

Implementation:

- Add `richtextbox.probe.undo` probe if one doesn't exist already — drives
  `EditingCommands.Undo` / `RichTextBox.Undo()`.
- Add `richtextbox.probe.redo` probe similarly.
- (The probes already exist from session 21; no new actions needed.)

- **Probe migration**: all formatting probes that previously invoked WPF
  command handlers directly via `InvokeTextEditorCharacters` were migrated to
  use `RoutedCommand.Execute(parameter, target)` through the standard WPF
  command routing. This ensures `CanExecute` is checked and the command
  binder executes the handler. The affected probes are:

  - `toggle-bold-selection-command` — `EditingCommands.ToggleBold.Execute`
  - `toggle-italic-selection-command` — `EditingCommands.ToggleItalic.Execute`
  - `toggle-underline-selection-command` — `EditingCommands.ToggleUnderline.Execute`
  - `toggle-bold-run-range-command` — `command.Execute` (via `RoutedCommand` param)
  - `toggle-italic-run-range-command` — same pattern
  - `toggle-underline-run-range-command` — same pattern
  - `apply-font-size-selection-command` — `GetEditingCommand("ApplyFontSize").Execute`
  - `increase-font-size-selection-command` — `EditingCommands.IncreaseFontSize.Execute`
  - `decrease-font-size-selection-command` — `EditingCommands.DecreaseFontSize.Execute`
  - `apply-font-family-selection-command` — `GetEditingCommand("ApplyFontFamily").Execute`
  - `apply-foreground-selection-command` — `GetEditingCommand("ApplyForeground").Execute`
  - `apply-background-selection-command` — `GetEditingCommand("ApplyBackground").Execute`
  - `align-left-selection-command` — `EditingCommands.AlignLeft.Execute`
  - `align-center/right/justify` — same pattern
  - `toggle-bullets-selection-command` — `EditingCommands.ToggleBullets.Execute`
  - `toggle-numbering-selection-command` — `EditingCommands.ToggleNumbering.Execute`
  - `increase-indentation-selection-command` — `EditingCommands.IncreaseIndentation.Execute`
  - `decrease-indentation-selection-command` — `EditingCommands.DecreaseIndentation.Execute`
  - `apply-inline-flow-direction-ltr/rtl` — `GetEditingCommand("ApplyInlineFlowDirectionLTR/RTL").Execute`
  - `remove-list-markers-command` — `removeListMarkers.Execute` (via reflection)

  Probes for `ApplySingleSpace`, `ApplyOneAndAHalfSpace`, `ApplyDoubleSpace`,
  `ApplyParagraphFlowDirectionLTR`, and `ApplyParagraphFlowDirectionRTL` were
  **not** migrated because their `EditingCommands` properties are `internal`
  and the commands were already using `InvokeTextEditorParagraphs` for
  internal-only handlers.

Tests:

- `UndoRedo_BoldFormat_RestoresNormalWeight`: create-plain("bold"),
  toggle-bold-selection-command, undo → weight != 700, redo → weight == 700.
- `UndoRedo_ItalicFormat_RestoresNormalStyle`: same pattern.
- `UndoRedo_UnderlineFormat_RestoresNoUnderline`: same pattern.
- `UndoRedo_FontSizeChange_RestoresOriginalSize`: same pattern.
- `UndoRedo_ForegroundChange_RestoresOriginalColor`: same pattern.
- `UndoRedo_BackgroundChange_RestoresOriginalColor`: same pattern.
- `UndoRedo_TextAlignmentChange_RestoresOriginalAlignment`: uses AlignCenter
  (AlignLeft is the default, making it a no-op).
- `UndoRedo_ParagraphFlowDirection_RestoresOriginalDirection`: same pattern.

All 8 tests pass.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Results: 130 passed, 0 skipped, 0 failed.

Next session:

- Ctrl+A SelectAll keyboard shortcut and keyboard shortcut coverage for
  clipboard operations.
