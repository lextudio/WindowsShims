# RichTextBox Port

This folder tracks the next phase of the WPF `RichTextBox` /
`System.Windows.Documents` source-first port in `LeXtudio.Windows`.

The older top-level files still matter:

- `docs/PLAN.md` records the original source-first migration playbook.
- `docs/RICHTEXTBOX-PORT-CATALOG.md` is a useful catalog, but it is currently
  stale for several core files. The project file is the source of truth.

## Current State

Baseline checked on this branch:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
Build succeeded: 0 warnings, 0 errors
```

The compile frontier is substantially past the first local-shell milestone:

- Upstream WPF `RichTextBox.cs` and `TextBoxBase.cs` are enabled.
- Upstream WPF `FlowDocument.cs`, `TextContainer.cs`, `TextPointer.cs`,
  `TextRange.cs`, `TextRangeBase.cs`, `TextRangeEdit*.cs`,
  `TextRangeSerialization.cs`, and many `TextEditor*` files are enabled.
- Uno-specific partials and bridge files provide rendering, input forwarding,
  caret/selection refresh, hyperlink activation, and platform gaps.
- Local legacy shell files can still exist on disk even when they are removed
  from compilation by `LeXtudio.Windows.csproj`.

Practical completion estimate:

- Source/compile migration: about 70-80%.
- Uno behavior parity: about 35-45%.
- Overall deliverable readiness: about 50-60%.

The gap is not the presence of the control type anymore. The gap is verified
editable behavior on Uno: text-tree movement, selection semantics, undo,
clipboard/serialization fidelity, keyboard editing edge cases, IME/spelling,
drag/drop, and layout/caret precision.

## Working Rules

1. Keep `LeXtudio.Windows` building after every session.
2. Prefer one narrow behavior rung per session.
3. Add tests before changing deep editor code whenever a non-UI test can express
   the expected behavior.
4. Treat `docs/RICHTEXTBOX-PORT-CATALOG.md` as historical until it is refreshed
   from the project file.
5. Do not chase full WPF document fidelity before the editable spine has a
   repeatable test harness.

## Milestones

### M0 - Local Status Baseline

Goal: make the current state auditable.

- Add this index.
- Record current build status.
- Identify stale catalog entries.
- Add at least one local RichTextBox/Documents test file so future sessions do
  not depend only on external runtime harness notes.

Done when:

- `index.md` names the current source and behavior status.
- `LeXtudio.Windows` builds.
- A first RichTextBox/Documents test target runs locally.

### M1 - Non-UI Document Model Tests

Goal: lock down document primitives that do not require a visual tree.

Candidate coverage:

- `FlowDocument` ownership and block collection behavior.
- `Paragraph`/`Run` insertion and text extraction.
- `TextRange` construction, ordering, and same-document validation.
- `AppendText` document mutation.
- Basic inline formatting property application.

Done when:

- Tests run under `src/LeXtudio.Windows.Tests`.
- Failures identify real behavior gaps rather than harness setup problems.

### M2 - Runtime Harness for RichTextBox Visual Behavior

Goal: bring the external UnoRichText runtime checks into a repeatable
WindowsShims-owned command path.

Candidate coverage:

- Template application creates the FlowDocument render scope.
- Paragraph/run rendering appears in the visual tree.
- Caret hit-testing does not throw.
- Selection invalidates render layout.
- Hyperlink hit-test and activation path still work.

Done when:

- There is a documented command to run the harness.
- A green run is recorded in this folder.
- Integration tests live under `tests/` and drive the host through DevFlow.

### M3 - Editing Spine

Goal: close the highest-value editable behavior gaps first.

Candidate coverage:

- Plain text input.
- Enter creates/continues paragraphs correctly.
- Backspace/Delete merge paragraphs and update selection.
- Arrow/home/end movement uses real caret positions.
- Basic undo/redo works for text insertion/deletion.

Done when:

- Behavior is covered by tests or a deterministic runtime probe.
- The bridge code stops relying on one-off fast paths for common editing cases.

### M4 - Clipboard and Serialization

Goal: validate the already-linked `TextRangeSerialization` path and document
which formats are intentionally partial on Uno.

Candidate coverage:

- Plain text copy/paste.
- XAML save/load for simple paragraphs/runs.
- RTF support status.
- Image/package payload limitations.

Done when:

- Supported formats are tested.
- Unsupported formats fail predictably or are explicitly documented.

### M5 - Optional WPF Parity Families

Goal: defer expensive subsystems until the editable spine is stable.

Deferred unless a consumer requires them:

- Full document pagination/printing.
- Fixed document/document sequence families.
- Advanced typography.
- Speller/TSF/IME parity.
- Full table layout fidelity.

## Session Log

### Session 1 - Baseline and First Test Harness

Status: complete.

Scope:

- Create this RichTextBox planning index.
- Add a first local test file for RichTextBox/Documents behavior.
- Run the core build and test command.

Result:

- Added `src/LeXtudio.Windows.Tests/RichTextBoxDocumentsTests.cs`.
- Added the test file to the explicit compile list in
  `src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj`.
- Confirmed reflection-level WPF-shaped API surface for:
  `RichTextBox.Document`, inherited `AppendText`, `FlowDocument`
  text-range boundaries, and `TextRange(TextPointer, TextPointer)`.
- Confirmed `LeXtudio.Windows` builds cleanly on `net10.0-desktop`.
- Confirmed the NUnitLite suite passes: 233/233.

Important finding:

- Direct `new FlowDocument()` / `new RichTextBox()` tests do not belong in the
  plain NUnitLite process. They currently fail before behavior assertions
  because Uno static initialization needs a dispatcher-backed runtime context
  when brush/default-style properties are created. Those tests must move to M2's
  runtime harness instead of being treated as document-model regressions.

Commands:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet run --project src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop -- --labels=Before
```

