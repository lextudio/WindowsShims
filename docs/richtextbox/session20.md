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
