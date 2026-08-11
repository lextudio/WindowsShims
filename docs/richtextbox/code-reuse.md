# Code Reuse and Conditional Compilation Audit

Status as of session 125 (2026-08-11), Uno 6.6, all suites green
(RichTextBox 238/238, model 234/234, DataGrid 62/62).

## The strategy

`src/LeXtudio.Windows/LeXtudio.Windows.csproj` links the upstream WPF source
tree (`ext/wpf` submodule, fork of `dotnet/wpf`) **by file inclusion**:

- `linked-upstream`: an upstream file compiles directly, with narrow
  `#if HAS_UNO` guards where platform differences require them.
- `local-shim` / `.uno.cs` partial: a local file supplies Uno-specific
  behavior a linked file depends on (rendering, input forwarding, DP
  registration), never a WPF-surface approximation.
- `deferred`: an upstream family intentionally excluded from the build via a
  `Compile Remove` wildcard (the M5-equivalent scope cut).

The historical direction of travel was away from "local shell approximations
with fork guards everywhere" toward "link the real upstream spine and bridge
the remainder" — see `RICHTEXTBOX-PORT-CATALOG.md` "Status Key" and the
DataGrid `index.md` row for session 22 ("third link attempt succeeded;
248 → 0 sites").

## Verified numbers (measured, not recalled)

Measurements are reproducible via `scripts/count-guards.py` (repo root;
`--gate` enforces the budget in CI).

### WPF code reuse

| Metric | Value |
|---|---|
| `ext/wpf` files linked via `Compile Include` (net of `Compile Remove`) | **313** |
| Linked `System.Windows.Documents` files | 203 |
| Lines of linked upstream WPF code | ~174,700 (Documents family ~119,300) |
| Documents files compiling **with zero guards** (pristine upstream) | **172 / 203 (85%)** |
| Files carrying any `HAS_UNO` guard | 41 |
| Shim-local code (`.uno.cs` partials + bridges) | ~22,400 lines |

The linked set is essentially the whole non-deferred WPF document/editing
model: `TextContainer`, `TextPointer`, `TextRange`/`TextRangeBase`, the
`TextRangeEdit*`/`TextEditor*` family (typing, paragraphs, lists, tables,
selection, copy/paste, drag/drop, context menu), the `Inline`/`Block` family
(`Run`, `Span`, `Paragraph`, `List`, `ListItem`, `Table*`, `Hyperlink`), undo
(`TextTreeUndo*`), and `TextRangeSerialization`. Reuse ratio is roughly
**8:1** linked WPF lines vs shim lines.

### Conditional compilation

| Location | Guards |
|---|---|
| Linked Documents family — `#if HAS_UNO` blocks | 100 |
| Linked Documents family — `#if !HAS_UNO` blocks | 92 (13 larger than 2,000 chars) |
| Guard density (Documents family) | 0.083% of lines |
| Shim side | 18 `HAS_UNO`, 26 `WINDOWS_APP_SDK`, 5 `WINUI_BRIDGE` |
| `Compile Remove` exclusions (deferred families) | 35 |

Guard profile of the large blocks:

- `#if !HAS_UNO` (dead under Uno, original WPF implementation compiled out):
  `TextEditorDragDrop.cs` (~31k — the OLE drag-drop process, **deleted** in
  the cleanup pass below), `TextMapOffsetErrorLogger.cs` (~10k — debug error
  logger, **re-excluded with the Speller family**), `TextElement.cs` (~4.8k —
  docs), `TextEditorCopyPaste.cs` (two blocks, ~6.4k total — TSF/`TextStore`
  clipboard paths), `RtfToXamlReader.cs` (~4k — the `WpfPayload`-package
  image path).
- `#if HAS_UNO` (Uno replacement):
  `RtfToXamlReader.cs` (~9.9k — `\pict` → self-contained data-URI image
  emission), `DPTypeDescriptorContext.cs` (~4k — **extracted to a shim
  partial** in the cleanup pass below), plus narrow guards in
  `TextEditorDragDrop` (drag-drop returns early — real drop handling lives in
  the shim), `TextEditorContextMenu`, `TextPointer`, `TextEditorMouse`.

Historical claims spot-checked and confirmed: session 35 removed the last two
one-off fast paths from `RichTextBox.uno.cs` (behavior now runs through the
migrated upstream editing commands); the DataGrid session 22 link pass
brought guarded call sites from 248 to 0.

## What remains and why it stays

1. **Platform-object differences cannot be deleted.** WinRT-aliased structs
   (`FontWeight`, `FontStyle`, ...), `#if WINDOWS_APP_SDK` constructor
   differences, and the lack of DP property inheritance on Uno are real
   platform gaps; the shim bridges them with narrow guards or small partials.
2. **The `#if !HAS_UNO` halves are insurance, not waste.** Keeping the
   original OLE/TSF paths in the linked files means the port can be built
   against real WPF for diffing and future feature ports; they cost nothing
   at runtime and only a little compile time.
3. **Deferred families (34 `Compile Remove`)** are scope cuts by design
   (`TextServices`, `TextStore`, `ImmComposition`, `Speller`,
   `NLGSpellerInterop`, `Fixed`/`DocumentSequence`, `ColumnResize*`,
   `AnchoredBlock`, ...). Spell-check and IME were re-integrated through
   Uno-native bridges instead (`ISpellCheckingService`, `LeXtudio.UI.Text.Core`).

## Proposals for further improvement

Prioritized by expected value. None of these are required for correctness;
the milestone backlog is complete.

### P1 — Make the guard budget measurable and policed — DONE

- `scripts/count-guards.py` reproduces the table above (linked-file count,
  pristine-file count, per-family `HAS_UNO` block counts and density,
  file-level-guarded files, big-block list, `Compile Remove` count), with
  `--json` output and a `--gate` mode.
- CI (`ci.yml`) runs `scripts/count-guards.py --gate` on macOS/Ubuntu before
  building. The gate currently enforces: Documents-family guard density
  ≤ 0.25%, pristine share ≥ 75%, and no forward `HAS_UNO` block over 4,000
  chars (reverse `#if !HAS_UNO` halves are the retained upstream WPF code and
  are exempt by design).
- To keep the measured numbers honest, the baseline section of this document
  should be refreshed whenever the budget shifts (it already went stale
  twice: 235/235→238/238, 53/54→62/62).

### P2 — Shrink the dead `#if !HAS_UNO` halves where fidelity no longer pays — PARTIALLY DONE

- `TextEditorDragDrop.cs`: the ~31k OLE path is permanently dead under Uno
  (every build defines HAS_UNO; `TextEditor.cs` only instantiates
  `_DragDropProcess` under `#if !HAS_UNO`). **Deleted** the whole
  `_DragDropProcess` class (960 → 305 lines); `_RegisterClassHandlers` is now
  an empty method and the Uno stub (`_DragDropProcessUno`) remains the
  `IDragDropProcess` implementation. Deleted code stays in git history.
- `TextMapOffsetErrorLogger.cs` (~10k): a nested `Speller` partial that the
  `Speller*.cs` `Compile Remove` wildcard missed; it compiled to nothing under
  its own `#if !HAS_UNO` file guard. **Re-excluded** via `Compile Remove` so
  the link set honestly reflects the deferred family.
- Remaining audit candidates: `TextEditorCopyPaste.cs` TSF blocks (~6.4k),
  `TextElement.cs` docs (~4.8k), `RtfToXamlReader.cs` WpfPayload image path
  (~4k). Each is smaller, still upstream-faithful code, and needs the same
  "will it ever compile here?" question answered before deletion.

### P3 — Consider moving the large `#if HAS_UNO` replacements into partials — STARTED

- `DPTypeDescriptorContext` (~4k): **done** — the Uno converter routing moved
  to `src/LeXtudio.Windows/System.Windows/Documents/DPTypeDescriptorContext.uno.cs`
  (`TryGetShimStringValue`); the linked file keeps a one-line call-site guard
  and a `partial` modifier.
- The remaining candidates were evaluated and **left in place**, for distinct
  reasons:
  - `RtfToXamlReader`'s data-URI image path (~9.9k) is inline code inside
    `ProcessImage` and touches four private fields (`_converterState`,
    `_imageCount`, `_lexer`, `_wpfPayload`); splitting it out would force the
    host to expose internal state, so the in-file guard is the cheaper option.
  - `TextRangeSerialization.WriteEmbeddedObject` (~4.9k) is a method-level
    `#if HAS_UNO`/`#else` pair of the same method; C# partials cannot host two
    definitions of one method, so a split would need an indirection method for
    no net guard savings.
  - Both blocks are stable (unchanged across the RTF/XAML image work in
    sessions 96/105) and covered by round-trip tests.

### P4 — Reduce `#if WINDOWS_APP_SDK` (26 occurrences, shim side) — NOT STARTED

- Most are 1–2-line WinRT-vs-Uno constructor/coercion differences. Group them
  behind small helpers (e.g. a `WinRt.Factory`-style indirection) so the
  per-site guards collapse to calls, or at least document each site's purpose
  in one place so the count stops drifting.

### P5 — Periodic deferred-family re-review (M5)

- The 35 `Compile Remove` families should be revisited when a consumer asks
  for one, with a cheap triage: link-and-compile attempt, then count the
  guards the attempt needs. `TextSchema`/`TextFlow`/`FlowNode`/`FlowPosition`
  are the likely first candidates if fixed-layout rendering or advanced text
  features are ever needed. The `NaturalLanguageHyphenator` family is a
  good "never" candidate — WPF itself ships it only for legacy line-breaking.

### P6 — Cross-cutting guard census across namespaces — DONE

- `scripts/count-guards.py` reports the guard budget per WPF family
  (Documents, Controls, Media, Markup, Input, WindowsBase, ...), not just
  Documents. Current repo-wide totals: 313 linked files, ~174.7k lines, 195
  `HAS_UNO` blocks, 263 pristine files, 35 `Compile Remove` exclusions.

## Bottom line

The port already does the right thing: link the real WPF source, guard
narrowly, defer deliberately. The remaining opportunities are hygiene
(measure + police the budget), not architecture (no fundamental re-shape is
needed).