Next session:

- Extend the new `tests/RichTextBox.*` DevFlow harness beyond the first runtime
  text readback checks.
- Candidate assertions: template render scope exists, caret hit-test does not
  throw, Enter/Backspace editing behavior, and hyperlink activation.

### Session 2 - DevFlow Integration Harness

Status: complete.

Scope:

- Add a WindowsShims-owned RichTextBox integration suite under `tests/`.
- Drive RichTextBox runtime behavior through DevFlow, matching the DataGrid
  integration-test architecture.
- Cover the first runtime-only behavior that plain NUnitLite cannot safely
  assert: `RichTextBox` / `FlowDocument` instantiation and text readback inside
  a real Uno dispatcher context.

Added projects:

- `tests/RichTextBox.TestScenarios`
- `tests/RichTextBox.IntegrationTestHost`
- `tests/RichTextBox.IntegrationTests`

DevFlow actions:

- `richtextbox.probe.state`
- `richtextbox.probe.create-plain`
- `richtextbox.probe.set-document`

Verified behavior:

- Host starts with the DevFlow agent.
- `RichTextBox.AppendText("hello")` mutates the default document enough for
  `TextRange(document.ContentStart, document.ContentEnd).Text` to contain
  `hello`.
- Assigning a `FlowDocument` containing `Paragraph(new Run("document text"))`
  reads back through the same `TextRange` path.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 3/3
```

Next session:

- Add a DevFlow probe that verifies the applied RichTextBox template creates the
  expected `MS.Internal.Documents.FlowDocumentView` render scope.
- Then add the first edit-key probe: focus, input, and readback after a simple
  character insertion.

### Session 3 - Render Scope and Append Mutation

Status: complete.

Scope:

- Extend the DevFlow snapshot with reflected runtime internals needed for
  template/render-scope verification.
- Add a second mutation probe for an existing RichTextBox instance.

DevFlow additions:

- `richtextbox.probe.state` now reports:
  `contentHostAvailable`, `renderScopeType`, and `textViewType`.
- `richtextbox.probe.append` appends text to the current RichTextBox and returns
  the same snapshot.

Verified behavior:

- After `richtextbox.probe.create-plain`, the template content host is available.
- The reflected render scope is
  `MS.Internal.Documents.FlowDocumentView`.
- The reflected text view is
  `MS.Internal.Documents.UnoFlowDocumentTextView`.
- A second `AppendText(" world")` call mutates the existing document and
  `TextRange(document.ContentStart, document.ContentEnd).Text` contains both
  the original and appended text.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 5/5
```

Next session:

- Add an input-path probe that focuses the RichTextBox and drives the actual
  character input path rather than calling `AppendText` directly.
- If platform event synthesis is unreliable, expose a narrow test-only DevFlow
  action that invokes `OnCharacterReceived` or the same `TextEditorTyping`
  bridge used by the Uno partial, then document the limitation.

### Session 4 - TextEditor Typing Path

Status: complete.

Scope:

- Add DevFlow coverage for text insertion through the WPF `TextEditorTyping`
  editing path rather than direct `AppendText`.
- Fix the first runtime editing-spine gap found by that probe.

DevFlow additions:

- `richtextbox.probe.text-input`

Implementation notes:

- Constructing/platform-synthesizing Uno `CharacterReceivedRoutedEventArgs` is
  not used in this session.
- The probe reflects into upstream `TextEditorTyping.DoTextInput(...)`, which is
  the actual insertion worker called after `OnTextInput` schedules input.
- A first attempt to call `TextEditorTyping.OnTextInput(...)` directly exposed a
  dispatcher mismatch: `ScheduleInput` queued a background callback and the Uno
  dispatcher path cleared `PendingInputItems` before the current call appended
  its item. The session bypasses that scheduling layer and tests the insertion
  worker directly.

