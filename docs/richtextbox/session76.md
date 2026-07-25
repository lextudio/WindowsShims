### Session 76 - Control-Level DP Inheritance (Foreground, Background, FontFamily, FontSize, FontWeight, FontStyle)

Status: completed.

Scope:

- WPF's `RichTextBox` inherits its `Foreground`, `Background`, `FontFamily`,
  `FontSize`, `FontWeight`, and `FontStyle` to the `FlowDocument` content. Setting
  `RichTextBox.Foreground = Brushes.Red` should make all text red.
- Currently these DPs are not forwarded to the document. The default text
  color is black regardless of what the RichTextBox has set.
- The WPF property-inheritance system (`FrameworkPropertyMetadataOptions.Inherits`)
  should handle this automatically through the visual tree, but under
  HAS_UNO the inheritance may not work because the RichTextBox and the
  FlowDocument are in separate Uno visual-tree branches.

Implementation:

- Added `InheritedForeground`, `InheritedBackground`, `InheritedFontFamily`,
  `InheritedFontSize`, `InheritedFontWeight`, `InheritedFontStyle` properties
  to `FlowDocumentView.uno.cs` (lines 63-68).
- Wired them from `RichTextBox.uno.cs` in `UpdateCaretFromSelection`
  (lines 495-500): set from `RichTextBox`'s own `Foreground`, `Background`,
  `FontFamily`, `FontSize`, `FontWeight`, `FontStyle` properties.
- Used the inherited values as fallback defaults in `ApplyRunFormatting`
  (lines 630-641): if a `FlorenceRun` doesn't specify a value, fall back
  to the corresponding `Inherited*` property.
- Build fix: `Microsoft.UI.Text.FontWeight` does not exist on `net10.0-desktop`.
  Changed to `FontWeight` (global using alias to `Windows.UI.Text.FontWeight`)
  and `global::System.Windows.FontWeights` for static accessors (Bold/Normal).

Tests:

- No new tests added; existing formatting tests (`ApplyFontWeightCommand`,
  `ApplyFontSizeCommand`, `ApplyFontFamilyCommand`, `ApplyForegroundCommand`,
  `ApplyBackgroundCommand`) exercise the plumbing indirectly.

Files modified:

- `FlowDocumentView.uno.cs` — added `Inherited*` properties, used in
  `ApplyRunFormatting`.
- `RichTextBox.uno.cs` — forward control-level DPs in `UpdateCaretFromSelection`.
