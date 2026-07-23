# RichTextBox Port

This folder tracks the next phase of the WPF `RichTextBox` /
`System.Windows.Documents` source-first port in `LeXtudio.Windows`.

The older top-level files still matter:

- `docs/PLAN.md` records the original source-first migration playbook.
- `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` is a useful catalog, but it is currently
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
- Uno behavior parity: about 45-55% (Session 35 fixed the `LogicalTreeHelper`
  no-op that left every `TextElement.Parent` unset; both remaining
  `RichTextBox.uno.cs` editing fast paths were then removed as dead code).
- Overall deliverable readiness: about 55-65%.

The gap is not the presence of the control type anymore. The gap is verified
editable behavior on Uno: text-tree movement, selection semantics, undo,
clipboard/serialization fidelity, keyboard editing edge cases, IME/spelling,
drag/drop, and layout/caret precision.

## Working Rules

1. Keep `LeXtudio.Windows` building after every session.
2. Prefer one narrow behavior rung per session.
3. Add tests before changing deep editor code whenever a non-UI test can express
   the expected behavior.
4. Treat `docs/richtextbox/RICHTEXTBOX-PORT-CATALOG.md` as historical until it is refreshed
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

Status: met as of Session 35. All candidate coverage above is tested, and the
two remaining `RichTextBox.uno.cs` fast paths (paragraph-merge, Enter) were
removed after Session 35's `LogicalTreeHelper` fix made the real upstream WPF
command handlers work correctly on their own.

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

Each session is tracked as a separate file:

| Session | File | Status |
|---------|------|--------|
| 1 | [session1.md](session1.md) | Baseline and First Test Harness |
| 2 | [session2.md](session2.md) | DevFlow Integration Harness |
| 3 | [session3.md](session3.md) | Render Scope and Append Mutation |
| 4 | [session4.md](session4.md) | TextEditor Typing Path |
| 5 | [session5.md](session5.md) | Scheduled Text Input and Backspace Command |
| 6 | [session6.md](session6.md) | Delete Command Coverage |
| 7 | [session7.md](session7.md) | RichTextBox OnKeyDown Path |
| 8 | [session8.md](session8.md) | Delete and Enter Key Coverage |
| 9 | [session9.md](session9.md) | Selection Replacement Input |
| 10 | [session10.md](session10.md) | ToggleBold Formatting |
| 11 | [session11.md](session11.md) | ToggleItalic Formatting |
| 12 | [session12.md](session12.md) | ToggleUnderline Formatting |
| 13 | [session13.md](session13.md) | Formatting Toggle-Off Coverage |
| 14 | [session14.md](session14.md) | Keyboard Formatting Shortcuts |
| 15 | [session15.md](session15.md) | ApplyFontSize Formatting |
| 16 | [session16.md](session16.md) | Relative Font Size Commands |
| 17 | [session17.md](session17.md) | Foreground and Background Formatting |
| 18 | [session18.md](session18.md) | Font Family Formatting |
| 19 | [session19.md](session19.md) | Paragraph Alignment Commands |
| 20 | [session20.md](session20.md) | Remove Formatting Fallbacks and Restore WPF Property Path |
| 21 | [session21.md](session21.md) | Undo and Redo Key Coverage |
| 22 | [session22.md](session22.md) | Partial Selection Formatting and WPF Line Spacing No-Op |
| 23 | [session23.md](session23.md) | Paragraph FlowDirection Through WPF Editing Path |
| 24 | [session24.md](session24.md) | Inline FlowDirection Through Structural Inline Formatting |
| 25 | [session25.md](session25.md) | FlowDirection Property Audit for Text Positions |
| 26 | [session26.md](session26.md) | Keyboard FlowDirection KeyUp Path |
| 27 | [session27.md](session27.md) | Clipboard Copy/Cut/Paste Coverage |
| 28 | [session28.md](session28.md) | Caret and Shift Selection Navigation Coverage |
| 29 | [session29.md](session29.md) | Line and Document Boundary Navigation Coverage |
| 30 | [session30.md](session30.md) | Word Navigation Coverage |
| 31 | [session31.md](session31.md) | Word Deletion Coverage |
| 32 | [session32.md](session32.md) | Non-Empty Selection Word Deletion Coverage |
| 33 | [session33.md](session33.md) | Paragraph-Merge Crash Found and Fixed (Backspace/Delete at Paragraph Boundary) |
| 34 | [session34.md](session34.md) | Clipboard/Serialization Format Coverage (M4) |
| 35 | [session35.md](session35.md) | Root-Caused Logical Tree Parent Gap; Removed Both Editing Fast Paths |
| 36 | [session36.md](session36.md) | Real Character-Received Input Path Coverage |
| 37 | [session37.md](session37.md) | List/ListItem Construction Crash Found and Fixed; List Command Scope Documented |
| 38 | [session38.md](session38.md) | List Indentation Actually Works; Prior "Bug" Was a Test-Probe Bug |
| 39 | [session39.md](session39.md) | RemoveListMarkers Coverage on an Existing List |
| 40 | [session40.md](session40.md) | ToggleBullets/ToggleNumbering Coverage on an Existing List |
| 41 | [session41.md](session41.md) | Hyperlink Hit-Test and Activation Coverage (M2) |
| 42 | [session42.md](session42.md) | Caret Hit-Test Round-Trip Coverage; M2 Closed Out |
| 43 | [session43.md](session43.md) | Table Construction Audit; Real IME Integration via LeXtudio.UI.Text.Core |
| 44 | [session44.md](session44.md) | Drag/Drop Wired Up (Dead Code Activated); Disabled a Browser-Popping CI Test |
| 45 | [session45.md](session45.md) | CI-Safe Hyperlink Test Restored; Click-Inside-Selection Collapse Fixed |