Product fix:

- `TextPointer.ITextPointer.GetValue(...)` now supplies WPF-compatible defaults
  for `FrameworkElement.LanguageProperty` (`en-US`) and
  `FrameworkElement.FlowDirectionProperty` (`LeftToRight`) when the Uno
  DP owner/default bridge returns `null` / `UnsetValue`.
- `FrameworkElement.LanguageProperty` also now has a non-null default
  `XmlLanguage` metadata value.

Verified behavior:

- `richtextbox.probe.text-input` can insert `abc` into a focused RichTextBox.
- Readback through `TextRange(document.ContentStart, document.ContentEnd).Text`
  contains the inserted text.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 6/6
```

Next session:

- Decide whether to fix `TextEditorTyping.ScheduleInput` under Uno so
  `OnTextInput(...)` itself can be tested without bypassing scheduling.
- Add the first deletion/editing command probe: insert text, invoke Backspace or
  Delete through the RichTextBox key path, and verify `TextRange` readback.

### Session 5 - Scheduled Text Input and Backspace Command

Status: complete.

Scope:

- Fix the Uno-specific `TextEditorTyping.ScheduleInput` dispatcher mismatch
  found in Session 4.
- Add DevFlow coverage for `TextEditorTyping.OnTextInput(...)` itself.
- Add the first deletion command probe through the upstream Backspace command
  handler.

Product fix:

- `TextEditorTyping.ScheduleInput(...)` now processes input synchronously on
  `HAS_UNO`. Uno's dispatcher can run the background callback before the current
  item is appended to `PendingInputItems`; synchronous processing preserves the
  editor ordering contract until WPF-style dispatcher batching is modeled.

DevFlow additions:

- `richtextbox.probe.text-input-event`
- `richtextbox.probe.backspace-command`

Verified behavior:

- `richtextbox.probe.text-input-event` drives
  `TextEditorTyping.OnTextInput(...)` and inserts text into the document.
- `richtextbox.probe.backspace-command` invokes the upstream Backspace command
  handler and removes the previous character after text insertion.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 8/8
```

Next session:

- Add Delete command coverage.
- Add a probe that goes through `RichTextBox.OnKeyDown` / Uno key mapping if a
  constructible `KeyRoutedEventArgs` path is available; otherwise keep command
  handler probes explicit and document the platform-event limitation.

### Session 6 - Delete Command Coverage

Status: complete.

Scope:

- Add DevFlow coverage for the upstream Delete command handler.
- Keep the test on the command path for now, matching the Backspace coverage
  from Session 5, while leaving full Uno key-event synthesis as a separate
  follow-up.

DevFlow additions:

- `richtextbox.probe.delete-selection-command`

Verified behavior:

- The probe focuses the current RichTextBox, calls the public `SelectAll()`
  API, invokes `TextEditorTyping.OnDelete(...)`, and verifies the selected
  document text is removed through `TextRange` readback.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 9/9
```

Next session:

- Investigate whether `RichTextBox.OnKeyDown` can be reached through a stable
  Uno `KeyRoutedEventArgs` construction path in the desktop integration host.
- If key-event construction remains unsuitable, add more command-handler probes
  for selection replacement, Return/paragraph insertion, and clipboard-free
  editing cases, and document the event-synthesis limitation explicitly.

### Session 7 - RichTextBox OnKeyDown Path

Status: complete.

Scope:

- Determine whether the desktop DevFlow host can construct Uno
  `KeyRoutedEventArgs` and call `RichTextBox.OnKeyDown(...)`.
- Add the first integration test that enters through the RichTextBox key path
  rather than directly invoking a WPF command handler.

Implementation notes:

- `Microsoft.UI.Xaml.Input.KeyRoutedEventArgs` has an internal constructor in
  the Uno desktop runtime. The test host creates it by reflection with:
  original source, `VirtualKey`, `VirtualKeyModifiers.None`, no physical key
  status, and no Unicode key.
- `RichTextBox.OnKeyDown(...)` is protected and overloaded/inherited, so the
  probe resolves the exact Uno `KeyRoutedEventArgs` signature by reflection.

DevFlow additions:

- `richtextbox.probe.key-down`

Verified behavior:

- After text is inserted through `richtextbox.probe.text-input-event`, invoking
  `richtextbox.probe.key-down` with `Back` reaches
  `RichTextBox.OnKeyDown(...)`, maps to the WPF Backspace editing command, and
  removes the previous character.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 10/10
```

Next session:

- Add key-path coverage for `Delete`, using selection or caret positioning to
  make the observable mutation deterministic.
- Then add Return/paragraph insertion coverage through the same
  `richtextbox.probe.key-down` action.

### Session 8 - Delete and Enter Key Coverage

