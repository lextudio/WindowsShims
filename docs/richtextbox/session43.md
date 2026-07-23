### Session 43 - Table Construction Audit; Real IME Integration via LeXtudio.UI.Text.Core

Status: complete.

Scope:

- Two threads: (1) audit `TextRangeEditTables`/table construction for the same
  class of `Parent`-chain bug found in lists (Session 35-37), per Session
  42's recommendation; (2) integrate real OS-level IME composition using the
  shared `LeXtudio.UI.Text.Core` package, which the user pointed out is
  already used by sibling projects (UnoEdit) for exactly this purpose.

## Part 1 — Table construction audit

- Checked whether tables render at all: `FlorenceLayoutEngine.Format` only
  walks `document.Blocks.OfType<Paragraph>()` — `Table`/`List` blocks are
  invisible to the visual layout engine entirely, confirming M5's "full table
  layout fidelity" deferral is really "no table layout, period," not a partial
  gap. This also retroactively clarifies Sessions 37-40: those List tests only
  ever validated the *document/editing model*, not visual rendering.
- Added `RichTextBoxScenarios.BuildTableDocument(...)`, building a 2x2
  `Table`/`TableRowGroup`/`TableRow`/`TableCell` tree directly via
  constructors (mirroring `BuildListDocument`). Construction did **not**
  crash — `SetTableDocument_BuildsTableWithoutCrashing` confirms
  `firstBlockType: "Table"` and correct plain-text extraction
  (`"a\tb\nc\td\n"`).
