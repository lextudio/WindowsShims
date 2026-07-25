### Session 54 - IME Composition Visual Underline/Highlight

Status: completed.

Scope:

- Session 43 integrated real OS-level IME composition via
  `LeXtudio.UI.Text.Core` (`CoreTextEditContext`). The offset mapping
  (`GetPlainTextOffset`/`GetPositionAtPlainTextOffset`) works generally,
  including for multi-paragraph documents. IME composition strings are
  inserted into the document correctly.

- Missing: **visual feedback** during composition. In WPF, in-progress IME
  composition text is rendered with an underline (typically a wavy or solid
  underline below the composing characters). This shim has no such
  rendering — composition text appears indistinguishable from committed text.

- `FlowDocumentView.uno.cs` already has manual underline rendering for
  selection (a `Line` shape positioned at the character baseline). The IME
  composition underline would follow the same pattern: draw an underline
  `Shape` below each composing character for the duration of the composition.

Implementation:

- The `CoreTextEditContext.TextUpdating` event provides `RangeStart` and
  `RangeLength` in plain-text offset space. Map these to visual character
  rectangles (via the existing `GetPositionAtPlainTextOffset` → caret rect
  path used by `caret-hit-test-round-trip`).

- `RichTextBox.Ime.uno.cs` tracks `_imeComposing` state. Add a field
  `_imeCompositionStart` and `_imeCompositionLength` (or equivalent) that
  records the current composition range offsets.

- In `FlowDocumentView.uno.cs`, add a rendering pass that draws an underline
  for any character position within the IME composition range. Reuse the
  existing `Line` shape pattern from selection rendering.

- The underline style can be a simple solid underline (matching the platform's
  IME convention) rather than a wavy underline — the key deliverable is
  visible feedback that composition is in progress.

- `CompositionCompleted` clears the composition range and removes the
  underline.

Tests:

- `ImeComposition_DuringComposition_ShowsUnderlineForComposingRange`:
  simulate `TextUpdating` for a 3-character composition range, verify that
  `FlowDocumentView` has underline `Shape` elements for the composing
  characters.

- `ImeComposition_AfterCompositionCompleted_RemovesUnderline`:
  simulate `TextUpdating` then `CompositionCompleted`, verify underline
  elements are removed.

- Visual verification via the test host's snapshot mechanism (check for
  underline `Line` children in the visual tree, not pixel-level rendering).

Files modified:

- `src/LeXtudio.Windows/.../Controls/RichTextBox.Ime.uno.cs` — track
  composition range.
- `src/LeXtudio.Windows/.../MS.Internal.Documents/FlowDocumentView.uno.cs` —
  render composition underline.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Next session:

- Table visual rendering in `FlorenceLayoutEngine`.