Status: complete.

Scope:

- Add more real `RichTextBox.OnKeyDown(...)` DevFlow coverage.
- Cover Delete through the Uno key path with a deterministic selection.
- Cover Enter/paragraph insertion through the Uno key path.

Product fixes:

- The RichTextBox test scenario now explicitly sets `AcceptsReturn = true` so
  Enter behavior is independent from metadata-bridge defaults.
- `RichTextBox.uno.cs` now handles unmodified `VirtualKey.Enter` on the Uno key
  path by inserting a new paragraph into `Document.Blocks`, moving the editor
  selection to the new paragraph, and refreshing the caret. This avoids the
  current upstream paragraph-split path, which can corrupt the Uno text tree by
  treating a `Paragraph` as an `Inline`.
- `TextEditorTyping.OnEnterBreak(...)` no longer requires WPF
  `UiScope.IsKeyboardFocused` under `HAS_UNO`; Uno focus is already represented
  by the key event entering the control.

DevFlow additions:

- `richtextbox.probe.key-down-select-all`

Verified behavior:

- `richtextbox.probe.key-down` with `Back` removes the previous character.
- `richtextbox.probe.key-down-select-all` with `Delete` removes selected text.
- `richtextbox.probe.key-down` with `Enter`, followed by text input, produces
  at least two document blocks and preserves text before and after the break.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 12/12
```

Next session:

- Add selection replacement coverage through `OnTextInput`: select existing
  text, type replacement text, and verify old content is removed.
- Start covering basic formatting commands such as ToggleBold once selection
  state is observable in the DevFlow snapshot.

### Session 9 - Selection Replacement Input

Status: complete.

Scope:

- Add DevFlow coverage for replacing an existing selection through the
  `TextEditorTyping.OnTextInput(...)` path.

DevFlow additions:

- `richtextbox.probe.replace-selection-text-input-event`

Verified behavior:

- The probe focuses the RichTextBox, selects all existing document text, sends
  composed character input through `OnTextInput(...)`, and verifies the old text
  is removed while the replacement text is present.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 13/13
```

Next session:

- Add observable selection metadata to the DevFlow snapshot, then cover basic
  formatting commands such as ToggleBold/ToggleItalic on selected text.

### Session 10 - ToggleBold Formatting

Status: complete.

Scope:

- Add selection and inline-formatting observability to the DevFlow snapshot.
- Cover selected-text ToggleBold through the upstream formatting command
  handler.

Product fix:

- `TextEditorCharacters.OnToggleBold(...)` now has a `HAS_UNO` fallback for
  non-empty RichTextBox selections: after invoking the upstream selection
  formatting path, it applies the resolved `FontWeight` to document inlines.
  This keeps ToggleBold observable while the Uno text tree's
  `TextSelection.ApplyPropertyValue(...)` path is incomplete.

DevFlow additions:

- Snapshot now reports `selectionText`, `selectionFontWeight`,
  `firstInlineType`, and `firstInlineFontWeight`.
- `richtextbox.probe.toggle-bold-selection-command`

Verified behavior:

- Selecting all text and invoking ToggleBold sets the first inline font weight
  to `700`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 14/14
```

Next session:

- Add ToggleItalic coverage using the same observable formatting path.
- Generalize the Uno formatting fallback if ToggleItalic exposes the same
  `TextSelection.ApplyPropertyValue(...)` gap.

### Session 11 - ToggleItalic Formatting

Status: complete.

Scope:

- Extend selected-text formatting coverage from ToggleBold to ToggleItalic.
- Generalize the `HAS_UNO` inline-formatting fallback.

Product fix:

- The ToggleBold fallback is now a shared
  `ApplyPropertyToDocumentInlines(...)` helper.
- `TextEditorCharacters.OnToggleItalic(...)` uses the same fallback for
  selected RichTextBox text because Uno `TextSelection.ApplyPropertyValue(...)`
  does not yet update inline properties reliably.

DevFlow additions:

- Snapshot now reports `firstInlineFontStyle`.
- `richtextbox.probe.toggle-italic-selection-command`

Verified behavior:

- Selecting all text and invoking ToggleItalic sets the first inline font style
  to `Italic`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 15/15
```

Next session:

- Add key-path Ctrl+B/Ctrl+I coverage if modifier injection can be added to
  the `KeyRoutedEventArgs` probe.
- Or cover ToggleUnderline with inline text decoration observability and the
  same formatting fallback.

### Session 12 - ToggleUnderline Formatting

Status: complete.

Scope:

- Extend selected-text formatting coverage from ToggleBold/ToggleItalic to
  ToggleUnderline.
- Add observable text-decoration state to the DevFlow RichTextBox snapshot.

Product fix:

