### Session 4 - App-Model Loading (R5)

Status: completed.

Scope:

- R5: the WPF application-model layer the fixed-document sources reach for when
  they load a page or resolve a relative uri — `BindUriHelper`, `BaseUriHelper`,
  `MimeTypeMapper`, `WpfWebRequestHelper`, `XamlReaderHelper`.

R5 is independent of the GlyphRun question, which is why it was taken before R6.

Findings:

- Measured surface: **227 errors before this session, 195 after** (164 excluding
  `RubberbandSelector.cs`, which stays gated until R8).

- **`MS/Internal/Utility/BindUriHelper.cs` is now linked and compiles in the
  normal build**, not just the un-gated one. It is the first fixed-document
  support file to come off the gate list. Three things were needed:
  - Its own guard widened to `#if PRESENTATIONFRAMEWORK || HAS_UNO` (session 1).
  - Its other half, `Shared/MS/Utility/BindUriHelper.cs`, linked as well. That
    file selects its namespace from the consuming assembly and `#error`s on an
    unknown one, so a `HAS_UNO` arm was added, and the
    `PRESENTATION_CORE || PRESENTATIONFRAMEWORK` feature gate around
    `GetResolvedUri`/`BaseUri`/`DoSchemeAndHostMatch` widened to include it.
  - The `INavigator` branch guarded: it is the Frame/NavigationWindow contract
    from the app-model layer. The `NavigationService` path immediately below it
    already covers everything reachable here.

- `BaseUriHelper`, `MimeTypeMapper` and `WpfWebRequestHelper` were each tried as
  links and rejected. Between them they pull in `PackWebRequestFactory`,
  `SiteOfOriginContainer`, `CookieHandler`, `CustomCredentialPolicy`,
  `SafeSecurityHelper` and `MS.Win32.Compile` — the pack:// URI and deployment
  stack, most of it Windows-specific. The actual call surface across the whole
  family is eleven members, so they became local files carrying upstream's
  constant values and tree-walk semantics, with the rejection reason in the
  header.

- `MimeTypeMapper` additionally `#error`s unless `PRESENTATION_CORE` is defined;
  that selector got a `HAS_UNO` arm before the link attempt, which is what let
  the real dependency problem surface.

- The old local `BaseUriHelper` stub lived in `System.Windows.Documents`
  (`HyperlinkSupport.cs`), which is the enclosing namespace of the upstream
  files, so it won name lookup over anything imported from
  `System.Windows.Navigation`. It was deleted; the replacement is in the right
  namespace.

- BAML is the WPF build task's compiled-XAML format and has no reader here. The
  two `XamlReader.LoadBaml` branches are guarded, so a BAML part now falls
  through to the existing unsupported-mime-type throw rather than silently
  half-working.

Changes:

- New `System.Windows/Navigation/AppModelShims.cs`:
  - `BaseUriHelper` — `BaseUriProperty`, `GetBaseUri`/`SetBaseUri`,
    `PackAppBaseUri`, `SiteOfOriginBaseUri`, `BaseUri`, `FixFileUri`.
    `GetBaseUriCore` walks up for the nearest `BaseUri` value or `IUriContext`,
    as upstream does; WPF's UIElement/ContentElement/Visual parent chain
    collapses to `FrameworkContentElement.Parent` plus the visual parent.
  - `MimeTypeMapper` — the five media types, upstream's exact strings.
  - `WpfWebRequestHelper.CreateRequestAndGetResponseStream` — pack:// served from
    the in-process `PackageStore`, everything else through `WebRequest`. No
    cookie or credential policy, unlike upstream.
  - `XamlReaderHelper.DefinitionMetroNamespaceURI` — the one constant the schema
    check reads, out of a 6800-line upstream parser.
- `System.Windows/Media/Imaging/ImagingShims.cs`: `PackageStore.GetPackage`.
- `System.Windows/Documents/HyperlinkSupport.cs`: local `BaseUriHelper` deleted.
- `LeXtudio.Windows.csproj`: links `Shared/MS/Utility/BindUriHelper.cs`;
  `MS/Internal/Utility/BindUriHelper.cs` removed from the gate list.
- `ext/wpf`: `HAS_UNO` arms in `Shared/MS/Utility/BindUriHelper.cs` and
  `Shared/MS/Internal/MimeTypeMapper.cs`; guards on the `INavigator` branch and
  the two BAML branches.

Tests:

- New `src/LeXtudio.Windows.Tests/AppModelShimTests.cs` (18 tests) exercising the
  linked `BindUriHelper` directly: absolute/relative/null uri resolution,
  `GetFragment` on absolute and relative uris, `StartWithFragment`, and
  `IsXamlMimeType` across the four XPS markup types plus BAML and an image type.
  Also the `MimeTypeMapper` constants, the pack base-uri authorities,
  `FixFileUri` both ways, and the Metro definition namespace.

Result:

- `LeXtudio.Windows` builds clean on `net10.0-desktop`.
- 286/286 model tests, 238/238 RichTextBox integration, 83/83 DataGrid integration.
- Un-gated surface: 195 errors, 164 excluding `RubberbandSelector.cs`.

Of those 164, about 100 are `GlyphRun`, `GlyphTypeface` and `FormattedText`
members. The non-text remainder is small: `PathGeometry.CreateFromGeometry`/
`AddGeometry`, `IFrameworkInputElement`, `DocumentGrid`, `IScrollInfo` in
`TextViewBase`, and a handful of one-offs.

Next: R7. The GlyphRun decision is now the only thing standing between the port
and the object model.
