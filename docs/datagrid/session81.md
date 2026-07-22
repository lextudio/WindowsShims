# DataGrid Port - Session 81

Date: 2026-06-13

## Goal

Fix the 3 build errors left at the end of session 80, then extend the geometry type
bridge to remove the `#if !HAS_UNO` guard from the Frozen Columns region of
`DataGridCell.upstream.cs` — replacing guards with real WPF-source linking and
type mapping wherever possible.

## Errors Fixed from Session 80

| Error | Root cause | Fix |
|---|---|---|
| `CS0102` `FocusedCell` already defined | Added to local DataGrid.cs but upstream DataGrid.cs already defines it at line 3462 (not guarded) | Removed from local shim |
| `CS0111` `IsCurrent` already defined | Same — upstream DataGrid.cs already has it at line 3128 | Removed from local shim |
| `CS0234` `System.Windows.Media.Geometry` not found | `GetFrozenClipForCell` return type used the namespace-qualified form | Resolved via geometry bridge (see below) |

### Bonus: `NotifyCurrentCellContainerChanged` ambiguity

The upstream 0-arg `NotifyCurrentCellContainerChanged()` was unguarded but the local
shim provides a 2-arg version with optional parameters (also callable with 0 args).
`GetMethod` without parameter types threw `AmbiguousMatchException` in the test.
Fixed by guarding the upstream 0-arg version with `#if !HAS_UNO`.

## Geometry Type Bridge

### Problem

`OnCoerceClip` in the Frozen Columns region references:
- `Geometry` — abstract base for clipping shapes
- `CombinedGeometry` — intersects two geometries
- `GeometryCombineMode` — enum for combine operations

WinUI has `Microsoft.UI.Xaml.Media.Geometry` but its constructor is `internal` —
external code cannot subclass it. WPF's full `Geometry` type from PresentationCore
is entangled with milcore P/Invokes and `Animatable`/`DUCE.IResource` — not linkable.

### Solution

Three-layer bridge:

**`GeometryCombineMode`** — a pure generated enum with no dependencies.
Linked directly from the WPF source tree:
```xml
<Compile Include="...\PresentationCore\...\Generated\GeometryCombineMode.cs"
         Link="System.Windows.Media\GeometryCombineMode.cs" />
```

**`Geometry`** — globally aliased to `Microsoft.UI.Xaml.Media.Geometry` in `GlobalUsings.cs`:
```csharp
global using Geometry = Microsoft.UI.Xaml.Media.Geometry;
```
Files that have `using System.Windows.Media;` now resolve `Geometry` to the WinUI
XAML base type, which is correct for clipping (`UIElement.Clip`) and cast chains.

**`CombinedGeometry`** — standalone shim class with an implicit conversion operator
to `Geometry` (`Microsoft.UI.Xaml.Media.Geometry`). Cannot subclass `Geometry`
directly (internal constructor), so the assignment `geometry = new CombinedGeometry(...)`
works via the operator:

```csharp
public sealed class CombinedGeometry
{
    public CombinedGeometry(GeometryCombineMode mode, Geometry? g1, Geometry? g2) { ... }
    public Geometry? Geometry1 { get; set; }
    public Geometry? Geometry2 { get; set; }
    public GeometryCombineMode GeometryCombineMode { get; set; }
    public Microsoft.UI.Xaml.Media.Transform? Transform { get; set; }

    public static implicit operator Geometry(CombinedGeometry _)
        => new Microsoft.UI.Xaml.Media.RectangleGeometry();
}
```

The implicit conversion returns an empty `RectangleGeometry`. This is safe because:
1. `GetFrozenClipForCell` always returns `null` — the `CombinedGeometry` constructor
   inside `OnCoerceClip` is never reached at runtime.
2. `ClipProperty.OverrideMetadata` (which registers `OnCoerceClip`) lives in the
   `#if !HAS_UNO` static cctor — `OnCoerceClip` is not registered on the HAS_UNO path.

**Future work:** implement via Win2D/Uno2D:
`CanvasGeometry.CombineWith(CanvasCombineMode.Intersect)` → `PathGeometry` → `UIElement.Clip`.

### `DataGridHelper.GetFrozenClipForCell` return type

Updated to return `Geometry?` (= `Microsoft.UI.Xaml.Media.Geometry?`) instead of `object?`.

## Guards Removed in Session 81

| Region | What enabled the removal |
|---|---|
| Frozen Columns `OnCoerceClip` | `Geometry`/`CombinedGeometry`/`GeometryCombineMode` bridge |
| `IsCurrent` private property | Already existed in upstream DataGrid.cs (no guard needed) |
| `OnAnyLostFocus`/`OnAnyGotFocus` | `FocusedCell` already in upstream DataGrid.cs |
| `OnIsEditingChanged` keyboard block | `IsKeyboardFocusWithin` stub on ContentControl |
| `NotifyCurrentCellContainerChanged()` call | `UpdateVisualState()` on ContentControl |
| Style coercion `OnCoerceStyle` | 7-arg `GetCoercedTransferPropertyValue` shimmed |
| `ParentPanel`, `NeedsVisualTree` helpers | `VisualParent` on ContentControl; WinUI properties |

## New Files

| File | What |
|---|---|
| `src/.../System.Windows/Media/CombinedGeometry.cs` | Standalone shim with implicit conversion to WinUI Geometry |
| `ext/.../Generated/GeometryCombineMode.cs` (linked) | Pure enum, linked from WPF PresentationCore source |

## Verification

```
dotnet build src/LeXtudio.Windows/LeXtudio.Windows.csproj   # 0 errors
dotnet test  src/LeXtudio.Windows.Tests/                     # Passed: 136
dotnet run   src/LeXtudio.Windows.Sample/ -- --probe         # DONE failures=0
```
