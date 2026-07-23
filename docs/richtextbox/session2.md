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
