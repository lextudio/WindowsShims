using System.Windows.Controls.Primitives;
using MS.Internal;

namespace System.Windows.Controls;

// FrameworkElementFactory: WPF template-building API not available in WinUI.
// The static ctor of DataGrid uses it to build a default ItemsPanel template;
// under Uno the template is applied from XAML/resources so this is a no-op.
public sealed class FrameworkElementFactory
{
    public FrameworkElementFactory(Type type) { }
    public FrameworkElementFactory(Type type, string? name) { }
    public void SetValue(DependencyProperty dp, object? value) { }
    public void AppendChild(FrameworkElementFactory child) { }
}

// ItemsPanelTemplate: wraps a FrameworkElementFactory. Uno resolves the
// DataGrid row-panel from XAML so this stub only needs to exist for
// compilation.
public sealed class ItemsPanelTemplate
{
    public ItemsPanelTemplate() { }
    public ItemsPanelTemplate(FrameworkElementFactory factory) { }
}

// BooleanToSelectiveScrollingOrientationConverter: converter used in DataGrid
// row-details templates. Not reachable until template materialisation.
internal sealed class BooleanToSelectiveScrollingOrientationConverter
    : System.Windows.Data.IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture) => null;
    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture) => null;
}

// VirtualizationMode: controls whether virtualizing panels recycle or
// discard containers. Forwarded to WinUI VirtualizingStackPanel.
public enum VirtualizationMode
{
    Standard = 0,
    Recycling = 1,
}

// VisualStates: WPF common-state-name constants + GoToState helper.
internal static class VisualStates
{
    public const string StateNormal   = "Normal";
    public const string StateDisabled = "Disabled";
    public const string StateFocused  = "Focused";
    public const string StateUnfocused = "Unfocused";
    public const string StateValid    = "Valid";
    public const string StateInvalidFocused  = "InvalidFocused";
    public const string StateInvalidUnfocused = "InvalidUnfocused";
    public const string StateSelected = "Selected";
    public const string StateUnselected = "Unselected";
    public const string StateSelectedInactive = "SelectedInactive";
    public const string StateCurrentCell = "CurrentCell";
    public const string StateRegularCell = "RegularCell";
    public const string StateDisplay = "Display";
    public const string StateEditing  = "Editing";
    public const string StateMouseOver = "MouseOver";

    public static bool GoToState(Microsoft.UI.Xaml.FrameworkElement element, bool useTransitions, params string[] states)
    {
        if (element is Control control)
        {
            foreach (var state in states)
            {
                if (Microsoft.UI.Xaml.VisualStateManager.GoToState(control, state, useTransitions))
                    return true;
            }
        }
        return false;
    }
}

// SystemResourceKey: WPF resource-key for system-defined brushes; stubs for
// focus-border lookup which is resolved via XAML styles at runtime.
// Extends ComponentResourceKey so it can be stored in ComponentResourceKey-typed fields.
public sealed class SystemResourceKey : System.Windows.ComponentResourceKey
{
    private SystemResourceKey() : base(typeof(SystemResourceKey), "DataGridFocusBorderBrush") { }
    public static SystemResourceKey DataGridFocusBorderBrushKey { get; } = new SystemResourceKey();
    // Session 60: style key for the linked DataGridComboBoxColumn.TextBlockComboBox.
    public static SystemResourceKey DataGridComboBoxColumnTextBlockComboBoxStyleKey { get; } = new SystemResourceKey();
}

// TextSearch: WPF attached-property based incremental-search support on
// ItemsControl. DataGrid holds an instance and calls DoSearch on key input.
public class TextSearch
{
    private readonly ItemsControl _owner;

    private TextSearch(ItemsControl owner) => _owner = owner;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(TextSearch),
            new PropertyMetadata(null));

    public static string? GetText(DependencyObject element)
        => (string?)element.GetValue(TextProperty);

    public static void SetText(DependencyObject element, string? value)
        => element.SetValue(TextProperty, value);

    internal static TextSearch? EnsureInstance(ItemsControl owner) => new TextSearch(owner);

    internal void DoSearch(string nextChar) { }
}

// ContentElement: WPF base for non-UIElement logical content (e.g. Hyperlink).
// DataGrid only tests for it as a focus fallback; the path is unreachable in
// the shim since Keyboard.FocusedElement returns null for non-UI elements.
public abstract class ContentElement
{
    public bool MoveFocus(Input.TraversalRequest request) => false;
}
