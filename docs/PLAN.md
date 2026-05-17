# WindowsShims Porting Playbook

## Goal

Use upstream WPF source files as the primary implementation wherever possible, and keep local Uno compatibility shims only where they are currently required for net9.0-desktop builds.

This document captures the migration patterns that have already worked so future porting can continue with low risk and fast feedback.

## Current Baseline

- Build target: net9.0-desktop in LeXtudio.Windows
- Baseline status: green (warnings only)
- Working approach: compile-frontier migration
  - Keep upstream includes enabled in project file
  - Remove local duplicates when upstream compiles
  - Keep minimal local adapters when platform differences require them

## What Has Worked

### 1. Solve structural collisions first

Largest progress came from resolving duplicate type collisions before adding more API surface.

Examples:
- Local and upstream TextRange/TextSelection collisions were removed by project-level compile remove entries for local duplicates.
- Sealed type incompatibilities were solved by removing local subclass assumptions in upstream mode.

### 2. Add compatibility in narrow, focused shims

When upstream files expected unavailable behavior, small targeted adapters were more reliable than large rewrites.

Examples:
- Explicit ITextContainer implementation in local TextContainer.
- Local FormattingDependencyObject for scenarios where DependencyObject cannot be directly instantiated in current target.

### 3. Use explicit interface implementations to avoid accessibility breaks

When internal WPF interfaces/types crossed public boundaries, explicit interface members avoided CS0050, CS0051, and related visibility issues while preserving required signatures.

### 4. Migrate one unit at a time and rebuild immediately

The effective loop has been:
1. Move one local shim toward upstream.
2. Build.
3. Fix missing symbols or API gaps.
4. Keep only if green.
5. Revert quickly if it breaks core baseline.

## Confirmed Required Adaptations (Do Not Remove Yet)

### TextSelection springload formatting object

In upstream TextSelection, instantiating DependencyObject directly fails in this target (abstract type error). The current adaptation to FormattingDependencyObject is required until a broader dependency/object-model change is introduced.

Practical rule:
- Keep FormattingDependencyObject substitutions in TextSelection for now.
- Do not spend more cycles trying to force new DependencyObject there without first changing the underlying type mapping strategy.

## Successful Shim Reduction Already Completed

### ValidationHelper moved to upstream

Completed migration:
- Enabled upstream ValidationHelper by removing its project exclusion.
- Removed local ValidationHelper stub from ValidationAndSerializationShims.
- Added required SR resource keys used by upstream ValidationHelper:
  - NotInAssociatedTree
  - TextSchema_TheChildElementBelongsToAnotherTreeAlready

Outcome:
- Build remains green after this migration.

## High-Value Porting Method

Use this exact sequence for each next candidate:

1. Pick one candidate pair
	- Local shim file currently active
	- Upstream file already included but removed, or not yet included

2. Switch compile frontier
	- Remove upstream Compile Remove entry, or add upstream Compile Include
	- Remove local duplicate if type collision is expected

3. Build immediately
	- dotnet build .\LeXtudio.Windows.csproj -f net9.0-desktop | Tee-Object -FilePath .\build-upstream-pass.log

4. Triage compile errors into buckets
	- Missing SR/resource key
	- Signature mismatch
	- Accessibility/visibility
	- Abstract/platform type instantiation
	- Runtime helper dependency not yet ported

5. Apply minimal fix
	- Prefer local bridge additions over upstream source divergence
	- Keep changes tightly scoped to the failing bucket

6. Rebuild and decide
	- If green: keep and move to next candidate
	- If not green after a reasonable pass: revert candidate and log blocker

## Candidate Priority Queue

Recommended near-term order:

1. RangeContentEnumerator
	- Reason: localized behavior and lower expected dependency fan-out than full editor stack

2. Validation/serialization adjacent helpers (excluding full TextRangeSerialization)
	- Reason: small utility classes can be migrated incrementally

3. Table edit/runtime helpers
	- Reason: medium complexity, but often isolated by helper boundaries

4. TextEditor deep pipeline pieces
	- Reason: highest coupling and should remain later-stage work

## Known Risk Patterns

### Resource key drift

Upstream files may reference SR keys not present in local SR shim. Expect CS0117 and treat as normal migration work.

