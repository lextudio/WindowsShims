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
