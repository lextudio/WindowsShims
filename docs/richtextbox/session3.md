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
