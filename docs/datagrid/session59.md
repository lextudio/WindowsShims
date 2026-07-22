# DataGrid Port - Session 59

Date: 2026-06-13

## Goal

Continue reducing local shims: replace the hand-written local `DataGridTextColumn`
(47 lines) with the **linked upstream `DataGridTextColumn.cs`** (≈420 lines of real
WPF: Font*/Foreground DPs, default styles, IME/caret editing, RefreshCellContent
font sync), and build the shared input substrate that lets the other concrete
columns be linked in later sessions.

## Approach: resolve the gaps, don't shrink the goal

Linking the upstream file surfaced four compile gaps. Each was resolved at the
right layer (verified by re-running the build after each), not by reverting:

1. **`MouseButtonEventArgs` not convertible from `RoutedEventArgs`** — the shim
   `MouseEventArgs` derived from `EventArgs`. Reparented it onto the new
   `InputEventArgs` (faithful to WPF: `MouseEventArgs : InputEventArgs :
   RoutedEventArgs`), dropping the now-inherited `Handled`.
2. **`TextBox.GetCharacterIndexFromPoint` missing** — WinUI's TextBox has no such
   method; the column uses it only to place the caret under the mouse. Added an
   extension returning `-1` (no hit → falls back to `SelectAll`, the WPF behavior
   when the click isn't over text).
3. **`Dispatcher.Invoke(Action, DispatcherPriority)` missing** — the IME branch of
   `OnInput` drains the dispatcher queue. The generated WinUI `CoreDispatcher` has
   no such overload (and the generator already owns the `Dispatcher` member, so a
   shadow property collides — CS0102). Added an `Invoke` extension on
   `CoreDispatcher` that runs the callback synchronously (the IME drain is moot in
   the shim).
4. **`FrameworkPropertyMetadata` ctor ambiguity on the 5 Font DPs** — the WPF and
   WinUI `(object, options, callback)` ctors are both candidates because the WPF
   `DependencyPropertyChangedEventArgs` has an implicit operator from the WinUI
   args, making the method group applicable to both delegates. The WinUI overload
   is intentionally kept (AvalonEdit needs it), so the fix is at the call sites:
   fully-qualified `(System.Windows.PropertyChangedCallback)` casts on the 5
   `AddOwner` metadata args (valid in upstream WPF too — harmless to the fork).

## Shared substrate added (unlocks Text + future CheckBox/Combo)

- New `System.Windows.Input.InputEventArgs : RoutedEventArgs`; reparented
  `KeyEventArgs`, `TextCompositionEventArgs`, and `MouseEventArgs` onto it so the
  column `OnInput(InputEventArgs)` downcasts compile.
- `DataGridColumn` base: `internal virtual void OnInput(InputEventArgs)` +
  `internal void BeginEdit(InputEventArgs, bool)` (mirrors upstream base).
- `DataGridHelper`: real `SyncColumnProperty`, `HasNonEscapeCharacters`,
  `IsImeProcessed`; no-op `Cache/RestoreFlowDirection` (RTL plumbing not needed).

## What Changed

- **Linked** `DataGridTextColumn.cs`; **deleted** the local shim entirely (no
  Uno-specific members remained — pure reduction).
- Net: the real WPF text-column body (Font DPs, styles, edit/caret/IME handling,
  font RefreshCellContent) now runs instead of a parallel reimplementation.

## Verification (confirmed, not assumed)

```
dotnet build  → 0 errors
dotnet run … --probe  → DONE failures=0  (33 steps)
dotnet test  → 125 passed, 0 failed  (+1 TextColumnBodyIsReusedFromUpstream)
```

Probe steps exercising the reused body: text cells render
(`first visible artifact`, `rows are DataGridRow…`), edit + write back
(`cell editing`), read-only coercion + cancelable events (`editing:`), and sort
(`header click sorts`, still deriving `SortMemberPath` via the session-58 bridge).

## Next Batch

1. **`DataGridCheckBoxColumn`** — the substrate (InputEventArgs/OnInput) is now in
   place and its only DP uses the simple metadata ctor. **Risk to verify first:**
   the local shim does *manual* write-back (Checked/Unchecked handlers) while
   upstream relies on `ApplyBinding(checkBox, IsCheckedProperty)` TwoWay write-back
   — confirm WinUI honors TwoWay to a POCO before deleting the manual path.
2. Then `DataGridComboBoxColumn` (largest local shim, 324 lines).
3. Longer-horizon: the Selector selection-engine reuse behind command routing.