- `TextEditorCharacters.OnToggleUnderline(...)` now normalizes non-collection
  Uno default values for `Inline.TextDecorationsProperty` before casting.
  This avoids an `InvalidCastException` when the value is still represented by
  `FreezableDefaultValueFactory`.
- ToggleUnderline now uses the shared `HAS_UNO` inline-formatting fallback so
  selected RichTextBox text receives the toggled underline decoration while
  `TextSelection.ApplyPropertyValue(...)` remains incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineHasUnderline`.
- `richtextbox.probe.toggle-underline-selection-command`

Verified behavior:

- Selecting all text and invoking ToggleUnderline sets underline decoration on
  the first inline.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 16/16
```

Next session:

- Cover repeated ToggleBold/ToggleItalic/ToggleUnderline calls to verify the
  selected inline formatting toggles back to the normal/default state.
- Then add Ctrl+B/Ctrl+I/Ctrl+U key-path coverage once modifier injection is
  validated through the Uno `KeyRoutedEventArgs` probe.

### Session 13 - Formatting Toggle-Off Coverage

Status: complete.

Scope:

- Cover repeated selected-text formatting commands so Bold, Italic, and
  Underline are verified in both directions.
- Remove order-dependent behavior from underline text decoration toggling.

Product fix:

- `TextDecorationCollection.TryRemove(...)` now builds and returns a new
  collection instead of mutating the source collection in place.
- This prevents repeated ToggleUnderline from modifying shared static
  collections such as `TextDecorations.Underline`, which previously made later
  underline operations order-dependent.

DevFlow additions:

- `ToggleBoldCommand_WhenInvokedTwice_RestoresNormalWeight`
- `ToggleItalicCommand_WhenInvokedTwice_RestoresNormalStyle`
- `ToggleUnderlineCommand_WhenInvokedTwice_RemovesUnderline`

Verified behavior:

- ToggleBold applies `700` on the first invocation and a second invocation
  restores a non-bold inline weight.
- ToggleItalic applies `Italic` on the first invocation and a second invocation
  restores a non-italic inline style.
- ToggleUnderline applies underline on the first invocation and a second
  invocation removes underline without corrupting shared underline state.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 19/19
```

Next session:

- Validate whether `KeyRoutedEventArgs` modifier injection can drive
  Ctrl+B/Ctrl+I/Ctrl+U through the RichTextBox key path.
- If modifier routing is not available yet, continue with command-level
  coverage for font size and foreground/background formatting.

### Session 14 - Keyboard Formatting Shortcuts

Status: complete.

Scope:

- Cover the RichTextBox `OnKeyDown(...)` entry point for selected-text
  formatting shortcuts.
- Validate Ctrl+B, Ctrl+I, and Ctrl+U against the same inline formatting
  observability used by command-level tests.

Product fix:

- `RichTextBox.OnKeyDown(...)` now recognizes Ctrl+B, Ctrl+I, and Ctrl+U when
  Alt is not held and dispatches `EditingCommands.ToggleBold`,
  `EditingCommands.ToggleItalic`, and `EditingCommands.ToggleUnderline`.
- The command dispatch reuses the migrated upstream command handlers and the
  existing Uno inline-formatting fallback.

DevFlow additions:

- `richtextbox.probe.key-down-select-all-modifiers`
- `KeyDown_ControlB_AppliesBoldToSelectedText`
- `KeyDown_ControlI_AppliesItalicToSelectedText`
- `KeyDown_ControlU_AppliesUnderlineToSelectedText`

Notes:

- The current Uno `KeyRoutedEventArgs` target does not expose a public
  `KeyModifiers` property. The DevFlow probe injects modifiers by temporarily
  setting the internal `Keyboard.ModifiersOverride`, matching the existing
  DataGrid test pattern for programmatic modifier state.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 22/22
```

Next session:

- Add command-level coverage for ApplyFontSize and font size increase/decrease.
- Add foreground/background formatting observability once brush values can be
  serialized consistently through the DevFlow snapshot.

### Session 15 - ApplyFontSize Formatting

Status: complete.

Scope:

- Add DevFlow observability for selected-text font size.
- Cover the upstream `TextEditorCharacters.OnApplyFontSize(...)` command
  handler for RichTextBox selections.

Product fix:

- `TextEditorCharacters.OnApplyFontSize(...)` now uses the shared `HAS_UNO`
  inline-formatting fallback for non-empty RichTextBox selections.
- This makes the applied absolute font size visible on document inlines while
  `TextSelection.ApplyPropertyValue(...)` is still incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineFontSize`.
- `richtextbox.probe.apply-font-size-selection-command`
- `ApplyFontSizeCommand_AppliesFontSizeToSelectedText`

Verified behavior:

- Selecting all text and invoking ApplyFontSize with `24` sets the first
  inline font size to `24`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 23/23
```

Next session:

