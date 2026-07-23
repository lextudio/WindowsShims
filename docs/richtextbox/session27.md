### Session 27 - Clipboard Copy/Cut/Paste Coverage

Status: complete.

Scope:

- Add DevFlow-driven coverage for RichTextBox clipboard operations without
  introducing behavior fallbacks.
- Exercise the migrated WPF `TextBoxBase.Copy()`, `Cut()`, and `Paste()` paths,
  which route through `TextEditorCopyPaste`.

DevFlow additions:

- `richtextbox.probe.copy-run-range`
- `richtextbox.probe.cut-run-range`
- `richtextbox.probe.paste-text`
- `richtextbox.probe.paste-text-at-run-offset`
- `richtextbox.probe.state` now reports `clipboardText`.

Verified behavior:

- Copying a first-run selection writes the selected text to the shim clipboard
  and leaves the document unchanged.
- Cutting a first-run selection writes the selected text to the shim clipboard
  and removes the selected content from the document.
- Pasting uses explicit first-run caret placement so the test verifies insertion
  at a known selection boundary instead of depending on the default caret
  position.

Commands:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --filter "FullyQualifiedName~CopyRunRange|FullyQualifiedName~CutRunRange|FullyQualifiedName~PasteText"
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 3/3 targeted
Passed: 48/48 full RichTextBox integration suite
```

Next session:

- Continue with selection/caret navigation coverage, especially explicit
  first-run caret offsets and Shift-selection expansion through the real
  RichTextBox key path.
