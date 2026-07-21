# Session 122

## Re-auditing `docs/datagrid-compare.md`'s "gap" claims

`docs/session121.md` had just closed out with the sort-arrow/hover VSM fix when a
re-read of `docs/datagrid-compare.md` (written earlier in session 121) turned up a
stale claim: the Editing section said WPF's `AddNew`/new-item-placeholder/`CancelEdit`
"are not supported" by `ItemCollection`. Reading `ItemCollection.cs` directly showed
real, working implementations of all three (`CanAddNew`, `AddNewItem`, `CancelEdit`,
`NewItemPlaceholderPosition`), plus a genuine "*" new-item-placeholder row wired
end-to-end through `DataGrid.CanUserAddRows` → `EnsureShimNewItemPlaceholderState()` →
`DataGridCell.BeginEdit()`'s placeholder-sentinel check → `DataGrid.ShimBeginEditPlaceholder`
→ `ItemCollection.AddNew()`. `git log` confirmed this had shipped in an earlier commit
(`5958a9f`, "Fill more DataGrid gaps") — the doc simply never caught up. Fixed by
rewriting that section with the real mechanism and file references.

Given one claim was stale, every other "gap"/"not implemented" claim in the doc was
re-verified against current code rather than trusted:

- **Accessibility/UI Automation "not implemented"** — confirmed still accurate.
  `AutomationPeer.FromElement`/`ListenerExists` are still hard-stubbed, `OnCreateAutomationPeer`
  still returns `null` everywhere, and `docs/session121.md` independently reaffirms
  "still fully inert" as the most recent status. No drift here.
- **`GroupStyle.Panel` not shimmed** — confirmed accurate; `GroupStyle.cs`'s own header
  comment documents this as a deliberate, still-current scope cut.
- **`DataGridHyperlinkColumn` not a true inline `Hyperlink`** — confirmed accurate;
  the `TextBlock`+`Tapped` design and rationale in `DataGridHyperlinkColumn.cs` match
  the doc exactly.
- **Filter extension (`DataGridFilterColumn`/`IsAutoFilterEnabled`)** — confirmed to
  exist as described.

Only the summary table's mirrored AddNew claim needed the matching fix.

## TextSearch: from dead code to a real, tested feature

The README's "known gaps" line for keyboard incremental search said: *"a simplified
approximation of WPF's `TextSearch.DoSearch` (no `NavigateToItem`; falls back to
focusing the matched row)"* — implying a working-but-limited feature. Investigating
this (prompted by the same "don't trust the doc, verify" pattern from the AddNew
finding) turned up something more basic: **`TextSearch.DoSearch` was never called
from anywhere.** `ItemsControlSpine.OnTextInput` — the hook real WPF's `ItemsControl`
overrides to call `TextSearch.EnsureInstance(this).DoSearch(e.Text)` — was an empty
virtual stub, and `DataGrid` never overrode it. The whole feature was unreachable from
any keyboard input path, not "simplified" — the local `TextSearch` class itself did
work in isolation, it just had no caller.

### What shipped this session

1. **Wired keyboard input to `TextSearch.DoSearch`.** WinUI's `KeyRoutedEventArgs`
   doesn't expose composed/IME text the way WPF's `TextCompositionEventArgs` does, so
   rather than hunt for an equivalent routed event, `DataGrid.OnKeyDown`'s existing
   `switch` (already home to arrow/Home/End/PageUp/PageDown navigation) gained a
   `default:` case: unmodified letter/digit `VirtualKey`s map directly to characters
   (`ShimVirtualKeyToChar`) and feed `TextSearch.EnsureInstance(this).DoSearch(...)`.
   Covers the actual use case (typing a name prefix) without porting IME composition.