- Cover IncreaseFontSize and DecreaseFontSize by adding relative font-size
  fallback behavior for selected RichTextBox inlines.
- Add foreground/background formatting observability once brush serialization
  is stable enough for DevFlow assertions.

### Session 16 - Relative Font Size Commands

Status: complete.

Scope:

- Cover selected-text IncreaseFontSize and DecreaseFontSize command handlers.
- Add Uno fallback behavior for relative font-size changes.

Product fix:

- `TextEditorCharacters.OnIncreaseFontSize(...)` now uses a `HAS_UNO`
  RichTextBox fallback for non-empty selections that increments inline font
  size by `OneFontPoint`.
- `TextEditorCharacters.OnDecreaseFontSize(...)` uses the same fallback with a
  negative delta.
- The fallback clamps to the upstream min/max font-size bounds and avoids the
  current `TextRange.ApplyPropertyValue(... Increase/Decrease ...)` exception
  path for selected RichTextBox text.

DevFlow additions:

- `richtextbox.probe.increase-font-size-selection-command`
- `richtextbox.probe.decrease-font-size-selection-command`
- `IncreaseFontSizeCommand_IncreasesSelectedTextFontSize`
- `DecreaseFontSizeCommand_DecreasesSelectedTextFontSize`

Verified behavior:

- Applying font size `24` and then IncreaseFontSize sets the first inline font
  size to `24.75`.
- Applying font size `24` and then DecreaseFontSize sets the first inline font
  size to `23.25`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 25/25
```

Next session:

- Add foreground/background formatting observability using stable brush
  serialization.
- Add command-level coverage for ApplyForeground and ApplyBackground.

### Session 17 - Foreground and Background Formatting

Status: complete.

Scope:

- Add stable brush observability to the DevFlow RichTextBox snapshot.
- Cover ApplyForeground and ApplyBackground command handlers for selected
  RichTextBox text.

Product fix:

- `TextEditorCharacters.OnApplyForeground(...)` now applies the selected brush
  to RichTextBox document inlines under `HAS_UNO`.
- `TextEditorCharacters.OnApplyBackground(...)` uses the same inline fallback
  for selected RichTextBox text.
- This keeps foreground/background formatting observable while selected
  `TextSelection.ApplyPropertyValue(...)` behavior remains incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineForeground` and `firstInlineBackground` as
  `#AARRGGBB` for solid brushes.
- `richtextbox.probe.apply-foreground-selection-command`
- `richtextbox.probe.apply-background-selection-command`
- `ApplyForegroundCommand_AppliesForegroundToSelectedText`
- `ApplyBackgroundCommand_AppliesBackgroundToSelectedText`

Verified behavior:

- Applying foreground with `LightGreen` sets the first inline foreground to
  `#FF90EE90`.
- Applying background with `LightPink` sets the first inline background to
  `#FFFFB6C1`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 27/27
```

Next session:

- Add ApplyFontFamily coverage and stable FontFamily snapshot serialization.
- Then evaluate paragraph formatting commands such as alignment once
  paragraph-level observability is in place.

### Session 18 - Font Family Formatting

Status: complete.

Scope:

- Add stable FontFamily observability to the DevFlow RichTextBox snapshot.
- Cover the ApplyFontFamily command handler for selected RichTextBox text.

Product fix:

- `TextEditorCharacters.OnApplyFontFamily(...)` now applies the selected
  `FontFamily` to RichTextBox document inlines under `HAS_UNO`.
- This mirrors the existing selected-text fallback used for font size,
  foreground, and background formatting while selected
  `TextSelection.ApplyPropertyValue(...)` behavior remains incomplete.

DevFlow additions:

- Snapshot now reports `firstInlineFontFamily`.
- `richtextbox.probe.apply-font-family-selection-command`
- `ApplyFontFamilyCommand_AppliesFontFamilyToSelectedText`

Verified behavior:

- Applying `Courier New` sets the first inline font family to `Courier New`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 28/28
```

Next session:

- Add paragraph-level snapshot fields needed for alignment assertions.
- Cover paragraph alignment commands once paragraph formatting can be observed
  through DevFlow.

### Session 19 - Paragraph Alignment Commands

Status: complete.

Scope:

- Add paragraph-level observability to the DevFlow RichTextBox snapshot.
- Cover AlignLeft, AlignCenter, AlignRight, and AlignJustify command handlers
  for selected RichTextBox paragraphs.

Product fix:

- `TextEditorParagraphs.OnAlignLeft(...)`, `OnAlignCenter(...)`,
  `OnAlignRight(...)`, and `OnAlignJustify(...)` now apply
  `Block.TextAlignmentProperty` directly to RichTextBox document blocks under
  `HAS_UNO`.