### Abstract/platform type incompatibility

Some WPF assumptions do not map directly to Uno/WinUI aliases. If direct instantiation fails, preserve adapter pattern instead of forcing upstream lines unchanged.

### Hidden fan-out from core editor files

TextEditor, TextTree, and related internals can pull large dependency surfaces quickly. Introduce only when adjacent prerequisites are already green.

## Working Safety Rules

1. Keep baseline green at all times.
2. Make one migration unit per pass.
3. Avoid large combined refactors.
4. Prefer local compatibility bridges over broad upstream edits.
5. If unexpected external edits appear in active files, pause and reconcile before continuing.

## Practical Command Set (Windows PowerShell)

Baseline build:

dotnet build .\LeXtudio.Windows.csproj -f net9.0-desktop | Tee-Object -FilePath .\build-upstream-pass.log

Quick error summary:

Select-String -Path .\build-upstream-pass.log -Pattern "Build FAILED|Build succeeded|Warning\(s\)|Error\(s\)|error CS"

Working tree check:

git status --short

## Definition of Done for Each Porting Step

A candidate migration is done only when all are true:

1. Net9.0-desktop build succeeds with zero errors.
2. No new broad upstream divergence introduced.
3. Change is minimal and understandable.
4. Result is documented in this file with blocker notes if partial.

## Session Notes Update Pattern

When finishing each migration unit, append:

- Candidate name
- What changed (project include/remove and local shim deletion/addition)
- Build outcome
- Any blockers and why they are architectural vs temporary
- Recommended next candidate

## Session Log

### RangeContentEnumerator → upstream (done)

**Project change:**

- Removed `<Compile Remove>` for `ext/wpf/.../RangeContentEnumerator.cs` so the upstream source compiles.
- Deleted local stub `src/LeXtudio.Windows/System.Windows/Documents/RangeContentEnumerator.cs` (previously returned empty enumeration).

**Local bridge additions to satisfy upstream API surface (all minimal, no behavioral change vs. old empty enumerator):**

