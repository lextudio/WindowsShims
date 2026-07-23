### Session 7 - RichTextBox OnKeyDown Path

Status: complete.

Scope:

- Determine whether the desktop DevFlow host can construct Uno
  `KeyRoutedEventArgs` and call `RichTextBox.OnKeyDown(...)`.
- Add the first integration test that enters through the RichTextBox key path
  rather than directly invoking a WPF command handler.

Implementation notes:

- `Microsoft.UI.Xaml.Input.KeyRoutedEventArgs` has an internal constructor in
  the Uno desktop runtime. The test host creates it by reflection with:
  original source, `VirtualKey`, `VirtualKeyModifiers.None`, no physical key
  status, and no Unicode key.
- `RichTextBox.OnKeyDown(...)` is protected and overloaded/inherited, so the
  probe resolves the exact Uno `KeyRoutedEventArgs` signature by reflection.

DevFlow additions:

- `richtextbox.probe.key-down`

Verified behavior:

- After text is inserted through `richtextbox.probe.text-input-event`, invoking
  `richtextbox.probe.key-down` with `Back` reaches
  `RichTextBox.OnKeyDown(...)`, maps to the WPF Backspace editing command, and
  removes the previous character.

Command:

```text
dotnet test tests/RichTextBox.IntegrationTests/RichTextBox.IntegrationTests.csproj
```

Result:

```text
Passed: 10/10
```

Next session:

- Add key-path coverage for `Delete`, using selection or caret positioning to
  make the observable mutation deterministic.
- Then add Return/paragraph insertion coverage through the same
  `richtextbox.probe.key-down` action.