- This keeps paragraph alignment command behavior observable while the generic
  paragraph path through `_OnApplyProperty(... applyToParagraphs: true)` is
  still incomplete.

DevFlow additions:

- Snapshot now reports `firstParagraphTextAlignment`.
- `richtextbox.probe.align-left-selection-command`
- `richtextbox.probe.align-center-selection-command`
- `richtextbox.probe.align-right-selection-command`
- `richtextbox.probe.align-justify-selection-command`
- `AlignCommand_AppliesTextAlignmentToSelectedParagraph`

Verified behavior:

- AlignLeft sets the first paragraph text alignment to `Left`.
- AlignCenter sets the first paragraph text alignment to `Center`.
- AlignRight sets the first paragraph text alignment to `Right`.
- AlignJustify sets the first paragraph text alignment to `Justify`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 32/32
```

Next session:

- Add paragraph line-spacing observability.
- Cover ApplySingleSpace, ApplyOneAndAHalfSpace, and ApplyDoubleSpace command
  handlers through DevFlow.

### Session 20 - Remove Formatting Fallbacks and Restore WPF Property Path

Status: complete.

Scope:

- Replace the coarse command-level RichTextBox formatting fallbacks from
  Sessions 15-19 with lower-level fixes that keep execution on the migrated
  WPF `TextSelection` / `TextRangeEdit` property path.
- Keep DevFlow coverage for character and paragraph formatting while asserting
  the FlowDocument model after WPF-style inline split/wrap behavior.

Product fix:

- Removed document-wide RichTextBox inline/block formatting fallbacks from
  `TextEditorCharacters` and `TextEditorParagraphs`.
- `WinUIDependencyPropertyExtensions` now records the real property type for
  WPF-style `DependencyProperty.Register`, `RegisterAttached`,
  `RegisterReadOnly`, and `RegisterAttachedReadOnly` calls. This lets WPF
  `TextSchema` identify incremental properties such as
  `TextElement.FontSizeProperty`.
- `TextSchema.ValuesAreEqual(...)` now compares WinUI `FontWeight` values by
  numeric `Weight` under `HAS_UNO`, preserving WPF toggle semantics for
  `ToggleBold`.
- `TextRangeEdit` paragraph formatting now has an Uno TextPointer-compatible
  block traversal under `HAS_UNO`, while still applying paragraph values
  through WPF `SetPropertyOnParagraphOrBlockUIContainer(...)`.
- DevFlow snapshots now observe the first leaf `Run` and inline tree so tests
  assert the effective formatted text after WPF split/wrap output.

DevFlow coverage:

- Existing formatting and paragraph alignment command tests now exercise the
  WPF property path rather than command-level fallbacks.
- Snapshot fields added: `inlineTree`, `firstRunFontWeight`,
  `firstRunFontStyle`, `firstRunFontSize`, `firstRunFontFamily`,
  `firstRunForeground`, `firstRunBackground`, and `firstRunHasUnderline`.

Verified behavior:

- ToggleBold and Ctrl+B apply bold through `TextRangeEdit` inline formatting.
- IncreaseFontSize and DecreaseFontSize use WPF incremental property handling.
- AlignLeft, AlignCenter, AlignRight, and AlignJustify apply through paragraph
  property handling.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --no-build
```

Result:

```text
Passed: 32/32
```

Next session:

- Add partial-selection formatting tests to prove selected ranges split
  precisely and unselected runs keep original values.
- Then cover line spacing commands through `TextRangeEdit` paragraph handling.

### Session 22 - Partial Selection Formatting and WPF Line Spacing No-Op

Status: complete.

Scope:

- Add DevFlow coverage for formatting only a subrange inside the first `Run`.
- Lock `ApplySingleSpace`, `ApplyOneAndAHalfSpace`, and `ApplyDoubleSpace` to
  upstream WPF behavior.

Product notes:

- No command-level formatting fallback was added.
- Upstream WPF `TextEditorParagraphs.OnApplySingleSpace(...)`,
  `OnApplyOneAndAHalfSpace(...)`, and `OnApplyDoubleSpace(...)` are empty
  handlers. The migrated shim should therefore preserve no-op behavior unless
  upstream WPF changes.
- While probing paragraph commands, `ApplyParagraphFlowDirectionRTL` exposed a
  separate Uno compatibility issue: `Block.FlowDirectionProperty` is an
  `AddOwner` of WinUI `FrameworkElement.FlowDirectionProperty`, and setting it
  on document content can trigger WinUI's `FrameworkElement` backing-field
  callback against a non-FrameworkElement target. This needs a focused DP-owner
  compatibility fix, not a RichTextBox command fallback.

DevFlow additions:

