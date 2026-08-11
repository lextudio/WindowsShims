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

### WPF code reuse

| Metric | Value |
|---|---|
| `ext/wpf` files linked via `Compile Include` (net of `Compile Remove`) | **314** |
| Linked `System.Windows.Documents` files | 244 |
| Lines of linked upstream WPF code | ~175,700 (Documents family ~143,845) |
| Documents files compiling **with zero guards** (pristine upstream) | **200 / 244 (82%)** |
| Files carrying any `HAS_UNO` guard | 44 |
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
| Linked Documents family — `#if HAS_UNO` blocks | 100 (7 larger than 2,000 chars) |
| Linked Documents family — `#if !HAS_UNO` blocks | 92 (13 larger than 2,000 chars) |
| Guard density (Documents family) | 0.13% of lines |
| Shim side | 18 `HAS_UNO`, 26 `WINDOWS_APP_SDK`, 5 `WINUI_BRIDGE` |
| `Compile Remove` exclusions (deferred families) | 34 |

Guard profile of the large blocks:

- `#if !HAS_UNO` (dead under Uno, original WPF implementation compiled out):
  `TextEditorDragDrop.cs` (~31k — the OLE drag-drop process),
  `TextMapOffsetErrorLogger.cs` (~10k — debug error logger),
  `TextElement.cs` (~4.8k — docs), `TextEditorCopyPaste.cs` (two blocks,
  ~6.4k total — TSF/`TextStore` clipboard paths), `RtfToXamlReader.cs` (~4k —
  the `WpfPayload`-package image path).
- `#if HAS_UNO` (Uno replacement):
  `RtfToXamlReader.cs` (~9.9k — `\pict` → self-contained data-URI image
  emission), `TextRangeSerialization.cs` (~4.9k — `WriteEmbeddedObject` data
  URI instead of OPC package URIs), `DPTypeDescriptorContext.cs` (~4.1k —
  `TypeConverter` bridge for WinRT-aliased structs), plus narrow guards in
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

### P1 — Make the guard budget measurable and policed

- Add `scripts/count-guards.sh` (or a small Python script) reproducing the
  table above: linked-file count, pristine-file count, `HAS_UNO` block count
  and density, big-block list, `Compile Remove` count.
- Wire it into CI as a soft gate: fail (or report) when guard density grows
  beyond ~0.2% or when a new `#if HAS_UNO` block exceeds ~2,000 chars without
  a review note. This is the cheapest way to keep the 82%-pristine property
  from eroding as new families get linked.
- Record the measured numbers in `RICHTEXTBOX-PORT-CATALOG.md` (the baseline
  section already went stale twice: 235/235→238/238, 53/54→62/62).

### P2 — Shrink the dead `#if !HAS_UNO` halves where fidelity no longer pays

- `TextEditorDragDrop.cs`: the ~31k OLE path is permanently dead under Uno
  (shim handles drop via its own `FlowDocumentView` path). Either delete the
  `#else` branch (keeping the file) or extract the OLE body into a
  `TextEditorDragDrop.Ole.cs` that only real-WPF builds include. Saves ~31k
  lines of compiled-out source and makes the HAS_UNO path the single source
  of truth for the shim.
- `TextMapOffsetErrorLogger.cs` (~10k): a DEBUG-only logger the shim replaces;
  consider `Compile Remove` + a 30-line shim if a consumer ever needs it.
- Audit the remaining `#if !HAS_UNO` blocks with the same question: is the
  original half ever going to be exercised here, or is it archaeology?

### P3 — Consider moving the 7 large `#if HAS_UNO` replacements into partials

- `RtfToXamlReader`'s data-URI image path and
  `TextRangeSerialization.WriteEmbeddedObject` are big enough to become
  `RtfToXamlReader.uno.cs` / `TextRangeSerialization.uno.cs` partials,
  keeping the linked files 100% pristine and shrinking the guard footprint to
  a single `partial class` declaration.
- Trade-off to evaluate first: the replacements share private local state with
  their host file (member access). The current in-file guard is a deliberate,
  documented choice for exactly this reason (see the catalog's note about
  `CanSave`/`CanLoad`/`Save`/`Load`); a partial split only wins if the
  replacement body is self-contained.
- Same question for `DPTypeDescriptorContext` (~4k) — likely fine where it is.

### P4 — Reduce `#if WINDOWS_APP_SDK` (26 occurrences, shim side)

- Most are 1–2-line WinRT-vs-Uno constructor/coercion differences. Group them
  behind small helpers (e.g. a `WinRt.Factory`-style indirection) so the
  per-site guards collapse to calls, or at least document each site's purpose
  in one place so the count stops drifting.

### P5 — Periodic deferred-family re-review (M5)

- The 34 `Compile Remove` families should be revisited when a consumer asks
  for one, with a cheap triage: link-and-compile attempt, then count the
  guards the attempt needs. `TextSchema`/`TextFlow`/`FlowNode`/`FlowPosition`
  are the likely first candidates if fixed-layout rendering or advanced text
  features are ever needed. The `NaturalLanguageHyphenator` family is a
  good "never" candidate — WPF itself ships it only for legacy line-breaking.

### P6 — Cross-cutting guard census across namespaces

- This audit covered `System.Windows.Documents` only. `Controls` (DataGrid
  family), `Media`, `Markup`, and `WindowsBase`-linked files carry their own
  guards (e.g. the DataGrid bridge, `Automation` stubs). Run the same
  measurement across all linked namespaces once, so the "guard budget" is a
  repo-wide number, not a per-family one.

## Bottom line

The port already does the right thing: link the real WPF source, guard
narrowly, defer deliberately. The remaining opportunities are hygiene
(measure + police the budget), not architecture (no fundamental re-shape is
needed).
