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
    public const string StateCurrent  = "Current";
    public const string StateRegular  = "Regular";
    public const string StateDisplay  = "Display";
    public const string StateEditing  = "Editing";
    public const string StateMouseOver = "MouseOver";
    public const string StatePressed  = "Pressed";
    public const string StateUnsorted = "Unsorted";
    public const string StateSortAscending = "SortAscending";
    public const string StateSortDescending = "SortDescending";

    // DataGridRow visual state names (used by DataGridRow.ChangeVisualState state machine)
    public const string DATAGRIDROW_stateAlternate              = "Normal_AlternatingRow";
    public const string DATAGRIDROW_stateMouseOver              = "MouseOver";
    public const string DATAGRIDROW_stateMouseOverEditing       = "MouseOver_Unfocused_Editing";
    public const string DATAGRIDROW_stateMouseOverEditingFocused = "MouseOver_Editing";
    public const string DATAGRIDROW_stateMouseOverSelected      = "MouseOver_Unfocused_Selected";
    public const string DATAGRIDROW_stateMouseOverSelectedFocused = "MouseOver_Selected";
    public const string DATAGRIDROW_stateNormal                 = "Normal";
    public const string DATAGRIDROW_stateNormalEditing          = "Unfocused_Editing";
    public const string DATAGRIDROW_stateNormalEditingFocused   = "Normal_Editing";
    public const string DATAGRIDROW_stateSelected               = "Unfocused_Selected";
    public const string DATAGRIDROW_stateSelectedFocused        = "Normal_Selected";

    // DataGridRowHeader visual state names (used by DataGridRowHeader.ChangeVisualState state machine)
    public const string DATAGRIDROWHEADER_stateMouseOver = "MouseOver";
    public const string DATAGRIDROWHEADER_stateMouseOverCurrentRow = "MouseOver_CurrentRow";
    public const string DATAGRIDROWHEADER_stateMouseOverEditingRow = "MouseOver_Unfocused_EditingRow";
    public const string DATAGRIDROWHEADER_stateMouseOverEditingRowFocused = "MouseOver_EditingRow";
    public const string DATAGRIDROWHEADER_stateMouseOverSelected = "MouseOver_Unfocused_Selected";
    public const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRow = "MouseOver_Unfocused_CurrentRow_Selected";
    public const string DATAGRIDROWHEADER_stateMouseOverSelectedCurrentRowFocused = "MouseOver_CurrentRow_Selected";
    public const string DATAGRIDROWHEADER_stateMouseOverSelectedFocused = "MouseOver_Selected";
    public const string DATAGRIDROWHEADER_stateNormal = "Normal";
    public const string DATAGRIDROWHEADER_stateNormalCurrentRow = "Normal_CurrentRow";
    public const string DATAGRIDROWHEADER_stateNormalEditingRow = "Unfocused_EditingRow";
    public const string DATAGRIDROWHEADER_stateNormalEditingRowFocused = "Normal_EditingRow";
    public const string DATAGRIDROWHEADER_stateSelected = "Unfocused_Selected";
    public const string DATAGRIDROWHEADER_stateSelectedCurrentRow = "Unfocused_CurrentRow_Selected";
    public const string DATAGRIDROWHEADER_stateSelectedCurrentRowFocused = "Normal_CurrentRow_Selected";
    public const string DATAGRIDROWHEADER_stateSelectedFocused = "Normal_Selected";

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

    private string _prefix = string.Empty;

    internal void DoSearch(string nextChar)
    {
        _prefix += nextChar;
        if (_prefix.Length > 20)
            _prefix = _prefix[^20..];

        if (TryMatchAndSelect(_prefix))
            return;

        // Not found with accumulated prefix; restart from the new character.
        _prefix = nextChar;
        TryMatchAndSelect(_prefix);
    }

    private bool TryMatchAndSelect(string prefix)
    {
        var gen = _owner?.ItemContainerGenerator;
        if (gen is null)
            return false;

        for (int i = 0; i < _owner.Items.Count; i++)
        {
            var item = _owner.Items[i];
            var text = item is DependencyObject d ? GetText(d) : null
                       ?? item?.ToString();
            if (text is not null && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var container = gen.ContainerFromIndex(i);
                if (container is Microsoft.UI.Xaml.FrameworkElement fe)
                {
                    fe.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
                }
                return true;
            }
        }
        return false;
    }
}

// ContentElement: WPF base for non-UIElement logical content (e.g. Hyperlink).
// DataGrid only tests for it as a focus fallback; the path is unreachable in
// the shim since Keyboard.FocusedElement returns null for non-UI elements.
public abstract class ContentElement
{
    public bool MoveFocus(Input.TraversalRequest request) => false;
}
