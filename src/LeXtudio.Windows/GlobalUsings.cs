// Type aliases that bridge WPF API to Microsoft.UI.Xaml.
// These enable WPF code to seamlessly use modern Uno/WinUI types without modifications.
// Key insight: Only forward types that have 1:1 equivalents in Microsoft.UI.Xaml.
// For types without equivalents (TextDecoration, Typeface, etc.), keep shim implementations.

global using System;
global using System.Diagnostics;

// ============ Media Types - Direct Microsoft.UI.Xaml Forwards ============
// Note: DO NOT include 'Brush' here - WPF source files define their own 'using Brush' statements
// and global alias conflicts with them. Brush forwarding happens in individual namespaces instead.

// Brush subclasses that map directly to WinUI
global using SolidColorBrush           = Microsoft.UI.Xaml.Media.SolidColorBrush;
global using LinearGradientBrush       = Microsoft.UI.Xaml.Media.LinearGradientBrush;
global using RadialGradientBrush       = Microsoft.UI.Xaml.Media.RadialGradientBrush;
global using ImageBrush                = Microsoft.UI.Xaml.Media.ImageBrush;
global using Brush                     = Microsoft.UI.Xaml.Media.Brush;
global using FontFamily                = Microsoft.UI.Xaml.Media.FontFamily;
global using FontWeight                = Windows.UI.Text.FontWeight;
global using FontStyle                 = Windows.UI.Text.FontStyle;
global using FontStretch               = Windows.UI.Text.FontStretch;

// ============ Geometry Types ============
global using Rect                      = Windows.Foundation.Rect;
global using Size                      = Windows.Foundation.Size;
global using Point                     = Windows.Foundation.Point;
global using Color                     = Windows.UI.Color;

// ============ Xaml Framework Types ============
global using FlowDirection             = Microsoft.UI.Xaml.FlowDirection;
global using TextAlignment             = Microsoft.UI.Xaml.TextAlignment;
global using TextWrapping              = Microsoft.UI.Xaml.TextWrapping;
global using TextBlock                 = Microsoft.UI.Xaml.Controls.TextBlock;

// ============ Dependency Property System ============
// DependencyProperty: unqualified usage routes to WinUI's native type.
// Extension methods in System.Windows/WinUIDependencyPropertyExtensions.cs provide the
// WPF-specific API (AddOwner, OverrideMetadata, etc.).
global using DependencyProperty        = Microsoft.UI.Xaml.DependencyProperty;
// FrameworkPropertyMetadata/Options: route to the shim types so WPF source files that pass
// FrameworkPropertyMetadata to RegisterAttached/Register compile without CS0104 ambiguity.
global using FrameworkPropertyMetadata        = System.Windows.FrameworkPropertyMetadata;
global using FrameworkPropertyMetadataOptions = System.Windows.FrameworkPropertyMetadataOptions;
// DependencyObject: alias to the real WinUI type so document objects participate in WinUI's
// property system. WPF-specific APIs (AddHandler, Dispatcher, etc.) are provided via
// C# 14 extension members in System.Windows/WinUIDependencyObjectExtensions.cs.
global using DependencyObject          = Microsoft.UI.Xaml.DependencyObject;
// FrameworkElement: alias to the real WinUI type. WPF-only APIs are provided via
// C# 14 extension members in System.Windows/WinUIFrameworkElementExtensions.cs.
// FrameworkContentElement has no WinUI equivalent — it stays as a local shim.
global using FrameworkElement          = Microsoft.UI.Xaml.FrameworkElement;
global using Visual                    = Microsoft.UI.Xaml.UIElement;
// Panel: alias to the WPF shim to resolve CS0104 between System.Windows.Controls.Panel and Microsoft.UI.Xaml.Controls.Panel.
global using Panel                     = System.Windows.Controls.Panel;
global using Control                   = System.Windows.Controls.Control;
// PropertyPath/BindingExpressionBase: WPF source files mean the WPF-shaped
// binding types, and Uno's implicit usings would otherwise resolve unqualified
// references to the Microsoft.UI.Xaml(.Data) types.
global using PropertyPath              = System.Windows.Data.PropertyPath;
global using BindingExpressionBase     = System.Windows.Data.BindingExpressionBase;
global using GeneralTransform          = System.Windows.Media.GeneralTransform;

// ============ Types with Shim Implementations (No Direct WinUI Equivalent) ============
// These are kept as local implementations because they don't exist in Microsoft.UI.Xaml:
// - Brush (base class - defined in System.Windows.Media.Brush.cs)
// - TextDecoration, TextDecorationCollection (WPF-specific)
// - TextEffect, TextEffectCollection (WPF-specific)
// - Typeface (WPF-specific typography type)

// ============ WPF Document Types ============
global using TextDecorationCollection = System.Windows.Media.TextDecorationCollection;
// ContextMenuEventArgs: WinUI also defines one in Microsoft.UI.Xaml.Controls; alias to WPF shim so upstream TextEditor* files compile.
global using ContextMenuEventArgs = System.Windows.Controls.ContextMenuEventArgs;

// TextDecorations static class (System.Windows.Media) — bring to global scope so
// upstream files in System.Windows.Documents can reference it without an explicit using.
global using TextDecorations                = System.Windows.Media.TextDecorations;
global using LocalizabilityAttribute        = System.Windows.Markup.LocalizabilityAttribute;
global using LocalizationCategory          = System.Windows.Markup.LocalizationCategory;
global using Readability                    = System.Windows.Markup.Readability;

// ============ Typography Enums - Aliased to Microsoft.UI.Xaml Equivalents ============
// WinUI defines the same OpenType feature enums with identical members/values.
global using FontVariants              = Microsoft.UI.Xaml.FontVariants;
global using FontNumeralStyle          = Microsoft.UI.Xaml.FontNumeralStyle;
global using FontNumeralAlignment      = Microsoft.UI.Xaml.FontNumeralAlignment;
global using FontFraction              = Microsoft.UI.Xaml.FontFraction;
global using FontEastAsianWidths       = Microsoft.UI.Xaml.FontEastAsianWidths;
global using FontEastAsianLanguage     = Microsoft.UI.Xaml.FontEastAsianLanguage;
global using FontCapitals              = Microsoft.UI.Xaml.FontCapitals;
