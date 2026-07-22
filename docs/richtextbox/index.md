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

- Build or wire a WindowsShims-owned runtime harness for actual
  `FlowDocument`/`RichTextBox` instantiation.
- First runtime assertions should cover default document creation,
  `AppendText("hello")`, and `TextRange(document.ContentStart,
  document.ContentEnd).Text`.
