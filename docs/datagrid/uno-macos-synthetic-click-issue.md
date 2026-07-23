# Uno Platform: synthetic (CGEventPost-injected) mouse clicks don't set `IsLeftButtonPressed` on macOS 15+

## Summary

On macOS 15+ (confirmed on macOS 27 / build 26A5378n), a `PointerPressed`/`PointerReleased`
pair correctly reaches the right WinUI element at the right coordinates when the click is
synthesized via `CGEventPost` (e.g. via `cliclick`, or any other CGEvent-based automation
tool), but the delivered `PointerPoint.Properties.IsLeftButtonPressed` reads `false` during
the press — as if no button were down at all. This breaks any control whose behavior depends
on that flag being `true` during a press, including native `ButtonBase`-derived controls'
`Click` event, which never fires for a synthetic click even though both
`PointerPressed`/`PointerReleased` visibly fire on the correct element.

Real (hardware) mouse/trackpad clicks are unaffected — this only reproduces with
CGEventPost-synthesized input, which is the standard mechanism used by essentially all
macOS UI-automation/testing tools (cliclick, Playwright-style native input helpers, etc.),
so this blocks automated interactive testing of any WinUI `Button`/`ButtonBase`-derived
control on Uno's Skia/macOS target.

## Where this was found

Investigating why `DataGridColumnHeader` (a custom `ButtonBase`-derived control) never
fired `OnClick` in response to synthetic clicks dispatched by a DevFlow test agent
(itself using a `cliclick`-equivalent CGEventPost implementation). Added temporary
diagnostics to the WPF-shim's `ButtonBase.OnPointerPressed`/`OnPointerReleased`:

```csharp
protected override void OnPointerPressed(PointerRoutedEventArgs e)
{
    base.OnPointerPressed(e);
    var pt = e.GetCurrentPoint(this);
    if (pt.Properties.IsLeftButtonPressed)   // <-- reads false for synthetic clicks
    {
        _isPressed = true;
        ...
    }
}
```

Findings, in order:

1. On a **fresh app launch**, the *first* click of any kind produced no
   `OnPointerPressed` at all — this turned out to be ordinary macOS window-activation
   behavior (the first click on an inactive window only focuses it) and is **not** part
   of this bug; doing any other click first (e.g. clicking a data row) before retrying
   made the header receive `OnPointerPressed` correctly.
2. With that ruled out, **`OnPointerPressed` and `OnPointerReleased` both fired
   correctly** on the header, at the right coordinates.
3. Yet `OnClick` (real WinUI `ButtonBase`'s native click-detection) never fired.
   Inspecting `_isPressed` at the top of `OnPointerReleased` showed it was `false` —
   meaning `pt.Properties.IsLeftButtonPressed` had already read `false` inside the
   preceding `OnPointerPressed` too.
4. The same non-response was reproduced on a **real, unmodified**
   `Microsoft.UI.Xaml.Controls.Button` (the DataGrid header's own filter-icon button) at
   the same coordinates — ruling out anything specific to the custom `ButtonBase` shim
   or `DataGridColumnHeader`.

## Root cause (traced to source)

`src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOWindow.m` branches on OS
version when computing `data.mouseButtons` for the native→managed event bridge:

```objc
// FIXME: NSEvent.pressedMouseButtons is returning a wrong value in Sequoia when using an external trackpad
if (_osVersion.majorVersion >= 15) {
    NSInteger mask = [MouseButtons mask];
    // ... (recovery logic if AppKit and our tracked mask disagree)
    data.mouseButtons = (uint32)mask;
} else {
    data.mouseButtons = (uint32)NSEvent.pressedMouseButtons;
}
```

`MouseButtons.m` (added by a recent, unrelated fix for **external hardware trackpads** on
Sequoia — commit `00febe23a5 fix(macOS): external trackpad events`) tracks button-down
depth per NSEvent type/button-number in a static counter array and derives a bitmask from
it, falling back to `NSEvent.pressedMouseButtons` and then a `CGEventSourceButtonState`
poll if the tracked state and AppKit disagree.

On the C# side, `Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs` turns that
mask into `PointerPointProperties`:

```csharp
IsLeftButtonPressed = (data.MouseButtons & NSEventTypeLeftMouseDown) == NSEventTypeLeftMouseDown,
```

This machine is macOS 27 (`_osVersion.majorVersion >= 15` is true), so it takes the
"tracked mask" branch, not the plain `NSEvent.pressedMouseButtons` branch. The tracked-mask
logic was written and tested against **real hardware** trackpad/mouse events; it has not
been verified against **CGEventPost-synthesized** events, and empirically the mask it
produces for a synthetic left-mouse-down does not have bit 0 set — `IsLeftButtonPressed`
ends up `false`.

**Not yet isolated further** (would require adding `NSLog` diagnostics to
`MouseButtons.m`/`UNOWindow.m` and rebuilding the native `UnoNativeMac` library via Xcode,
which needs its own dedicated pass): whether the synthetic event's `NSEvent.type` doesn't
match what `[MouseButtons track:]`'s switch expects, whether the depth-counter state gets
reset/corrupted by an interleaved event before `mask` is read, or whether
`CGEventSourceStateCombinedSessionState` (used in the Quartz fallback) simply doesn't see
HID-tap-injected button state the same way a real hardware event registers it.

## Suggested next steps for whoever picks this up

1. Reproduce directly against `Uno.UI.Runtime.Skia.MacOS` (no WindowsShims/Roma
   involvement needed) — a minimal Uno Skia-desktop app with a single `Button`, clicked
   via `cliclick c:x,y` (or raw `CGEventPost`), on macOS 15+.
2. Add temporary `NSLog` around `[MouseButtons track:]`/`mask`/`buttonMask:` in
   `MouseButtons.m` and in the `data.mouseButtons = (uint32)mask;` assignment in
   `UNOWindow.m`, to see the actual `NSEvent.type`/`buttonNumber` the synthetic event
   carries and what mask gets computed.
3. Compare against a **real** hardware click's log output to find exactly where the two
   diverge.
4. Consider whether the fix should special-case CGEventPost-tap-injected events (they may
   need to be recognized and handled via the `NSEvent.pressedMouseButtons`/Quartz-poll path
   directly, bypassing the depth-counter tracking meant for real hardware's
   sometimes-missing separate button events).

## Environment

- macOS 27 (build 26A5378n)
- `unoplatform/uno` repo at commit `1384233a04` (HEAD at time of writing), the tracked-mask
  logic landed in `00febe23a5`/`5825a18a23` ("fix(macOS): external trackpad …")
- Reproduced via a DevFlow test agent's `cliclick`-backed native-input implementation
  (`wpf-labs/external/cliclick-sharp`, itself a straightforward `CGEventCreateMouseEvent` +
  `CGEventPost(kCGHIDEventTap, ...)` — nothing unusual there), but the bug is in Uno's
  event-bridge code, not in how the click was synthesized.