- Investigated `TableCell`/`TableRow`/`TableRowGroup`'s `OnNewParent`
  override (upstream WPF's hook for syncing a reparented cell into its new
  row's `Cells` collection, etc.) — confirmed this virtual is never invoked
  anywhere in the shim (`FrameworkContentElement.Parent` is a bare
  auto-property with no dispatch, unlike real WPF). This is a latent gap
  *distinct* from Session 35's fix: `ListItem` never overrides `OnNewParent`
  so lists were unaffected, but `TableCell`/`TableRow`/`TableRowGroup`/
  `FlowDocument` do. However, `TableCellCollection.Add`/`TableRowCollection.Add`
  etc. maintain their own internal storage directly (not via the
  `OnNewParent` side effect), so the normal construction path (used by the
  test above and by any real editing code that goes through the public
  collection APIs) is unaffected. The risk is narrower than it first
  appeared: only a hypothetical code path that reparents a `TableCell` via a
  bare `.Parent =` assignment (bypassing `Cells.Add`/`Insert`/`Remove`) would
  hit it, and no such path was found in the currently-linked
  `TextRangeEditTables.cs`.
- Conclusion: no crash, no smoking gun bug proportional to the effort of a
  deeper chase, and tables aren't rendered anyway. Left as a documented
  low-priority risk (see `docs/RICHTEXTBOX-PORT-CATALOG.md`) rather than
  pursued further — matches Session 42's own guidance to prioritize by actual
  consumer need over speculative mining.

## Part 2 — Real IME integration

Initial investigation found the macOS Uno Skia desktop backend ships zero
composition/marked-text strings in its runtime assembly — looked like a hard
platform-level dead end. The user then pointed at
`/Users/lextm/uno-tools/TextCore.Uno/src/LeXtudio.UI.Text.Core`, a
UI-framework-agnostic cross-platform IME bridge (`CoreTextEditContext` +
Win32/macOS-native-AppKit/Linux-IBus adapters) already consumed by UnoEdit —
reversing that conclusion entirely.

### Integration

- `LeXtudio.UI.Text.Core` (NuGet, version pinned centrally in
  `src/Directory.Packages.props`) referenced unconditionally from
  `LeXtudio.Windows.csproj` (per explicit instruction — no dual
  ProjectReference/PackageReference toggle like the Win2D pattern).
- New file `System.Windows/Controls/RichTextBox.Ime.uno.cs` (partial,
  `#if HAS_UNO`):
  - `EnsureImeContext()` (called from `OnApplyTemplate`): creates a
    `CoreTextEditContext` via `CoreTextServicesManager.GetForCurrentView()`,
    subscribes `TextRequested`/`TextUpdating`/`SelectionRequested`/
    `SelectionUpdating`/`LayoutRequested`/`CompositionStarted`/
    `CompositionCompleted`/`CommandReceived`, resolves the real native window
    handle via reflection into `Uno.UI.Xaml.WindowHelper.GetNativeWindow`
    (mirroring UnoEdit's `TryGetNativeWindowHandle` pattern), and attaches.
  - `TextRequested`: returns `TextRange(document.ContentStart,
    document.ContentEnd).Text`.
  - `TextUpdating`: replaces the requested range with the composed/committed
    text via `TextRange.Text =`, then moves the caret to `range.End`.
  - `SelectionRequested`/`SelectionUpdating`: bridge to/from
    `TextEditor.Selection`.
  - `CommandReceived`: maps ~14 common AppKit selectors
    (`deleteBackward:`, `moveLeft:`, `insertNewline:`, etc.) to existing
    `EditingCommands`, executed the same way `RichTextBox.uno.cs`'s
    Ctrl+B/Ctrl+Z handling already does.
  - `OnKeyDown` gives the IME first refusal via `ProcessKeyEvent` before WPF's
    own key handling; `OnCharacterReceived` skips its own insertion path
    while `_imeComposing` is true (composed text arrives via `TextUpdating`
    instead, avoiding double-insertion).
  - `UpdateCaretFromSelection` now also calls `NotifyImeOfCaretAndSelection`
    (`NotifySelectionChanged`/`NotifyCaretRectChanged`/`NotifyLayoutChanged`)
    so the platform's IME candidate window can track the caret.

### The hard part: offset-space mismatch (a real bug, found and fixed)

`CoreTextEditContext`'s `Range`/`Selection` offsets are indices into the
*plain-text string* `TextRequested` returns — not raw `TextContainer` symbol
offsets. `FlowDocument.ContentStart` sits before the first `Paragraph`'s own
structural boundary, and `Paragraph.ContentStart` in turn sits one unit
before the first `Run`'s `ContentStart` (each `TextElement` contributes its
own `ElementStart` edge as a symbol unit) — so naively resolving IME offsets
from `document.ContentStart` silently corrupted composed text by 1-2 units
per structural level. Found via the exact same "write a test, watch it fail
with a plausible-looking-but-wrong result, trace the real cause" methodology
used in Sessions 35/38: a CJK commit test showed correct text but a caret
offset off by exactly the descent depth, and a range-replace test showed one
extra un-replaced character at the tail.

Fix: `GetImeOffsetBase(document)` descends to the first `Run`'s
`ContentStart` (matching exactly what `Snapshot()`'s own `firstRun.ContentStart.GetOffsetToPosition(...)`
already uses throughout this test suite) instead of `document.ContentStart`.
For the post-insertion caret specifically, using the mutated `TextRange`'s
own `.End` (rather than a second offset-based lookup from the pre-insertion
base pointer) sidesteps a chicken-and-egg case: a freshly empty document has
no `Run` at all until the first character is inserted, so any base pointer
computed *before* the insertion is unreliable for computing a position
*after* it.

**Known limitation**: this offset mapping is only correct for single-paragraph
content (which is what 96 of this suite's tests already exercise). Multi-paragraph/
list/table documents would need a proper plain-text-offset walk (WPF's own
`TextRangeBase.GetTextInternal`-style logic, which already handles paragraph
breaks/list markers correctly for the `.Text` getter) rather than a fixed
structural-descent shortcut — left as a follow-up if a consumer needs
multi-paragraph IME composition.

### Verification methodology

Real OS-level CJK/Japanese/Korean composition can't be scripted headlessly
(confirmed: `MacOSTextInputAdapter.Attach` requires and gets a genuine native
window handle in the DevFlow test host process — `AttachToWindowHandle`
returns `true` in every run, so the bridge really is live — but there's no
way to simulate actual keystrokes into the OS's IME candidate window from an
automated test). Instead, tests call `CoreTextEditContext`'s own **public**
`Raise*` methods directly (`RaiseTextUpdating`, `RaiseCommandReceived`) —
these are the exact same methods the native adapter calls internally when a
real IME commits text or AppKit sends a `doCommandBySelector:` — so the tests
exercise the identical code path a live IME would drive, just with a
synthetic trigger instead of real keystrokes.

New DevFlow probes: `richtextbox.probe.ime-context-state`,
`richtextbox.probe.simulate-ime-text-updating(newText, rangeStart, rangeEnd)`,
`richtextbox.probe.simulate-ime-command(command)`.

Tests added:

- `CreatePlain_AttachesRealCoreTextEditContext` — every `RichTextBox`
  successfully creates and attaches a real `CoreTextEditContext` on
  `OnApplyTemplate` (window handle resolves, `AttachToWindowHandle` returns
  `true`, in this environment).
- `SimulateImeTextUpdating_CommittedCjkComposition_InsertsRealText` — raising
  `TextUpdating` with `"你好"` inserts real CJK text into the document and
  leaves the caret at the correct post-insertion offset.
- `SimulateImeTextUpdating_ReplacesExistingRange` — raising `TextUpdating`
  with a non-empty existing range replaces exactly that range.
- `SimulateImeCommand_MapsToEditingCommandAndReportsHandled`
  (`deleteBackward:`, `moveLeft:`) — command mapping executes the
  corresponding `EditingCommands` and reports `Handled = true`.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet test tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet run --project src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop -- --labels=Before
```

Result:

```text
LeXtudio.Windows: 0 errors
RichTextBox.IntegrationTests: 100/100
DataGrid.IntegrationTests: 52/53 (1 pre-existing skip)
LeXtudio.Windows.Tests (NUnitLite): 233/233
```

Next session:

- Extend the offset mapping to handle multi-paragraph documents correctly
  (proper plain-text-offset walk) if a consumer needs IME composition beyond
  single-paragraph content.
- `CompositionStarted`/`CompositionCompleted` currently only toggle
  `_imeComposing`; there's no visual underline/highlight for in-progress
  (uncommitted) composition text the way real IMEs show it — cosmetic, not
  functional, but worth revisiting if a consumer needs the visual composition
  indicator.
- Consider a live manual smoke test (actually typing Pinyin/Cangjie into a
  running sample app) to validate end-to-end behavior beyond what the
  simulated-event tests can prove, since no automated test can drive real OS
  keystrokes into the native IME candidate window.
