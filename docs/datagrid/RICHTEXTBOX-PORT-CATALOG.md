# RichTextBox Port Catalog

This catalog tracks the WPF `RichTextBox` source-first port in
`LeXtudio.Windows`.

`PLAN.md` remains the narrative migration log. This file is the current status
catalog: what source is linked, what is a local shell or bridge, and what blocks
the next migration step.

## Status Key

- `linked-upstream`: upstream WPF source compiles directly or with narrow
  bridges.
- `local-shell`: local implementation exists and intentionally approximates the
  WPF surface.
- `local-bridge`: compatibility layer used by linked upstream files.
- `partial-uno`: split implementation with Uno-specific partial behavior.
- `blocked`: known required item that cannot be enabled yet.
- `deferred`: known WPF source outside the current milestone.

## Current Baseline

- Project: `src/LeXtudio.Windows/LeXtudio.Windows.csproj`
- Target: `net10.0-desktop`
- Baseline build status: green according to `PLAN.md`.
- Current Uno-side test path: dispatcher-bound document/control tests run
  through `LeXtudio.RichText.Tests --uno-runtime-tests`; plain VSTest skips
  those fixtures.

## Minimum RichTextBox Spine

| Area | WPF source / family | Current WindowsShims source | Status | Notes / blocker |
|---|---|---|---|---|
| RichTextBox control | `PresentationFramework/System/Windows/Controls/RichTextBox.cs` | `System.Windows/Controls/RichTextBox.cs` | `local-shell` | Exposes WPF-shaped `Document`, `AppendText`, selection and typing-property entry points. Not yet upstream source. |
| TextBox base | `System.Windows.Controls.Primitives.TextBoxBase` | `System.Windows/Controls/Primitives/TextBoxBase.cs` | `local-shell` | Required base class for the local `RichTextBox` shell. |
| Flow document | `System.Windows.Documents.FlowDocument` | `System.Windows/Documents/FlowDocument.cs` plus linked upstream project item | `local-shell` | Instantiation is verified through the Uno runtime test harness in `UnoRichText/docs/RichTextBox/session3.md`. |
| Text element base | `System.Windows.Documents.TextElement` | `System.Windows/Documents/TextElement.cs`, `TextElement.uno.cs`, `TextElementFontExtensions.cs` | `partial-uno` | Uno partial and extension members provide the active platform adaptation. |
| Inline/block model | `Inline`, `Run`, `Span`, `Bold`, `Italic`, `Paragraph`, `Block`, `BlockCollection` | Linked upstream files plus local `.uno.cs` partials where present | `partial-uno` | Rendering consumer is UnoRichText `RichTextBlock`/`RichTextBox` host. |
| TextPointer | `System.Windows.Documents.TextPointer` | `System.Windows/Documents/TextPointer.cs` | `local-shell` | Provides shape and bridge methods; movement/tree semantics are still shallow. |
| TextContainer | `System.Windows.Documents.TextContainer` | `System.Windows/Documents/TextPointer.cs` | `local-shell` | Co-located with `TextPointer`; generation and movement support are minimal. |
| TextRange | `System.Windows.Documents.TextRange` | `System.Windows/Documents/TextRange.cs` | `local-shell` | Current API surface exists, but full WPF range editing is not complete. |
| TextSelection | `System.Windows.Documents.TextSelection` | `System.Windows/Documents/TextSelection.cs` | `local-shell` | Uses `FormattingDependencyObject` adaptation per `PLAN.md`. |
| TextEditor spine | `TextEditor`, `TextEditor*` helpers | `System.Windows/Documents/EarlyBatchEditorShims.cs` | `local-bridge` | Current helpers are mostly no-op shims. This remains the highest-coupling migration area. |
| Undo | `MS.Internal.Documents.ParentUndoUnit`, related undo units | Linked upstream files plus `MS.Internal.Documents/ParentUndoUnitBridge.cs` | `linked-upstream` / `local-bridge` | Several upstream undo units already compile; undo manager remains intentionally minimal. |
| Serialization | `TextRangeSerialization`, XAML/RTF helpers | `System.Windows/Documents/ValidationAndSerializationShims.cs`, `XamlRtfShims.cs`, linked helper files | `local-bridge` | Enough surface for compilation; full fidelity still needs targeted verification. |
| Word breaking | `SelectionWordBreaker` | linked upstream file plus native-method constants/stubs | `linked-upstream` / `local-bridge` | Native NLS calls are stubbed to fail/zero per `PLAN.md`. |

## Already Migrated Upstream Candidates

See `PLAN.md` for detailed session notes. Current notable completed migrations:

- `RangeContentEnumerator`
- `TextElementEditingBehaviorAttribute`
- `ElementEdge`
- `SelectionHighlightInfo`
- `TextSegment`
- `TextContainerChangeEventArgs`
- `TextElementEnumerator`
- `TextParentUndoUnit`
- `ChangeBlockUndoRecord`
- `Typography`
- `TypographyProperties`
- `SplayTreeNode`
- `UIElementPropertyUndoUnit`
- `SelectionWordBreaker`
- `RowSpanVector`
- `ParentUndoUnit`

## Deferred / Non-Spine Families

These families are useful to track but should not block the first editable
RichTextBox milestone unless the upstream editor spine requires them directly:

- FlowDocument table layout and PTS table helpers
- fixed-document, fixed-page, and paginator families
- document sequence and annotation families
- adorners beyond what caret/selection rendering requires
- advanced typography and formatting beyond editable text fidelity

## Immediate Next Catalog Work

1. Keep this catalog synchronized with each `PLAN.md` migration entry.
2. Add exact linked source paths for the remaining `TextEditor*` helpers before
   attempting another upstream migration.
3. Move the next dispatcher-bound `RichTextBox` host tests onto the runtime
   harness created in `UnoRichText/docs/RichTextBox/session3.md`.
