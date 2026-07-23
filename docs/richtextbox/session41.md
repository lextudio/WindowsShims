### Session 41 - Hyperlink Hit-Test and Activation Coverage (M2)

Status: complete.

Scope:

- M2's remaining candidate: "Hyperlink hit-test and activation path still
  work." Cover `MS.Internal.Documents.FlowDocumentView.GetHyperlinkAt(...)`
  and `.ActivateHyperlink(...)` — the two methods `RichTextBox.uno.cs`'s
  `OnPointerPressed`/`OnPointerReleased` call for real pointer-driven
  hyperlink interaction.

Approach:

- Did not synthesize real `PointerRoutedEventArgs`/`PointerPoint` to drive
  `OnPointerPressed`/`OnPointerReleased` end-to-end (much heavier than the
  `KeyRoutedEventArgs`/`CharacterReceivedRoutedEventArgs` reflection used in
  earlier sessions, and `GetHyperlinkAt`/`ActivateHyperlink` are themselves
  the actual hit-test/activation logic, not a thin wrapper). Instead call
  those two methods directly via reflection, using a real point computed from
  the actual rendered layout rather than a guessed pixel coordinate.
- To get a real point: `RichTextBoxScenarios.BuildHyperlinkDocument(before,
  link, after)` builds a `Paragraph` with three `Inline`s (`Run`, `Hyperlink`,
  `Run`). After layout, `FlowDocumentView.Page.Lines[].Runs[]` (the shim's
  own `FlorenceLine`/`FlorenceRun` layout model) already carries each run's
  `X`/`Width` and each line's `Y`/`Height`, plus a `Hyperlink` back-reference
  on the run — exactly the data needed to compute both an in-hyperlink point
  and a definitely-outside point, with no coordinate guessing.

DevFlow additions:

- `richtextbox.probe.set-hyperlink-document(before, link, after)` — loads a
  `before / Hyperlink(link) / after` paragraph.
- `richtextbox.probe.get-hyperlink-rect` — reflects into
  `FlowDocumentView.Page.Lines[].Runs[]` to find the run whose `Hyperlink` is
  non-null, returning its `{x, y, width, height}`.
- `richtextbox.probe.hyperlink-hit-test(x, y)` — calls
  `FlowDocumentView.GetHyperlinkAt(point)` and reports whether a `Hyperlink`
  was found, plus its text content.
- `richtextbox.probe.activate-hyperlink-at(x, y)` — hit-tests, and if found,
  attaches a temporary `Click` handler, calls
  `FlowDocumentView.ActivateHyperlink(hyperlink)`, and reports whether the
  handler fired (`hyperlink.RaiseClick()` → `OnClick()` → the public `Click`
  routed event, all synchronous within the one probe call).
- `RequireRenderScope` test-host helper, consolidating the existing
  `RenderScope` reflection pattern used elsewhere in `Snapshot`.

Tests added:

- `HyperlinkHitTest_AtHyperlinkCenter_FindsTheHyperlink` — hit-testing at the
  computed center of the hyperlink run's rect returns that `Hyperlink`, with
  `linkText == "link"`.
- `HyperlinkHitTest_OutsideHyperlinkRun_FindsNoHyperlink` — hit-testing a
  point just before the hyperlink run (inside the plain `"before "` run)
  returns no hyperlink.
- `ActivateHyperlinkAt_HyperlinkCenter_RaisesClick` — activating the
  hit-tested hyperlink raises its `Click` event.

No product code changes were needed — `GetHyperlinkAt`/`ActivateHyperlink`
already worked correctly; this was purely new test coverage for
previously-untested, already-working code.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 90/90
```

Next session:

- M2's remaining candidates: caret hit-testing (`GetTextPositionFromPoint`/
  `TextPointer.GetCharacterRect` round-trip) not throwing, and "selection
  invalidates render layout" — both could reuse the same
  rendered-layout-introspection approach established this session (real
  computed points instead of guessed pixel coordinates).
