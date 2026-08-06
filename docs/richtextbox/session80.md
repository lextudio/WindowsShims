### Session 80 - SpellCheck.IsEnabled Bridge + Squiggle Rendering

Status: completed.

Scope:

- WPF's `SpellCheck.IsEnabled` on a `RichTextBox` now flows end-to-end: the
  `SpellCheck` shim (`TextBoxBaseShims.cs`) notifies the `RichTextBox`, which
  stores the flag and pushes it to the `FlowDocumentView`, where misspelled
  words get red wavy squiggles drawn beneath them. The backend is Uno's
  `ISpellCheckingService` add-in (`Uno.WinUI.SpellChecking`, embedded Hunspell
  en_US dictionary) rather than the WPF `Speller*.cs` stack.

Bridge:

- `SpellCheck(object owner)` ctor now wires `_isEnabledChanged = enabled =>
  richTextBox.SetSpellCheckEnabledInternal(enabled)` when `owner is RichTextBox`;
  plain `TextBox` owners remain unwired.
- `RichTextBox.uno.cs` gained `_spellCheckEnabled`,
  `SetSpellCheckEnabledInternal(bool)`, and `PushSpellCheckToView()`, invoked
  from `OnApplyTemplate()` and `UpdateCaretFromSelection()`.
- `LeXtudio.Windows.csproj` adds `<UnoFeatures>SpellChecking</UnoFeatures>` for
  the `net10.0-desktop` target so the `Uno.WinUI.SpellChecking` package is
  referenced transitively.

Rendering:

- `FlowDocumentView.uno.cs` lazily resolves `ISpellCheckingService` via
  `Assembly.Load("Uno.WinUI.SpellChecking")` (forces the generated
  `ApiExtension` registration to run) then
  `ApiExtensibility.CreateInstance<ISpellCheckingService>()`.
- `ArrangeOverride` calls `RefreshSpellCheckSquiggles()` after
  `RefreshImeCompositionUnderline()`. Per `FlorenceLine`: a char-classification
  `GetWords` tokenizer feeds `service.SpellCheck(...)`, each correction range is
  mapped to pixels via `UnoFlowDocumentTextView.GetPixelXForOffset`, and a
  pooled red `Microsoft.UI.Xaml.Shapes.Polyline` (1px, sine 2.5px period,
  ±1.25px amplitude, at `baseline + 2`) renders the squiggle. Unused pool
  entries are collapsed.

Notes / decisions:

- Squiggles are XAML `Polyline` shapes (like the existing IME underline `Line`s),
  not raw Skia/CanvasDrawingContext.
- `CanvasDrawingContext.cs` / `FormattedText.cs` stay in
  `LeXtudio.Windows/System.Windows/Media/`. They are WPF-shaped adapter types,
  not Win2D API, so they must NOT move into `Uno2D/src/Win2D.UnoCompat`, which
  tracks strict parity against the real Win2D surface.

Tests:

- 4 new integration tests: disabled-by-default/no squiggles; misspelled words
  get >=2 squiggles with valid pixel ranges; correct words produce none;
  toggle-off clears squiggles.

Files modified:

- `src/LeXtudio.Windows/System.Windows/Controls/TextBoxBaseShims.cs`
- `src/LeXtudio.Windows/System.Windows/Controls/RichTextBox.uno.cs`
- `src/LeXtudio.Windows/MS.Internal.Documents/FlowDocumentView.uno.cs`
- `src/LeXtudio.Windows/LeXtudio.Windows.csproj`
- `tests/RichTextBox.IntegrationTestHost/MainPage.cs`
- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs`

Result:

- 192/192 RichTextBox integration tests pass (4 new, 0 failures).
- 233/233 model tests pass (`LeXtudio.Windows.Tests`).
- `SpellCheck.IsEnabled = true` renders squiggles under known misspellings
  ("teh", "foxx") and none under correct words ("the quick brown dog").
