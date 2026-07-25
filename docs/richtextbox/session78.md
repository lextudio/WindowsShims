### Session 78 - XAML Formatting Serialization

Status: completed.

Scope:

- Formatting properties (FontWeight, FontStyle, FontSize, Foreground, Background,
  TextDecorations) were not serialized to XAML, preventing formatted clipboard
  copy/paste and document persistence with formatting. Session 61 had enabled
  `DataFormats.Xaml` save/load but formatting properties were silently dropped.
- The root cause was a chain of three issues:
  1. `WinUIDependencyPropertyExtensions.Name` returned `property.ToString()`
     which for WinUI `DependencyProperty` gives the type name
     ("Microsoft.UI.Xaml.DependencyProperty"), not the registered property name
     ("FontWeight"). This was used by `TextRangeSerialization.GetPropertyNameForElement`
     to write XML attribute names, producing invalid XAML.
  2. WinRT-aliased structs (`FontWeight`, `FontStyle`, `FontStretch`) have no
     `TypeConverter` attribute, so `DPTypeDescriptorContext.GetStringValue`
     returned null and properties fell through to the complex-property path.
  3. The XAML reader (`XamlReader.ParseRun`) only handled `FontWeight="Bold"`
     (not numeric `"700"`), only `FontStyle="Italic"` (not `"Normal"` or
     `"Oblique"`), and skipped `TextDecorations` entirely.

Implementation:

- **WinUIDependencyPropertyExtensions.cs**: Added reflection-based property
  name extraction (`_name` field on `DependencyProperty`) and an `OwnerTypes`
  dictionary populated by shim `Register`/`RegisterAttached` overloads.
- **DPTypeDescriptorContext.cs** (`#if HAS_UNO`): Added special-case
  conversions for `TextElement.FontWeightProperty` → `FontWeightConverter`,
  `TextElement.FontStyleProperty` → `FontStyleConverter`,
  `TextElement.FontStretchProperty` → `.ToString()`,
  `TextElement.ForegroundProperty` → `FormatColor()` for `SolidColorBrush`,
  `TextElement.BackgroundProperty` → `FormatColor()` for `SolidColorBrush`.
- **XamlReader.cs**: Added `StripQualifier()` to handle both qualified
  (`TextElement.FontWeight`) and simple (`FontWeight`) attribute names.
  Extended `FontWeight` parsing to handle numeric values (e.g., `"700"`).
  Extended `FontStyle` parsing to handle all named values.
  Added `TextDecorations` parsing for `"Underline"` and `"Strikethrough"`.

Tests:

- `SaveLoad_Xaml_RoundTripsBoldFormatting` — FontWeight round-trips as "700".
- `SaveLoad_Xaml_RoundTripsItalicFormatting` — FontStyle round-trips as "Italic".
- `SaveLoad_Xaml_RoundTripsUnderlineFormatting` — TextDecorations preserved.
- `SaveLoad_Xaml_RoundTripsFontSizeFormatting` — FontSize round-trips as "24".
- `SaveLoad_Xaml_RoundTripsForegroundFormatting` — Foreground round-trips as "#FF90EE90".
- `SaveLoad_Xaml_RoundTripsMixedFormatting` — multiple properties round-trip together.

Files modified:

- `ext/.../DPTypeDescriptorContext.cs` — special-case conversions for
  FontWeight, FontStyle, FontStretch, Foreground, Background under `#if HAS_UNO`.
- `src/.../WinUIDependencyPropertyExtensions.cs` — `Name` via reflection,
  `OwnerType` via tracked dictionary.
- `src/.../XamlReader.cs` — `StripQualifier`, numeric FontWeight, named
  FontStyle, TextDecorations parsing.
- `tests/.../MainPage.cs` — removed temporary `get-xaml` probe.
- `tests/.../RichTextBoxIntegrationTests.cs` — 6 new formatted XAML round-trip tests.

Result:

- 187/187 RichTextBox integration tests pass (6 new, 0 failures).
- 181 pre-existing tests unchanged.
