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

### 2. **Delegate Adaptation**

WPF and WinUI delegate signatures differ slightly. The `PropertyCallbackAdapter` class seamlessly converts between:
- `System.Windows.PropertyChangedCallback` ↔ `Microsoft.UI.Xaml.PropertyChangedCallback`
- `System.Windows.DependencyPropertyChangedEventArgs` ↔ `Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs`

```csharp
// Internal use only—handled automatically by FrameworkPropertyMetadata
var winuiCallback = PropertyCallbackAdapter.ToWinUI(systemWindowsCallback);
```

**Why?** The two frameworks have the same semantic intent but different namespace origins. The adapter handles the bridge transparently.

### 3. **Metadata Bridging**

`FrameworkPropertyMetadata` stores WPF-style metadata but converts to WinUI equivalents when needed via `ToWinUIMetadata()`:

```csharp
// WPF code
var metadata = new FrameworkPropertyMetadata(
    defaultValue: 12,
    options: FrameworkPropertyMetadataOptions.AffectsRender,
    changed: myCallback
);

// Internally converts to WinUI when registering properties
var winuiMetadata = metadata.ToWinUIMetadata();
```

### 4. **Extension Methods for Missing Members**

WPF `TextElement` doesn't natively have `FontWeight` and `FontStyle` in Uno. Extensions provide them:

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
- `System.Windows.FrameworkPropertyMetadata` ↔ `Microsoft.UI.Xaml.FrameworkPropertyMetadata`
- `System.Windows.PropertyChangedCallback` ↔ `Microsoft.UI.Xaml.PropertyChangedCallback`

### Extended (via Extension Methods)
- `TextElement.FontWeight` (attached property)
- `TextElement.FontStyle` (attached property)

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

- **PropertyChangedCallback**: Must use `PropertyCallbackAdapter.ToWinUI()` when passing WPF callbacks to WinUI APIs
- **Complex Document Semantics**: Some WPF-specific document behaviors (layout, selection, undo) are simplified in the shim
- **Performance**: Extension methods for properties add indirection vs. native WPF implementation

## See Also

- `System.Windows\PropertyCallbackAdapter.cs` — Delegate conversion logic
- `System.Windows\FrameworkPropertyMetadata.cs` — Metadata bridging
- `GlobalUsings.cs` — Central type-forwarding declarations
