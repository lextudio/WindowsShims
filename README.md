# LeXtudio.Windows

[![NuGet](https://img.shields.io/nuget/v/LeXtudio.Windows.svg?label=LeXtudio.Windows&&style=flat-square)](https://www.nuget.org/packages/LeXtudio.Windows)

A compatibility library providing WPF namespace shims for the [**Uno Platform**](https://platform.uno), enabling straightforward porting of WPF code without source modification.

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

- **Uno Platform (net10.0-desktop)**: Linux, macOS, iOS, Android
- **WinUI 3 (net10.0-windows10.0.19041.0)**: Windows 10+

## Ported WPF Controls

Beyond the type shims, this library carries three full WPF controls onto Uno/WinUI. The common approach: real upstream WPF source (from the [`dotnet/wpf`](https://github.com/dotnet/wpf) submodule) is linked directly into the build with `#if HAS_UNO` guards, so large parts of each control's behavior are genuine, unmodified WPF code — not a reimplementation. Local `.uno.cs` bridge files fill in only what can't compile as-is against WinUI's native `DependencyObject`/layout model.

### ToolBar / ToolBarTray

The closest of the three to full parity. `ToolBar`, `ToolBarTray`, `ToolBarPanel`, and `ToolBarOverflowPanel` are all linked straight from upstream WPF — overflow behavior, band layout, and tray drag/dock all run real WPF logic. Only one supporting file (`ToolBarOverflowAwarePanel`) is local, bridging overflow-awareness onto Uno's layout model. No known behavioral gaps beyond general native-layout differences.

### RichTextBox

The full text-document spine is linked upstream: `RichTextBox`, `FlowDocument`, `TextElement`, `TextPointer`, `TextRange` (including edit/list/table variants), `TextSelection`, the `ITextPointer`/`ITextRange`/`ITextSelection` interfaces, undo units, and the word-breaker — all real WPF code. Local bridges add Uno-specific plumbing: pointer/hyperlink hit-testing and template wiring (`RichTextBox.uno.cs`), collection-changed notification on `TextElementCollection`, and serialization support surfaces.

**Known gap**: the `TextEditor*` spine — the internal command orchestrator behind typing, selection, and formatting commands — remains a thin bridge rather than linked upstream code; this is the largest remaining fidelity gap. Also out of scope: table layout in `FlowDocument`, fixed-document/paginator support, document sequences, and annotations.

### DataGrid

The control root (`DataGrid.cs`) and its supporting enums/value types (`DataGridLength`, selection/visibility enums, `SortDescription`) are linked upstream, alongside the column header drag-resize handler and column-collection core fields. The row/cell generation and virtualization pipeline, the `ItemsControl → Selector → MultiSelector → DataGrid` tower, and `CollectionView`/`ItemCollection` are rebased onto WinUI's `Control`/layout model via local bridges.

**Fully working**: row/column virtualization, selection, sorting, real data-level grouping (`CollectionViewGroup`/`GroupStyle`, full API coverage including selectors and `HidesIfEmpty`), frozen columns (including under virtualization, with real editing and selection), column resize/reorder with a floating drag header and Escape-to-cancel, column-header container reuse, hyperlink columns, real cell editing, row details (including variable-height rows under virtualization), clipboard copy, and keyboard incremental search (`TextSearch`, including dotted `TextPath` property matching and real OS double-click-interval timing).

**Known gaps**:
- **Accessibility / UI Automation** is not implemented — the largest remaining gap. This is a deliberate, honest stub (`AutomationPeer.FromElement` returns null) rather than a silent partial implementation. (Plan to address this with Uno Platform v6.6's new accessibility support.)

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

- **Target Frameworks**: `net10.0-desktop` (Uno) and `net10.0-windows10.0.19041.0` (WinUI)
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
