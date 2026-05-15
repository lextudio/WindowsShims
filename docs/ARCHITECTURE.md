# LeXtudio.Windows Architecture

## Overview

LeXtudio.Windows bridges **WPF API compatibility** with **modern Microsoft.UI.Xaml/Uno Platform** technology. Rather than reimplementing WPF types, it acts as a transparent facade that maps WPF namespaces and APIs to their Uno/WinUI equivalents.

## Design Principles

### 1. **Type Forwarding via Global Using Aliases**

Core types like `Brush`, `TextDecorationCollection`, and `Typeface` are not reimplemented—they are **aliased** directly to `Microsoft.UI.Xaml.Media` equivalents via `GlobalUsings.cs`.

```csharp
global using Brush = Microsoft.UI.Xaml.Media.Brush;
global using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
global using TextDecorationCollection = Microsoft.UI.Xaml.Media.TextDecorationCollection;
```

**Benefit:** 
- No type conversion overhead
- Full compatibility with WinUI capabilities
- Automatic access to all WinUI brush types (LinearGradient, Radial, Image, etc.)

### 2. **Metadata Bridging**

`FrameworkPropertyMetadata` inherits from `Microsoft.UI.Xaml.PropertyMetadata` and can be passed directly to `Microsoft.UI.Xaml.DependencyProperty.Register(...)`. It accepts both WPF-style and WinUI-style changed callbacks:

```csharp
var metadata = new FrameworkPropertyMetadata(
    defaultValue: 12,
    changed: (d, e) => { /* WPF PropertyChangedCallback */ }
);
DependencyProperty.Register("MyProp", typeof(double), typeof(MyClass), metadata);
```

Note: WPF `PropertyChangedCallback` is stored locally but **not dispatched** through WinUI's property system, because `System.Windows.DependencyObject` does not inherit from `Microsoft.UI.Xaml.DependencyObject`.

### 3. **Extension Members for Missing Members**

WPF `TextElement` doesn't natively have `FontWeight` and `FontStyle` in Uno. Extensions provide them, so use that C# language feature:

```csharp
textElement.SetFontWeight(FontWeights.Bold);
textElement.SetFontStyle(FontStyles.Italic);

var weight = textElement.GetFontWeight();
```

## File Organization

```
System.Windows/
├── Media/
│   ├── Brush.cs                    // Documentation; real type is global using alias
│   ├── TextDecorationCollection.cs // Global using alias
│   └── FontFamily.cs               // Global using alias
├── Documents/
│   ├── TextElement.cs              // Core WPF document element
│   ├── TextElementFontExtensions.cs // Adds FontWeight/FontStyle properties
│   └── ...                         // Other document types
├── FrameworkPropertyMetadata.cs    // Bridges to WinUI with callback conversion
├── PropertyCallbackAdapter.cs      // Delegate adaptation logic
└── PropertySystem.cs               // PropertyChangedCallback definition

GlobalUsings.cs                     // Central place for all type-forwarding aliases
```

## Supported Targets

- **net9.0-desktop** (Uno Platform): Uses Microsoft.UI.Xaml via aliases
- **net9.0-windows10.0.19041.0** (Windows App SDK): Uses Microsoft.UI.Xaml directly

Both targets get the same WPF API surface but powered by modern WinUI underneath.

## Type Conversion Reference

### Automatically Forwarded (via GlobalUsings)
- `System.Windows.Media.Brush` → `Microsoft.UI.Xaml.Media.Brush`
- `System.Windows.Media.SolidColorBrush` → `Microsoft.UI.Xaml.Media.SolidColorBrush`
- `System.Windows.Media.FontFamily` → `Microsoft.UI.Xaml.Media.FontFamily`
- `System.Windows.Media.TextDecorationCollection` → `Microsoft.UI.Xaml.Media.TextDecorationCollection`
- All standard enums (FlowDirection, TextAlignment, TextWrapping)