- `richtextbox.probe.toggle-bold-run-range-command`
- `richtextbox.probe.toggle-italic-run-range-command`
- `richtextbox.probe.toggle-underline-run-range-command`
- `richtextbox.probe.apply-single-space-selection-command`
- `richtextbox.probe.apply-one-and-a-half-space-selection-command`
- `richtextbox.probe.apply-double-space-selection-command`
- Snapshot now reports paragraph `LineHeight`, `LineStackingStrategy`, and
  `FlowDirection`.
- `inlineTree` now includes per-run text, weight, style, size, and underline
  state.

Verified behavior:

- Formatting `abcdef` range `[2, 4)` splits the first run so `ab` and `ef`
  remain unformatted while `cd` receives Bold, Italic, or Underline.
- WPF line-spacing commands leave paragraph `LineHeight` as `NaN` and
  `LineStackingStrategy` as `MaxHeight`.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~WithPartialRunSelection|FullyQualifiedName~LineSpacingCommand_MatchesWpfNoOpBehavior"
```

Result:

```text
Passed: 6/6
```

Next session:

- Fix content-element `FlowDirection` DP owner compatibility so upstream
  `ApplyParagraphFlowDirectionLTR/RTL` can run through the WPF property path.
- Add DevFlow coverage for paragraph flow direction once that DP issue is
  fixed.

### Session 23 - Paragraph FlowDirection Through WPF Editing Path

Status: complete.

Scope:

- Fix `FlowDirection` on migrated document content without adding command
  fallbacks.
- Add DevFlow coverage for `ApplyParagraphFlowDirectionLTR` and
  `ApplyParagraphFlowDirectionRTL`.

Product fixes:

- `Block.FlowDirectionProperty` and `Inline.FlowDirectionProperty` now register
  Uno-native dependency properties under `HAS_UNO` instead of reusing WinUI
  `FrameworkElement.FlowDirectionProperty` through `AddOwner`.
- `TextSchema` now classifies the Uno `Block.FlowDirectionProperty` as an
  inheritable paragraph property and the Uno `Inline.FlowDirectionProperty` as
  an inheritable/structural character property.
- `TextEditorParagraphs.OnApplyParagraphFlowDirectionLTR/RTL` uses
  `Block.FlowDirectionProperty` under `HAS_UNO`, while preserving upstream WPF's
  `FrameworkElement.FlowDirectionProperty` path for non-Uno builds.
- The paragraph property worker keeps applying through
  `SetPropertyOnParagraphOrBlockUIContainer` and the normal WPF
  `ClearValue`/`SetValue` path; no RichTextBox-specific fallback was added.
- Plain paragraphs no longer return early from list splitting when the
  selection is not inside a list.

DevFlow additions:

- `richtextbox.probe.apply-paragraph-flow-direction-ltr-selection-command`
- `richtextbox.probe.apply-paragraph-flow-direction-rtl-selection-command`
- `ParagraphFlowDirectionCommand_AppliesFlowDirectionToSelectedParagraph`
  verifies both `LeftToRight` and `RightToLeft`.

Debugging note:

- A temporary `/tmp/richtextbox-flowdirection.log` probe confirmed that the
  command reached `TextRangeEdit.SetParagraphPropertyWorker` and set the local
  `Paragraph` `FlowDirection` value. The temporary file logging was removed
  before verification.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 42/42
```

Next session:

- Continue with inline flow direction command coverage and any remaining
  caret/selection behavior that still references
  `FrameworkElement.FlowDirectionProperty`.

### Session 21 - Undo and Redo Key Coverage

Status: complete.

Scope:

- Add DevFlow coverage for the public WPF `TextBoxBase.Undo()` and `Redo()`
  APIs on RichTextBox.
- Add real `RichTextBox.OnKeyDown(...)` coverage for Ctrl+Z and Ctrl+Y.

DevFlow additions:

- `richtextbox.probe.undo`
- `richtextbox.probe.redo`
- `richtextbox.probe.state` now reports `canUndo` and `canRedo`.

Product fix:

- `RichTextBox.uno.cs` now maps Ctrl+Z to `ApplicationCommands.Undo`,
  Ctrl+Shift+Z to `ApplicationCommands.Redo`, and Ctrl+Y to
  `ApplicationCommands.Redo` before falling through to character formatting
  shortcuts.

Verified behavior:

- Text inserted through `TextEditorTyping.OnTextInput(...)` is removed by
  public `Undo()` and restored by public `Redo()`.
- The same mutation is removed/restored through the Uno `OnKeyDown` path for
  Ctrl+Z/Ctrl+Y.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~UndoRedo_RestoresTextInputMutation|FullyQualifiedName~KeyDown_ControlZAndControlY_InvokeUndoRedo"
```

Result:

```text
Passed: 2/2
```

Next session:

- Add partial-selection formatting tests to prove selected ranges split
  precisely and unselected runs keep original values.
- Then cover line spacing commands through `TextRangeEdit` paragraph handling.