2. **`ItemsControl.NavigateToItem` — a real, reusable port**, added to
   `ItemsControlSpine.cs` as `internal virtual void NavigateToItem(object? item)`.
   Scoping this looked expensive at first (real WPF's `NavigateToItem`/`MakeVisible`
   depend on `IScrollInfo`/`ScrollHost`/`IsOnCurrentPage` viewport geometry that
   doesn't exist in this shim), but `DataGrid` already had everything needed:
   `MoveSelectionToIndex` (used by Home/End/PageUp/PageDown) already does
   scroll-into-view-then-select via `OnBringItemIntoView` + `ItemContainerGenerator`.
   `DataGrid.NavigateToItem` just delegates to it. The spine's base implementation
   (a plain focus-if-already-realized fallback) is what the old `TextSearch` did
   inline; it's now a named, overridable hook instead of unreachable dead logic.

3. **Timer-based prefix reset**, replacing the old fixed-length truncation (which
   never reset on a pause, just capped the accumulated string at 20 chars). Real
   WPF uses a `DispatcherTimer` (`ResetTimeout`/`OnTimeout`) keyed off 2x the OS
   double-click interval; this uses a flat 1000ms `DispatcherTimer` instead of
   querying the platform double-click time. No new shim needed — WinUI's own
   `Microsoft.UI.Xaml.DispatcherTimer` already resolves through the project's global
   `using Microsoft.UI.Xaml`, and turned out to already be in a real (if narrow) use
   at `MS.Internal.Documents/FlowDocumentView.uno.cs:30` for caret blink — same
   `Interval`/`Tick`/`Start`/`Stop` shape as real WPF's timer, just no
   `DispatcherPriority` constructor overload.

4. **Regression coverage added where there was none.** A new
   `datagrid.probe.text-search` DevFlow probe (via reflection, since constructing a
   real `KeyRoutedEventArgs` isn't practical in the headless host) verifies
   `ShimVirtualKeyToChar` maps `VirtualKey.A` → `"A"` and rejects `Enter`, then calls
   `TextSearch.DoSearch` directly and confirms `DataGrid.SelectedIndex` actually
   moves — i.e., the match reached `NavigateToItem`. New xUnit test
   `TextSearch_DoSearchNavigatesToMatchedItem` in `DataGridIntegrationTests.cs`
   exercises this against the real running app (29/30 integration tests passing, the
   1 skip pre-existing and unrelated).

### Follow-up: closed both remaining gaps

After landing the above, the user asked directly whether the two remaining
differences called out in the README were worth closing now, and opted to do both:

1. **`TextSearch.TextPath`** — a new attached property, named and shaped after real
   WPF's `TextSearch.TextPathProperty`/`GetTextPath`/`SetTextPath`, settable on the
   `ItemsControl` itself. `TryMatchAndSelect` now resolves per-item search text via
   (in order) `TextPath` reflection lookup → `TextSearch.GetText` → `item.ToString()`.
   Only a single, non-nested property name is resolved (plain reflection, not a real
   `PropertyPath`/`BindingExpression` walk) — covers the common "search by this
   property" case (e.g. `TextSearch.SetTextPath(grid, "Name")` for a plain POCO with
   no `ToString()` override) without the much larger effort of porting WPF's actual
   property-path engine.
2. **Real double-click-interval-based reset timeout** — `ResetTimeout` reads
   `System.Windows.SystemParameters.DoubleClickTime * 2`, matching real WPF's
   `2 * GetDoubleClickTime()` formula exactly, instead of a hardcoded 1000ms.

`datagrid.probe.text-search` extended to search on `MetadataRow.Name` via `TextPath`
for `"Type15"` (deliberately not `"Type1"`, which prefix-matches `Type1`/`10`-`19` —
`"Type15"` is unique, so a correct match must land exactly on index 14/RID 15,
proving real per-item property resolution rather than accidentally matching index 0
via the old ToString()-only path). `TextSearch_DoSearchNavigatesToMatchedItem`
extended with the matching assertions.

### Follow-up 2: `SystemParameters.DoubleClickTime` — from stub to real OS query

After landing the above, the user pushed back on leaving `DoubleClickTime` itself as
a fixed 500ms guess: both Windows and macOS have a real value obtainable via P/Invoke,
so it should be queried for real, not approximated.

- **Windows**: `user32!GetDoubleClickTime()` — direct, milliseconds, no surprises.
- **macOS**: there's no C API for this, only AppKit's `NSEvent.doubleClickInterval`
  class property (`NSTimeInterval`, i.e. a `double` in seconds). Read via the
  Objective-C runtime directly (`objc_getClass`/`sel_registerName`/`objc_msgSend`) —
  the same mechanism any Objective-C caller uses, no AppKit binding library needed
  for a single property read. Two ABI details mattered and were verified live rather
  than assumed:
  - **Floating-point return calling convention differs by architecture.** x86_64
    requires the `objc_msgSend_fpret` entry point for a `double`-returning call;
    arm64's unified calling convention means the plain `objc_msgSend` symbol works
    for every return shape. Both are declared; the one bound is picked at runtime via
    `RuntimeInformation.ProcessArchitecture`.
  - **AppKit isn't guaranteed already loaded into the process.** A first standalone
    test (a throwaway `dotnet run` console app, not the real shim) called
    `objc_getClass("NSEvent")` without loading AppKit first and got back `NULL` —
    the property read then silently returned `0` instead of failing loudly. Fixed by
    `dlopen`-ing `AppKit.framework` before resolving the class (cheap/idempotent if
    already loaded, which the real Uno/Skia desktop app's native window chrome
    almost certainly has done already).

Verified end-to-end against the real, running app rather than trusted from the build
alone: this dev machine's actual double-click threshold is a non-default 5000ms
(`defaults read -g com.apple.mouse.doubleClickThreshold` → `5`), and the live
`datagrid.probe.double-click-time` probe returned exactly `5000` — an exact match,
not a coincidence, confirming the native call reads the real live setting rather than
some other cached/default value. New xUnit test
`SystemParameters_DoubleClickTimeQueriesRealOsValue` asserts only that the value is a
real positive number (not the specific 5000, which is this machine's own
non-default configuration, not a portable invariant).

### Follow-up 3: nested `TextPath` segments, by reusing existing binding-shim code

The user pushed back a second time on the "only a single, non-nested property name"
scope cut in follow-up 1 above, insisting on reusing real WPF-shaped code rather than
a narrower hand-rolled lookup. Checking what already existed in the binding shim
turned up `System.Windows.Data.BindingExpression.EvaluatePath(object item, string
path)` (`BindingExpression.cs:68`) — a real dotted-path walker, already used by the
selector spine's untargeted binding expressions, that splits on `.` and resolves each
segment via reflection, returning `MS.Internal.Data.BindingValue.UnsetValue` on any
failure. This is exactly what `TextSearch.TextPath` needed, so rather than duplicate
path-splitting logic, `EvaluatePath` was widened from `private` to `internal` and
`TextSearch.GetItemText` now calls it directly instead of doing its own
single-property `GetType().GetProperty(...)` lookup.

Net effect: `TextSearch.TextPath="Owner.Name"` now works, not just
`TextSearch.TextPath="Name"` — closing the "not nested paths" caveat from follow-up 1
by reuse rather than a new parser. Verified with a new nested property
(`MetadataRow.Owner.Name`, values `"Owner1".."Owner20"`) added to the integration
host's sample data, and a corresponding probe/test extension
(`selectedIndexByNestedTextPath`/`nestedTextPathMatchedOwner15`, same
unique-match-at-index-14 pattern as the flat-property case) confirming the nested
path resolves to the correct row, not just falling through to a coincidental match.

No further gaps remain open in this area — the earlier README wording ("simplified
approximation... falls back to focusing the matched row") both undersold the actual
gap (dead code, not "simplified") and is now moot regardless, since the wiring and
all three behavioral shortfalls it implied (no scroll-into-view, no property-path
matching — including nested paths, and a fixed reset timeout) are fixed.

## Files touched

- `docs/datagrid-compare.md` — AddNew/CancelEdit/placeholder section + summary table corrected.
- `README.md` — TextSearch gap description rewritten across three follow-ups as TextPath/DoubleClickTime/nested-paths landed.
- `src/LeXtudio.Windows/System.Windows/Data/BindingExpression.cs` — `EvaluatePath` widened from `private` to `internal` for reuse by `TextSearch`.
- `src/LeXtudio.Windows/System.Windows/Controls/ItemsControlSpine.cs` — `NavigateToItem` added.
- `src/LeXtudio.Windows/System.Windows/Controls/DataGrid.cs` — `NavigateToItem` override, `OnKeyDown` default case, `ShimVirtualKeyToChar`.
- `src/LeXtudio.Windows/System.Windows/Controls/DataGridHelperStubs.cs` — `TextSearch` timer-based reset (now `SystemParameters.DoubleClickTime`-driven), `TextPath` attached property + `GetItemText` reflection lookup, routes matches through `NavigateToItem`.
- `tests/DataGrid.IntegrationTestHost/MainPage.cs` — `datagrid.probe.text-search` (ToString() case + TextPath case).
- `tests/DataGrid.IntegrationTests/DataGridIntegrationTests.cs` — `TextSearch_DoSearchNavigatesToMatchedItem` (extended).

Full suite: 229/229 unit, 29/30 integration (1 pre-existing unrelated skip).
