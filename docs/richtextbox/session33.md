### Session 33 - Paragraph-Merge Crash Found and Fixed (Backspace/Delete at Paragraph Boundary)

Status: complete. Bug found and fixed.

Scope:

- Attempted to add key-path coverage for paragraph merging: Delete at the end
  of a paragraph, and Backspace at the start of a paragraph, should merge the
  adjacent paragraph's content into the current one (M3 candidate coverage:
  "Backspace/Delete merge paragraphs and update selection").

Finding:

- `RichTextBox.uno.cs` (`OnKeyDown`, around lines 246-318) already has a
  paragraph-merge fast path for unmodified Backspace/Delete at a paragraph
  boundary. It is untested and currently crashes both directions
  (`KeyDown: paragraph-merge-back`/`-forward fast-path`) with a
  `NullReferenceException`.
- Repro: create-plain `""`, `text-input-event "abc"`, `key-down Enter`,
  `text-input-event "def"` (two paragraphs), place the caret at the boundary,
  then `key-down Delete` (or the Home+Backspace equivalent from the other
  side).
- Root cause: `TextRangeEditLists.MergeParagraphs` reposition triggers
  `Run.OnTextUpdated` -> `FrameworkContentElement.SetCurrentDeferredValue` ->
  `DeferredRunTextReference.Resolve()`, which constructs
  `new TextRange(_run.ContentStart, _run.ContentEnd)`. That constructor path
  goes through `TextRangeEditTables.BuildTableRange` /
  `FindTableElements`, which walks `TextElement.Parent` from each position up
  to a shared `commonAncestor`. Mid-reposition, a `Run`'s `Parent` chain is
  inconsistent under this Uno shim (the same limitation already called out in
  the `OnKeyDown` comment: "`Run.Parent` doesn't surface its containing
  Paragraph"), so the walk never reaches `commonAncestor` and hits a null
  `TextElement.Parent` dereference instead of the intended
  `Invariant.Assert` guard.
- This means `DeferredRunTextReference.Resolve()` is unsafe to call from
  inside any repositioning/tree-mutation operation, since it goes through the
  full table-aware `TextRange` construction machinery just to read a run's own
  text.

Product fix:

- Checked upstream WPF: `ext/wpf/.../Controls/DeferredRunTextReference.cs`
  already exists there, and its `GetValue` reads via
  `TextRangeBase.GetTextInternal(_run.ContentStart, _run.ContentEnd)` rather
  than constructing a `TextRange`. `GetTextInternal` walks `TextPointer`s
  directly to build plain text and never calls
  `TextRangeBase.Select`/`TextRangeEditTables.BuildTableRange`, so it never
  touches the fragile `TextElement.Parent` ancestor walk.
- `src/LeXtudio.Windows/System.Windows/Documents/DeferredRunTextReference.cs`
  (`Resolve()`) previously reimplemented this by constructing
  `new TextRange(_run.ContentStart, _run.ContentEnd)`, which routed through
  `TextRangeEditTables.FindTableElements` and crashed. It now calls
  `TextRangeBase.GetTextInternal(...)` directly, matching upstream WPF exactly
  instead of a local reimplementation. `TextRangeBase.cs` (and
  `GetTextInternal`) was already linked into `LeXtudio.Windows.csproj`, so no
  new files were needed.

DevFlow additions: none (reused `set-caret-run-offset`, `key-down`, and
`key-down-modifiers`, which already existed).

Tests added:

- `KeyDown_DeleteAtParagraphEnd_MergesNextParagraph`
- `KeyDown_BackspaceAtParagraphStart_MergesPreviousParagraph`

Verified behavior:

- With two paragraphs `abc` / `def`, placing the caret at the end of the first
  paragraph and pressing `Delete` merges them into a single paragraph
  `abcdef`.
- From the same two-paragraph document, moving to the start of the second
  paragraph (`Ctrl+End` then `Home`) and pressing `Backspace` merges them into
  a single paragraph `abcdef`.
- Both directions previously crashed with `NullReferenceException`; both now
  pass.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 68/68
```

Next session:

- Continue selection/editing coverage: consider selection replacement via the
  real key path (typing over a selection through `OnKeyDown`/character input
  rather than `OnTextInput` directly), or begin auditing
  clipboard/serialization format coverage (M4) now that the editing spine has
  broad key-path coverage including paragraph merging.