- `System.Windows.Documents.TextContainer.Generation` → public `uint`, returns `0` (mirrors the existing explicit `ITextContainer.Generation`). Upstream uses this for stale-tree guards; the initial `_generation` captured by the enumerator stays equal to the live value, so the guard never trips.
- `System.Windows.Documents.TextPointer(TextPointer)` copy constructor — forwards to the existing `(other, 0)` overload.
- `System.Windows.Documents.TextPointer.GetTextRunLength(LogicalDirection)` → returns the underlying `Run.Text` length when the pointer's owner is a `Run`, else `0`.
- `System.Windows.Documents.TextPointer.MoveToElementEdge(ElementEdge)` — no-op (`internal` because `ElementEdge` is internal).
- `System.Windows.Documents.TextPointer.MoveToPosition(ITextPointer)` — no-op.
- `System.Windows.FrameworkContentElement.IsLogicalChildrenIterationInProgress` → `false`.
- `System.Windows.WinUIFrameworkElementExtensions.IsLogicalChildrenIterationInProgress` (C# 14 extension property on `Microsoft.UI.Xaml.FrameworkElement`) → `false`. Only reachable through the dead branch behind the never-tripped generation check, but the symbol has to exist for the upstream file to compile.

**Build outcome:** `dotnet build LeXtudio.Windows.csproj -f net9.0-desktop` → **0 errors**, warnings only (baseline preserved).

**Blockers:** None.

**Behavioral parity vs. old shim:** The previous stub always returned an empty enumeration. The upstream class now walks via `TextPointer` movements, but those movements are no-ops in our shim (`MoveToNextContextPosition`, `MoveToElementEdge`, `MoveToPosition`, `CompareTo` against immobile pointers). End result: `MoveNext()` still returns `false` immediately — same observable behavior, but with the upstream surface in place for future enabling once `TextPointer` itself gains real movement.

**Recommended next candidate:** TextElementEditingBehaviorAttribute (next `Compile Remove` in the queue at csproj line ~600). Should be small — likely just a SR-key or accessibility issue.

### TextElementEditingBehaviorAttribute → upstream (done)

**Project change:** removed `<Compile Remove>`, deleted local file at `src/LeXtudio.Windows/System.Windows/Documents/TextElementEditingBehaviorAttribute.cs`.

**Bridge additions:** none — upstream class has no external dependencies.

**Notable divergence resolved:** local stub defaulted `IsMergeable = true, IsTypographicOnly = true`; upstream defaults to `false, false`. All callers (`Inline`, `InlineUIContainer`) set the named properties explicitly, so default values are unobservable. `Inherited` flag on AttributeUsage was `true` in local — same as upstream default.

**Build outcome:** 0 errors, baseline preserved.

**Recommended next candidate:** ElementEdge.

### ElementEdge → upstream (done)

**Project change:** removed `<Compile Remove>`, deleted local `ElementEdge.cs`.

**Bridge additions:** none.

**Notable divergence resolved:** local was `0,1,2,3` (sequential), upstream is `[Flags]` with `1,2,4,8` and `byte` storage. No code path uses integer arithmetic on values — only `is`/`==` against named members (verified via grep). Safe swap.

**Build outcome:** 0 errors.

**Recommended next candidate:** SelectionHighlightInfo (small, no fan-out).

### SelectionHighlightInfo → upstream (done)

**Project change:** removed `<Compile Remove>`. No local file to delete (none existed).

**Bridge additions:**

- `System.Windows.SystemColors.HighlightColor` → returns `Colors.Blue`. The other brush properties (`HighlightTextBrush`, `HighlightBrush`) already existed in the shim.

**Build outcome:** 0 errors. `Brush.Freeze()` already provided by `FreezableExtensions`.

**Recommended next candidate:** TextSegment (small struct, follow the same pattern).

### TextSegment → upstream (done)

**Project change:** removed `<Compile Remove>`. Removed local definition from `src/LeXtudio.Windows/System.Windows/Documents/EarlyBatchEditorShims.cs` (struct was inline there).

**Notable divergence resolved:**

- Local was `public struct`; upstream is `internal struct`. All callers in WindowsShims use `TextSegment` only from `internal` members, so accessibility reduction is safe.
- Upstream adds a 3-arg ctor (`bool preserveLogicalDirection`) and a `static readonly TextSegment Null` — neither was present locally but neither breaks existing callers (they all use the 2-arg form).
- Upstream depends on `ValidationHelper.VerifyPositionPair` (already migrated) and `ITextPointer.GetFrozenPointer` (already present on the shim).

**Build outcome:** 0 errors.

**Recommended next candidate:** TextContainerChangeEventArgs (136 lines, likely a small event args POCO).

## Session Summary (this pass)

Five migrations completed, all green on net9.0-desktop:

1. **RangeContentEnumerator** — biggest pass. Required minimal bridge additions on `TextContainer`, `TextPointer`, `FrameworkElement`, `FrameworkContentElement` (all returning safe-default values).
2. **TextElementEditingBehaviorAttribute** — zero-bridge swap; the only divergence was default values that no caller relied on.
3. **ElementEdge** — zero-bridge swap; verified no integer arithmetic on enum values.
4. **SelectionHighlightInfo** — one added member (`SystemColors.HighlightColor`).
5. **TextSegment** — zero-bridge swap after dropping a local public-vs-upstream-internal duplicate.

Patterns reinforced:
- The "compile frontier" loop in this plan works as designed; each candidate took 1–2 minimal local additions.
- When upstream uses an `internal` type and local provided a `public` duplicate, accessibility reduction has been safe so far (consumers in this repo only reach those types from `internal` code).
- Extension-member properties (C# 14) are essential for adding "WPF surface" to WinUI-aliased types like `FrameworkElement` — already established pattern in `WinUIFrameworkElementExtensions.cs`.

## Session 2 Log

### TextContainerChangeEventArgs + TextElementEnumerator + TextParentUndoUnit + ChangeBlockUndoRecord → upstream (done)

**Project change:** removed four `<Compile Remove>` entries; removed local stubs from `EarlyBatchEditorShims.cs`.

**Bridge additions:**
- `UndoManager.OpenedUnit` return type changed from `object` to `MS.Internal.Documents.IParentUndoUnit` (required by `ChangeBlockUndoRecord`).
- `UndoManager.Open(IParentUndoUnit)` and `Close(IParentUndoUnit, UndoCloseAction)` added as no-ops.
- `ITextPointer.Offset` property added to local interface; `TextPointer.Offset` returns `0`.

**Build outcome:** 0 errors.

### Typography + TypographyProperties → upstream (done)

**Project change:** removed `<Compile Remove>` for `Typography.cs`; added `<Compile Include>` for `MS\Internal\Text\TypographyProperties.cs`.

**Bridge additions:**
- Removed 7 local WPF typography enum definitions (`FontVariants`, `FontFraction`, `FontCapitals`, `FontNumeralStyle`, `FontNumeralAlignment`, `FontEastAsianWidths`, `FontEastAsianLanguage`) from `System.Windows.Media.TextFormatting.cs`.
- Added `global using` aliases to `GlobalUsings.cs` mapping them to `Microsoft.UI.Xaml` equivalents (same OpenType values).
- Added `protected void OnPropertiesChanged() {}` to local `TextRunTypographyProperties` base class (upstream `TypographyProperties` calls it but it lives in PresentationCore which we don't compile).

**Build outcome:** 0 errors.

### SplayTreeNode → upstream (done)

**Project change:** removed `<Compile Remove>`.

**Bridge additions:**
- Local `TextTreeTextElementNode` stub (in `EarlyBatchEditorShims.cs`) now inherits `SplayTreeNode` and implements all 10 abstract members with auto-properties (no-ops).

**Build outcome:** 0 errors.

### UIElementPropertyUndoUnit → upstream (done)

**Project change:** removed `<Compile Remove>`.

**Bridge additions (all in `EarlyBatchEditorShims.cs`):**
- `System.Windows.Expression` abstract class stub (used as `is Expression` type-check guard in undo logic).
- `System.Windows.Documents.TextTreeUndo.GetOrClearUndoManager(ITextContainer)` static shim returning `null` (UndoManager is always disabled so the undo path never runs).
- `SafeNativeMethods.GetStringTypeEx` signature stub.
- `UnsafeNativeMethods.FindNLSString` signature stub with `foundLength = 0` and return `-1`.

**Build outcome:** 0 errors.

### SelectionWordBreaker → upstream (done)

**Project change:** removed `<Compile Remove>`.

**Bridge additions (all new constants in `SafeNativeMethods` shim):**
- `C1_PUNCT`, `CT_CTYPE3`, `C3_KATAKANA`, `C3_HIRAGANA`, `C3_IDEOGRAPH`, `C3_HALFWIDTH`, `C3_FULLWIDTH`, `C3_DIACRITIC`, `C3_NONSPACING`, `C3_VOWELMARK`, `C3_KASHIDA` — Win32 character-type bit flags used for CJK/RTL word-break logic (all return false from stubs so word-break always falls through to default).

**Build outcome:** 0 errors.

## Session 2 Summary

Five new migrations completed, all green on net9.0-desktop:

1. **TextContainerChangeEventArgs + TextElementEnumerator + TextParentUndoUnit + ChangeBlockUndoRecord** — UndoManager API surface extension.
2. **Typography + TypographyProperties** — WinUI typography enum aliasing via `GlobalUsings.cs`.
3. **SplayTreeNode** — abstract base class; stub TextTreeTextElementNode updated to inherit.
4. **UIElementPropertyUndoUnit** — new `Expression`, `TextTreeUndo`, and P/Invoke stubs needed.
5. **SelectionWordBreaker** — Win32 character-type constants added to SafeNativeMethods shim.

Patterns reinforced:
- WinUI implicit global usings (from Uno SDK) can cause CS0104 ambiguous type references. Resolution: remove local enum definitions and add `global using` aliases to `GlobalUsings.cs` pointing to the WinUI equivalents.
- Abstract base classes from PresentationCore (e.g. `TextRunTypographyProperties`) need bridge methods like `OnPropertiesChanged()` added locally when upstream subclasses call them.
- The `TextTree*.cs` and `TextTreeUndo*.cs` wildcard `<Compile Remove>` patterns override explicit includes that appear earlier in the csproj. Files needing those base types require shims or re-ordering.
- The P/Invoke shim pattern (always return failure/zero) works well for Win32 NLS APIs — functionality degrades gracefully (no word-break, no find) since these are optional enhancements.

