### Session 68 - Performance Stress Testing

Status: completed.

Scope:

- The shim has never been tested with large documents (1000+ paragraphs,
  10000+ runs). Performance bottlenecks may exist in:
  - `TextContainer` splay tree operations during large edits.
  - `FlorenceLayoutEngine.Format` when formatting thousands of lines.
  - `CollectSpans` recursion for documents with deep inline nesting.
  - Undo/redo with large change blocks.
  - Clipboard operations (copy/paste) with large selections.
- This session adds stress tests and profiles the hot paths.

Implementation:

- Add probe methods for creating large documents (`create-large-document`
  with paragraph count and run count parameters).
- Add a `benchmark-format` probe that measures `FlorenceLayoutEngine.Format`
  execution time.
- Run tests with 100, 500, 1000, 5000 paragraphs and measure timing.
- Fix any performance bottlenecks found (e.g., cache `SpanInfo` lists, avoid
  repeated `TextRange` constructions in `CollectSpans`, optimize splay tree
  node walks).

Tests:

- `Stress_LargeDocument_CreateAndFormat`: create 1000 paragraphs, verify
  format completes within 5 seconds.
- `Stress_LargeDocument_UndoRedo`: create 500 paragraphs, undo all, redo
  all, verify correctness.
- `Stress_LargeDocument_CopyPasteCreated`: create 100 paragraphs, select
  all, copy, paste into new document, verify content matches.
- `Stress_DeeplyNestedInlines_Format`: create a paragraph with 100 nested
  Bold/Italic/Span elements, verify format completes.

Files modified:

- `src/LeXtudio.Windows/MS.Internal/Florence/FlorenceEngine.cs` — optimize
  `CollectSpans` and `FormatParagraph` if bottlenecks are found.
- `src/LeXtudio.Windows/.../Documents/TextContainer.cs` — optimize splay
  tree operations if needed.
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  stress tests.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- Consumer gap triage and documentation.
