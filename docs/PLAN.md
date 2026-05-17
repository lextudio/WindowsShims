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

