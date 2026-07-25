### Session 62 - Formatted Clipboard Round-Trip

Status: completed.

Scope:

- Session 56 verified multi-paragraph plain-text paste. Session 61 adds XAML
  serialization. This session combines them: copy formatted content (bold,
  italic, underline, font size, foreground color) from one RichTextBox, paste
  into another (or the same document), and verify formatting is preserved.
- The clipboard command path (`ApplicationCommands.Copy`/`Cut`/`Paste`)
  automatically selects the best format. Verify that `DataFormats.Xaml` is
  preferred over `DataFormats.Text` when available, and that formatted
  content survives the round-trip.
- Also test cross-application pasting: set clipboard to known XAML content
  and verify the RichTextBox's Paste handler correctly deserializes it.

Implementation:

- Depends on session 61's XAML serialization working for basic cases.
- If the WPF clipboard format negotiation works automatically, this session
  is primarily test coverage.
- If not, add a clipboard-format preference hint in the paste path to
  prioritize XAML over Text.

Tests:

- `CopyPaste_BoldText_RoundTripsFormatting`: type bold text, copy, paste,
  verify bold weight preserved.
- `CopyPaste_MixedFormatting_RoundTripsFidelity`: document with multiple
  formatting changes (bold, italic, different font sizes, colors), copy
  selection, paste, verify inline tree matches.
- `CopyPaste_Hyperlink_RoundTripsLink`: copy/paste a hyperlink, verify
  NavigateUri preserved.
- `CutPaste_RemovesFromSourceAndInsertsAtTarget`: cut formatted text,
  verify source loses it, target gains it with formatting.

Files modified:

- `tests/RichTextBox.IntegrationTests/RichTextBoxIntegrationTests.cs` — new
  tests.
- Possibly `MainPage.cs` — add clipboard-format inspection probe.

Regression sweep:

```text
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj -f net10.0-desktop
dotnet test tests/RichTextBox.IntegrationTests/
```

Next session:

- Table arrow-key navigation.
