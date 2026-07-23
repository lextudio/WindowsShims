### Session 34 - Clipboard/Serialization Format Coverage (M4)

Status: complete.

Scope:

- Begin M4 (Clipboard and Serialization): validate `TextRange.Save`/`Load`
  against `DataFormats.Text`, and document which formats are intentionally
  partial on Uno per the milestone's "Done when" criteria.

Finding:

- `TextRangeBase.CanSave`/`CanLoad` (in the linked upstream
  `TextRangeBase.cs`) have an explicit `#if HAS_UNO` branch:
  `return dataFormat == DataFormats.Text;` — XAML/RTF/XamlPackage are
  deliberately not wired up in this shim slice ("XAML serialization isn't
  enabled in the shim slice"). This is an intentional, already-documented
  scope limitation, not a bug.
- `TextRangeBase.Save`/`Load` handle `DataFormats.Text` under `HAS_UNO`, but
  fall through to the same `else` branch as any truly-unrecognized format for
  Xaml/Rtf/XamlPackage, throwing
  `ArgumentException(SR.TextRange_UnsupportedDataFormat, ...)` — i.e. the
  failure mode for unsupported formats is already predictable, matching M4's
  "Done when" bar ("Unsupported formats fail predictably or are explicitly
  documented").

DevFlow additions:

- `richtextbox.probe.can-save-load-format(format)` — reports
  `TextRange.CanSave`/`CanLoad` for an arbitrary `DataFormats` value against
  the current document's full range.
- `richtextbox.probe.save-load-format-roundtrip(format)` — saves the current
  document range to a `MemoryStream` in the given format, then loads it back
  into a fresh `FlowDocument` and swaps it into the `RichTextBox`. Used both to
  prove the round-trip for `Text` and to observe the exception for
  unsupported formats.

Tests added:

- `CanSaveLoad_Text_IsSupported`
- `CanSaveLoad_NonTextFormats_AreUnsupportedUnderUno` (`Xaml`, `Rtf`,
  `XamlPackage`)
- `SaveLoad_PlainText_RoundTripsThroughAFreshFlowDocument`
- `SaveLoad_NonTextFormats_FailPredictablyUnderUno` (`Xaml`, `Rtf`,
  `XamlPackage`)

Verified behavior:

- `CanSave`/`CanLoad` report `true` for `DataFormats.Text` and `false` for
  `Xaml`/`Rtf`/`XamlPackage`.
- `Save`/`Load` round-trip plain text ("hello world") through a brand new
  `FlowDocument` via `DataFormats.Text`.
- `Save`/`Load` throw `ArgumentException` for `Xaml`/`Rtf`/`XamlPackage`
  instead of silently corrupting the stream or the document.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj --logger "console;verbosity=minimal"
```

Result:

```text
Passed: 76/76
```

Next session:

- M4 candidate coverage remaining: "Image/package payload limitations" — not
  applicable while `XamlPackage` stays unsupported under `HAS_UNO`; revisit
  only if a consumer requests image/package clipboard support.
- Otherwise M4's "Done when" bar looks met: supported format (`Text`) is
  tested, and unsupported formats (`Xaml`/`Rtf`/`XamlPackage`) are both tested
  and already documented as intentional via the `HAS_UNO` branch in
  `TextRangeBase.cs`.
- Good next candidates: revisit the deeper `TextElement.Parent`/`Run`
  repositioning gap noted in Session 33 (the root cause behind both the
  paragraph-merge fast path and the Enter fast path in `RichTextBox.uno.cs`),
  or continue M3's remaining item — selection replacement via the real key
  path (typing over a selection through `OnKeyDown`/character input rather
  than `OnTextInput` directly).
