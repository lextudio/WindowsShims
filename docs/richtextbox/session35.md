### Session 35 - Root-Caused and Fixed the Logical Tree Parent Gap; Removed Both Editing Fast Paths

Status: complete.

Scope:

- Deep-dive the "`Run.Parent` doesn't surface its containing Paragraph"
  limitation flagged repeatedly since Sessions 22-25 and again in Session 33,
  instead of continuing to work around it. Find the actual root cause and fix
  it once, rather than patching individual symptoms.

Investigation method:

- Read the upstream WPF call chain for paragraph merging
  (`TextRangeEdit.DeleteInlineContent` → `startPosition.ParagraphOrBlockUIContainer`
  → `TextPointer.ParentBlock` → walks `Inline.Parent` while
  `parentBlock is Inline`) to find exactly which `.Parent` walk breaks and why
  `TextRangeEditLists.MergeParagraphs` silently no-ops today.
- Traced every caller of `LogicalTreeHelper.AddLogicalChild`/`RemoveLogicalChild`
  in the linked `TextContainer.cs` (element insertion at line ~1139,
  `ReparentLogicalChildren` at line ~2093-2101) — these are upstream WPF's only
  mechanism for keeping a `TextElement`'s `FrameworkContentElement.Parent` in
  sync with its containing scope on every insert/move/remove.
- Found `src/LeXtudio.Windows/System.Windows/LogicalTreeHelper.cs`: both
  methods were literal no-ops (`{ }`), explicitly commented as a stub. This
  means `Run.Parent` (and every other `TextElement.Parent`) was *never* set by
  normal document mutation, for any RichTextBox in this shim — not just during
  paragraph merges.
- Used `DevFlow` probes (`richtextbox.probe.key-down`,
  `set-caret-run-offset`, existing `backspace-command`/`delete-selection-command`)
  plus the RichTextBox host's `/tmp` (`GetTempPath()`) log sink
  (`rtb-template.log`, actually resolved under
  `/var/folders/.../T/rtb-template.log` on macOS) to prove before/after
  behavior empirically: temporarily short-circuited each fast path with
  `if (false && ...)`, reran the paragraph-merge and Enter tests, and grepped
  the log for the fast-path markers to confirm they were never entered while
  the tests still passed.

Root cause:

- `LogicalTreeHelper.AddLogicalChild`/`RemoveLogicalChild` were no-op stubs.
  Every `TextElement` (Run, Paragraph, Span, ...) inserted into a
  `FlowDocument`'s content tree never had its `FrameworkContentElement.Parent`
  set, because the one code path responsible for that (`TextContainer.cs`)
  calls exactly these two methods and nothing else.
- This silently broke every WPF algorithm that walks `TextElement.Parent`
  chains: `TextPointer.ParentBlock` (→ `Paragraph`/`ParagraphOrBlockUIContainer`,
  used by paragraph-merge-on-delete), and
  `TextRangeEditTables.FindTableElements`'s common-ancestor walk (the Session
  33 crash). It's the same underlying gap referenced by the Session 22-25
  `FlowDirection` work, though those sessions worked around it at the DP level
  rather than fixing `Parent` itself.

Product fix:

- `LogicalTreeHelper.AddLogicalChild(parent, child)`: if `child` is a
  `FrameworkContentElement`, set `child.Parent = parent`.
- `LogicalTreeHelper.RemoveLogicalChild(parent, child)`: if `child` is a
  `FrameworkContentElement` and its current `Parent` is (still) `parent`, clear
  it to `null`.
- `LogicalTreeHelper.GetParent` now returns the same `FrameworkContentElement.Parent`
  instead of always `null`.
- This is a minimal, targeted fix — it does not attempt real WPF's full
  logical-tree machinery (visual parent pointers, tree-change events), only
  the `Parent` bookkeeping that the linked WPF document/editing code actually
  depends on.

Validation and fast-path removal:

- With the fix in place, temporarily disabled both custom fast paths in
  `RichTextBox.uno.cs` (`OnKeyDown`): the paragraph-merge fast path (Backspace
  at paragraph start / Delete at paragraph end,
  `TextRangeEditLists.MergeParagraphs` called directly) and the Enter fast
  path (manually appending a new `Paragraph` to `Document.Blocks`).
- Reran `KeyDown_DeleteAtParagraphEnd_MergesNextParagraph`,
  `KeyDown_BackspaceAtParagraphStart_MergesPreviousParagraph`, and
  `KeyDown_Enter_InsertsParagraphBreak` with both fast paths disabled: **all
  three still passed**, and the `/tmp` log confirmed zero fast-path log lines
  were emitted — the real upstream `TextEditorTyping.OnBackspace`/`OnDelete`/
  `OnEnterBreak` command handlers now correctly merge/split paragraphs on
  their own.
- Ran the full RichTextBox suite with both fast paths disabled: **76/76
  passed**.
- Concluded both fast paths were now dead code and deleted them permanently
  from `RichTextBox.uno.cs`, along with their now-unused helpers
  (`FindEditingBlock`, `IsAtParagraphEndForDelete`, `DescribeBlock`).

Regression sweep (after permanent removal):

- `dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop`
  → 0 errors.
- RichTextBox integration suite: 76/76.
- DataGrid integration suite (cross-cutting shim regression check, since
  `LogicalTreeHelper` is shared infrastructure): 52 passed, 1 pre-existing
  skip, 0 failed.
- `LeXtudio.Windows.Tests` NUnitLite suite (document-model tests): 233/233.

Command:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet test tests/DataGrid.IntegrationTests/DataGrid.IntegrationTests.csproj --logger "console;verbosity=minimal"
dotnet run --project src/LeXtudio.Windows.Tests/LeXtudio.Windows.Tests.csproj -f net10.0-desktop -- --labels=Before
```

Result:

```text
LeXtudio.Windows: 0 errors
RichTextBox.IntegrationTests: 76/76
DataGrid.IntegrationTests: 52/53 (1 pre-existing skip)
LeXtudio.Windows.Tests (NUnitLite): 233/233
```

Why this matters beyond RichTextBox:

- `LogicalTreeHelper`/`FrameworkContentElement.Parent` are shared shim
  infrastructure, not RichTextBox-specific. Any other migrated WPF code that
  walks `TextElement.Parent`/`FrameworkContentElement.Parent` chains — now and
  in future sessions — gets this fix for free instead of needing its own
  workaround, the way Sessions 22-25's FlowDirection work and Session 33's
  paragraph-merge crash both did.
- This directly satisfies M3's "Done when" bar: "The bridge code stops relying
  on one-off fast paths for common editing cases" — both remaining
  `RichTextBox.uno.cs` fast paths are gone, and the corresponding behavior now
  runs through the real, migrated upstream WPF editing commands.

Next session:

- M3 (Editing Spine) now looks fully done per its own bar. Good next
  candidates: selection replacement via the real key path (typing over a
  selection through `OnKeyDown`/character input rather than `OnTextInput`
  directly), or a fresh audit of other `TextElement.Parent`-dependent code
  paths (e.g. table support, list handling) that may now work correctly for
  the first time and could gain test coverage cheaply.
