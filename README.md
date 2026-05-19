# LeXtudio.Windows

[![NuGet](https://img.shields.io/nuget/v/LeXtudio.Windows.svg?label=LeXtudio.Windows&&style=flat-square)](https://www.nuget.org/packages/LeXtudio.Windows)

A compatibility library providing WPF namespace shims for the **Uno Platform**, enabling straightforward porting of WPF code without source modification.

## Overview

`LeXtudio.Windows` implements portable equivalents of WPF types from `System.Windows.*`, `System.Windows.Media.*`, and `System.Windows.Documents.*` namespaces. These shims allow WPF-based libraries to compile and run on Uno Platform (both Uno Desktop and WinUI 3) with minimal code changes.

## Why Use This?

When porting a WPF library to Uno Platform, you typically face a choice:
1. Modify all source files to remove WPF dependencies
2. Use compatibility shims to maintain the original surface

This library chooses option 2. By providing stub implementations of common WPF types, it lets you keep your source code structure intact while running on Uno/WinUI targets.

## Installation

```bash
dotnet add package LeXtudio.Windows
```

Or via NuGet Package Manager:
```
Install-Package LeXtudio.Windows
```

## Supported Platforms

- **Uno Platform (net9.0-desktop)**: Linux, macOS, iOS, Android
- **WinUI 3 (net9.0-windows10.0.19041.0)**: Windows 10+

## What's Included

### System.Windows
- WPF-compatible core types and runtime helpers including `DependencyObject`, `FrameworkElement`, `DependencyProperty`, `RoutedEvent`, `RoutedCommand`, and `IWeakEventListener`
- Clipboard and input support via `DataObject`, `DataFormats`, `Keyboard`, `Mouse`, `CommandBinding`, and `IInputElement`
- Platform-friendly helpers such as `Dispatcher`, `SystemFonts`, `SystemColors`, `FocusManager`, and `InputLanguageManager`

### System.Windows.Media
- WinUI-backed media helpers for `Brushes`, `Colors`, `ImageSource`, `Pen`, and `Matrix`
- Text layout and formatting support with `Typeface`, `FormattedText`, `DrawingContext`, `CompositionTarget`, and `NumberSubstitution`
- Rich text styling via `TextDecorations`, `TextDecorationCollection`, `TextEffect`, and text formatting helpers

### System.Windows.Documents
- Rich document model elements like `FlowDocument`, `Paragraph`, `Run`, `Span`, `Hyperlink`, and `Table`
- Editing and selection types including `TextRange`, `TextPointer`, `TextSelection`, `TextElement`, `TextContainer`, `List`, and `ListItem`
- Serialization and document interoperability helpers for XAML/RTF scenarios

### System.Windows.Markup and Controls
- XAML serialization helpers such as `XamlReader`, `XamlWriter`, and `XamlDesignerSerializationManager`
- Control shims and bridge extensions including `RichTextBox`, `TextBlock` extensions, `Image`, `PanelShims`, and WinUI interoperability helpers

## Usage Example

For a WPF-based library, no changes needed:

```csharp
using System.Windows;
using System.Windows.Media;

public class TextFormatter
{
    public void Format()
    {
        var family = new FontFamily("Consolas");
        var weight = FontWeight.Bold;
        var style = FontStyle.Italic;
        
        // Code works on WPF, Uno Desktop, and WinUI 3
    }
}
```

The library automatically provides the correct implementation for your target platform:
- On Uno Desktop, shims provide minimal functionality matching WPF surface
- On WinUI 3, shims align with WinUI equivalents

## Architecture Notes

- **Target Frameworks**: `net9.0-desktop` (Uno) and `net9.0-windows10.0.19041.0` (WinUI)
- **Nullable**: Disabled for broader compatibility with older WPF libraries
- **Documentation**: Includes XML docs for all public types
- **License**: MIT

## Limitations

These are compatibility shims, not full WPF reimplementations. They provide:
- Type definitions matching WPF's public surface
- Core properties and behaviors
- Minimal computation where needed (e.g., FontWeight conversions)

They do **not** provide:
- Full rendering pipelines
- Advanced typography features
- Performance optimizations matched to native platforms

For complex text layout or specialized rendering, defer to platform-specific APIs once shims are loaded.

## Related Projects

- **[UnoRichText](https://github.com/lextudio/UnoRichText)** — Cross platform rich text controls. Ported from RichTextBox (WPF).
- **[UnoEdit](https://github.com/lextudio/unoedit)** — Code editor for Uno Platform and WinUI. Ported from AvalonEdit (WPF).

## License

MIT — See LICENSE file in the repository.

## Contributing

Found an issue or missing type? Contributions welcome! Please file an issue or submit a PR to the [this repo](https://github.com/lextudio/WindowsShims) repository.
