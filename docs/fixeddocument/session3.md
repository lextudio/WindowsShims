### Session 3 - Element-Level Gaps and Mechanical Leftovers (R4)

Status: completed.

Scope:

- R4: the WPF element surface the fixed-document sources read, plus the two
  mechanical buckets folded in from session 2's plan — the `Image`/`Path`
  using-aliases and the missing `SR` constants.

No fixed-document file is un-gated yet; this is still compile surface.

Findings:

- Measured surface: **389 errors before this session, 227 after.** Excluding
  `RubberbandSelector.cs` and `BindUriHelper.cs`, which stay gated regardless
  (R5 and R8), the object-model-relevant count is **183**.

- All 50 missing `SR` constants exist in
  `PresentationFramework/Resources/Strings.resx`, so they were generated with
  their real English message text instead of the key-name placeholder the older
  entries use. Exceptions thrown from linked fixed-document code now say what
  went wrong.

- `AutomationProperties` was not actually missing from WinUI. The local
  `System.Windows.Automation.AutomationProperties` shadows
  `Microsoft.UI.Xaml.Automation.AutomationProperties` for every upstream file
  with `using System.Windows.Automation;`, so `Name`/`HelpText` had to be
  re-exposed on the shim, forwarding to the WinUI attached properties.

- Extension members cannot be called without a receiver. WPF code calls
  `VerifyAccess()`, `GetDpi()` and `InheritanceBehavior` bare on `this`, so four
  call sites needed `this.` added behind `#if HAS_UNO`. `VerifyAccess` already
  existed on `WinUIDependencyObjectExtensions`; adding a second one produced
  CS0121 ambiguity rather than fixing anything.

- `Windows.Foundation.Rect` has `Contains(Point)` but not `Contains(Rect)`,
  which the SOM table-nesting code relies on.

- WinUI types `FrameworkElement.Language` as a string where WPF types it as
  `XmlLanguage`, and upstream passes the property straight through. An implicit
  string conversion on the `XmlLanguage` shim covers it.

Changes:

- `ext/wpf`: `Image`/`Path` using-aliases added to 11 files behind `#if HAS_UNO`;
  four `this.`-qualified call sites (`DocumentReference.VerifyAccess` twice,
  `PageContent._Init`'s `InheritanceBehavior`, `Glyphs`/`FixedPage`'s `GetDpi`).
- `SR.cs`: 50 constants carrying the real resx message text.
- `System.Windows/Automation/AutomationProperties.cs`: `NameProperty`,
  `HelpTextProperty`, `GetName`/`SetName`, `GetHelpText`/`SetHelpText`.
- `System.Windows/WinUIFrameworkElementExtensions.cs`:
  - `UIElement.BringIntoView()` and `BringIntoView(Rect)` over WinUI's
    `StartBringIntoView`.
  - `UIElement.GetDpi()` reading `XamlRoot.RasterizationScale`, 1.0 before the
    element is in a tree.
  - `FrameworkElement.IsInitialized` mapped to WinUI's `IsLoaded`.
  - `FrameworkElement.InheritanceBehavior` plus the `InheritanceBehavior` enum,
    stored on an attached property. Recorded but inert: WinUI's property system
    has no inheritance boundary to hook, and the doc comment says so.
- `System.Windows/Media/RectAndTransformExtensions.cs`: `Rect.Contains(Rect)`.
- `System.Windows/TextEditorSystemShims.cs`: implicit `string` to `XmlLanguage`.

Tests:

- `RectAndTransformExtensionsTests`: `Contains` for fully-inside, identical and
  overhanging rects, plus the empty-rect cases.
- New `FixedDocumentElementShimTests`: the `XmlLanguage` implicit conversion and
  its invariant fallback. The fallback test first used `"not-a-tag"`, which ICU
  happily resolves to a culture named `not`; it now uses characters no tag may
  contain.

Result:

- `LeXtudio.Windows` builds clean on `net10.0-desktop`.
- 268/268 model tests, 238/238 RichTextBox integration, 83/83 DataGrid integration.
- Un-gated surface: 227 errors (183 excluding the two permanently-gated files),
  down from 585 at the start of session 2.

What is left is now almost entirely one problem. Of the 183 object-model errors,
about 90 are `GlyphRun`, `GlyphTypeface` and `FormattedText` members; the rest is
app-model loading (`MimeTypeMapper`, `WpfWebRequestHelper`,
`BaseUriHelper.GetBaseUri`, ~15), `PathGeometry.CreateFromGeometry`/`AddGeometry`,
`IFrameworkInputElement`, and a handful of one-offs.

Next: R7's GlyphRun decision is now the gate on everything else. R5 (app-model
loading) can proceed in parallel and is independent of it.