### Adapted (via Custom Code)
- `System.Windows.FrameworkPropertyMetadata` **inherits from** `Microsoft.UI.Xaml.PropertyMetadata` — can be passed directly to `Microsoft.UI.Xaml.DependencyProperty.Register(...)`. **Single unified class**: constructors accept both `System.Windows.PropertyChangedCallback` (WPF style) and `Microsoft.UI.Xaml.PropertyChangedCallback` (WinUI style); the previous `LeXtudio.UI.Xaml.FrameworkPropertyMetadata` has been removed.
- `System.Windows.PropertyChangedCallback` (WPF delegate signature kept for source compatibility with linked WPF code; not dispatched from WinUI's notification path because `System.Windows.DependencyObject` doesn't inherit from `Microsoft.UI.Xaml.DependencyObject`)

### Unified Types (single source of truth)
- **`DependencyProperty`** = `Microsoft.UI.Xaml.DependencyProperty` (no separate WPF class). Extended via:
  - Instance extension methods: `AddOwner(Type)`, `AddOwner(Type, FrameworkPropertyMetadata)`, `OverrideMetadata(Type, FrameworkPropertyMetadata)`, `GlobalIndex`
  - Static extension methods (C# 14): `Register(...)` and `RegisterAttached(...)` 5-arg overloads accepting `ValidateValueCallback`
  - All defined in `System.Windows/WinUIDependencyPropertyExtensions.cs`

### Extended (via Extension Methods)
- `TextElement.FontWeight` (attached property)
- `TextElement.FontStyle` (attached property)
- `Microsoft.UI.Xaml.DependencyProperty.AddOwner(...)` (WPF API not in WinUI)

### Do Not Port: WPF Types with No WinUI Rendering Path

> **Policy**: Do not compile WPF types into this shim library unless a WinUI rendering or layout equivalent exists. Compiling a type just to make it *instantiable* is not sufficient — without a host that can render it, the type is inert and its porting cost is wasted.

**Counter-example — FlowDocument table model** (`Table`, `TableRow`, `TableCell`, etc.)

These were briefly compiled from upstream WPF source. The attempt was abandoned because:

- WinUI has no `FlowDocument` pipeline; there is no `TableCell`-to-visual translation layer.
- The WPF source pulled in ~10 foundation types (`IAcceptInsertion`, `IIndexedChild`, `ContentElementCollection`, `TableTextElementCollectionInternal`, `RowSpanVector`, …) and several local no-op stubs.
- Despite compiling cleanly, the objects could never be rendered by any WinUI-side consumer.
- Net result: dead API surface, maintenance burden, and misleading confidence.

**What to do instead**: If a consuming project needs table layout, implement it as a native WinUI control (`Grid`, custom `Panel`, etc.) and expose a WPF-compatible façade *only if* a real rendering path exists end-to-end.

## Pre-Porting Checklist — Avoiding Wasted Work

Before adding any WPF type to this shim, answer these questions in order. Stop at the first **No**.

| # | Question | If No → action |
|---|----------|----------------|
| 1 | Does a direct WinUI equivalent exist (`Microsoft.UI.Xaml.*` or `Windows.Foundation.*`)? | Alias it in `GlobalUsings.cs` — no shim needed. |
| 2 | Does WinUI have a *close* equivalent that can be extended? | Extend via C# extension members (`WinUIDependencyPropertyExtensions.cs` pattern). |
| 3 | Is there a WinUI-side consumer (control, panel, layout pass) that will actually *render* this type? | Do not port. Document the gap here instead (see below). |
| 4 | Is the porting cost proportionate to the value delivered to active consumers? | Defer or skip. |

### How to Check for WinUI Equivalents

**Option A — API catalog search (recommended)**

The WinUI API surface is documented at:
- `https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/` (Windows App SDK / WinUI 3)
- `https://learn.microsoft.com/en-us/uwp/api/` (UWP/WinUI 2 — still relevant for Uno)

Search for the WPF type name or concept. If no result, the gap is real.

**Option B — `dotnet-api-diff` / `ApiCompat`**

```powershell
# Compare WPF and WinUI assembly API surfaces
dotnet tool install -g Microsoft.DotNet.ApiCompat.Tool
# Then diff Microsoft.UI.Xaml.dll against PresentationFramework.dll
```

**Option C — Grep the WinUI metadata**

```powershell
# Download the WinUI NuGet and inspect exported types
dotnet new classlib -o gap-check
cd gap-check
dotnet add package Microsoft.WindowsAppSDK
# Then use ILSpy or dnSpy to inspect Microsoft.UI.Xaml.dll
```

**Option D — Ask before implementing**

When unsure, open a search query like:
> `site:learn.microsoft.com winui TableCell` or `site:github.com/microsoft/microsoft-ui-xaml TableCell`

If there are zero results, assume the type has no WinUI home.

### Known WPF→WinUI Gaps (do not port)

| WPF namespace | Types | Reason |
|---|---|---|
| `System.Windows.Documents` | `Table`, `TableRow`, `TableCell`, `TableColumn`, `TableRowGroup`, `*Collection` | No FlowDocument pipeline in WinUI; no rendering consumer. |
| `System.Windows.Documents` | `FlowDocument`, `FixedDocument`, `FixedPage` | WinUI uses `RichEditBox` / custom controls instead. |
| `System.Windows.Documents` | `TextPointer`, `TextRange`, `TextSelection` | WinUI text model is `ITextRange` / `CoreTextEditContext`; different contract. |
| `System.Windows.Controls` | `RichTextBox` (WPF full) | WinUI has `RichEditBox` — different model; write a façade over `RichEditBox` rather than porting. |
| `System.Windows` | `FrameworkElement.ContextMenu` (WPF-style) | WinUI uses `MenuFlyout`; expose through extension if needed. |

Add to this table whenever a porting attempt is abandoned.

## Usage Example

```csharp
// WPF-style code that "just works" with Uno Platform
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;

var run = new Run { Text = "Hello" };
run.SetFontWeight(FontWeights.Bold);           // Extension method
run.Foreground = new SolidColorBrush(Colors.Blue); // Aliased type

var brush = run.Foreground as Brush;           // Works seamlessly
```

Under the hood:
- `SolidColorBrush` = `Microsoft.UI.Xaml.Media.SolidColorBrush`
- `Brush` = `Microsoft.UI.Xaml.Media.Brush`
- `Colors.Blue` = `Windows.UI.Colors.Blue`
- No manual conversion needed

## Benefits of This Approach

1. **Zero Type Conversion Overhead**: Aliases are compile-time only
2. **Full WinUI Compatibility**: All WinUI capabilities are immediately available
3. **Clean WPF API Surface**: Code reads as if using WPF, but runs on modern platform
4. **Minimal Shim Code**: Only bridge adapter logic, not type reimplementation
5. **Future-Proof**: As WinUI evolves, brush types, text effects, etc. automatically improve

## Adding New Forwarded Types

To add a new type forwarding:

1. Add to `GlobalUsings.cs`:
   ```csharp
   global using MyType = Microsoft.UI.Xaml.MyType;
   ```

2. No other changes needed—type is immediately available in `System.Windows.Media` namespace.

## Limitations

- **PropertyChangedCallback not dispatched**: WPF-style `PropertyChangedCallback` stored in `FrameworkPropertyMetadata` is never called from WinUI's property change notification path, because `System.Windows.DependencyObject` does not inherit from `Microsoft.UI.Xaml.DependencyObject`.
- **Document semantics are thin shims**: `TextElement`, `Block`, `Inline` etc. expose the WPF content model for authoring but have no WPF layout engine behind them. Rendering is the consumer's responsibility.
- **No FlowDocument pipeline**: WinUI has no equivalent of WPF's `FlowDocumentScrollViewer` / `DocumentPaginator`. Types that rely on it (table model, fixed document, text pointers) cannot be meaningfully ported — see the Pre-Porting Checklist above.
- **Extension method indirection**: C# extension members for `DependencyProperty` and `TextElement` properties add a small call overhead vs. native properties.

## See Also

- `System.Windows\FrameworkPropertyMetadata.cs` — Metadata bridging
- `System.Windows\WinUIDependencyPropertyExtensions.cs` — 5-arg Register overloads, AddOwner, OverrideMetadata
- `GlobalUsings.cs` — Central type-forwarding aliases
