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

    // Session 122: root cause of every real WPF ChangeVisualState override (DataGridRow,
    // DataGridCell, DataGridColumnHeader all call VisualStates.GoToState(this, ...)) silently
    // never applying any visual state. `Control` here — unqualified, same namespace as this
    // file (System.Windows.Controls) — resolved to *this shim's own* `Control` class, not
    // native `Microsoft.UI.Xaml.Controls.Control`. None of DataGridRow/DataGridCell/
    // DataGridColumnHeader inherit from this shim's Control (they inherit directly from
    // native WinUI base classes — e.g. DataGridColumnHeader : ButtonBase :
    // Microsoft.UI.Xaml.Controls.Primitives.ButtonBase), so `element is Control` was always
    // false and VisualStateManager.GoToState was never even called. Confirmed live: calling
    // VisualStateManager.GoToState directly on a DataGridColumnHeader instance correctly
    // applies a defined VisualState; going through this helper (as every real ChangeVisualState
    // override does) did not, until this fix.
    public static bool GoToState(Microsoft.UI.Xaml.FrameworkElement element, bool useTransitions, params string[] states)
    {
        if (element is Microsoft.UI.Xaml.Controls.Control control)
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
// ItemsControl. DataGrid.OnKeyDown maps unmodified letter/digit keys to calls
// into DoSearch (see DataGrid.cs; real WPF instead hooks a separate OnTextInput
// routed event carrying IME-composed text, which WinUI's KeyRoutedEventArgs
// doesn't expose).
public class TextSearch
{
    // Session 122: real WPF's TextSearch resets the accumulated prefix via a
    // DispatcherTimer (ResetTimeout/OnTimeout) after 2x the OS double-click
    // interval elapses since the last keystroke — so pausing mid-search starts
    // a fresh prefix instead of matching against a stale, unbounded-length
    // string. The prior local shim instead just truncated the prefix to a
    // fixed length, which doesn't reset on a pause at all. WinUI's own
    // DispatcherTimer (no separate shim needed — resolved via the project's
    // global `using Microsoft.UI.Xaml`) has the same Interval/Tick/Start/Stop
    // shape real WPF's System.Windows.Threading.DispatcherTimer does, so this
    // reuses it directly rather than hand-rolling a timer. Session 122 (follow-up):
    // reads System.Windows.SystemParameters.DoubleClickTime (already shimmed for
    // TextEditor.cs, stubbed at 500ms) instead of a hardcoded 1000ms, matching
    // real WPF's `2 * GetDoubleClickTime()` formula exactly rather than just its
    // typical result.
    private static TimeSpan ResetTimeout => TimeSpan.FromMilliseconds(SystemParameters.DoubleClickTime * 2);

    private readonly ItemsControl _owner;
    private DispatcherTimer? _resetTimer;

    private TextSearch(ItemsControl owner) => _owner = owner;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached("Text", typeof(string), typeof(TextSearch),
            new PropertyMetadata(null));

    public static string? GetText(DependencyObject element)
        => (string?)element.GetValue(TextProperty);

    public static void SetText(DependencyObject element, string? value)
        => element.SetValue(TextProperty, value);

    // Session 122 (follow-up): real WPF's TextSearch.TextPath, settable on the
    // ItemsControl itself, names a property path evaluated against each *item*
    // (not the item's container) to get its search text — this is how
    // ComboBox/ListBox's DisplayMemberPath-driven search works for plain POCOs
    // that aren't DependencyObjects and don't override ToString(). Only a plain
    // single-level property name is supported here (reflection, not a full
    // PropertyPath/BindingExpression walk — dotted multi-level paths aren't
    // resolved), which covers the common "search by this property" case without
    // the much larger PropertyPath-parsing effort real WPF's version pulls in.
    public static readonly DependencyProperty TextPathProperty =
        DependencyProperty.RegisterAttached("TextPath", typeof(string), typeof(TextSearch),
            new PropertyMetadata(null));

    public static string? GetTextPath(DependencyObject element)
        => (string?)element.GetValue(TextPathProperty);

    public static void SetTextPath(DependencyObject element, string? value)
        => element.SetValue(TextPathProperty, value);

    internal static TextSearch? EnsureInstance(ItemsControl owner) => new TextSearch(owner);

    private string _prefix = string.Empty;

    internal void DoSearch(string nextChar)
    {
        ResetTimer();

        _prefix += nextChar;
        if (TryMatchAndSelect(_prefix))
            return;

        // Not found with accumulated prefix; restart from the new character.
        _prefix = nextChar;
        TryMatchAndSelect(_prefix);
    }

    private void ResetTimer()
    {
        if (_resetTimer is null)
        {
            _resetTimer = new DispatcherTimer { Interval = ResetTimeout };
            _resetTimer.Tick += (_, _) =>
            {
                _resetTimer?.Stop();
                _prefix = string.Empty;
            };
        }
        else
        {
            _resetTimer.Stop();
        }

        _resetTimer.Start();
    }

    private bool TryMatchAndSelect(string prefix)
    {
        var textPath = GetTextPath(_owner);
        for (int i = 0; i < _owner.Items.Count; i++)
        {
            var item = _owner.Items[i];
            var text = GetItemText(item, textPath);
            if (text is not null && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // Session 122: routes through ItemsControl.NavigateToItem (DataGrid
                // overrides it to reuse MoveSelectionToIndex — real scroll-into-view +
                // selection, so a virtualized/off-screen match gets realized) instead
                // of resolving+focusing a container directly, which silently failed
                // for any match not already generated.
                _owner.NavigateToItem(item);
                return true;
            }
        }
        return false;
    }

    // Session 122 (follow-up): TextPath beats TextSearch.GetText/ToString() when
    // set. Resolution reuses System.Windows.Data.BindingExpression.EvaluatePath —
    // the same dotted-path walker the binding shim's untargeted expressions use —
    // instead of a single-property-only reflection lookup, so multi-segment paths
    // ("Owner.Name") work here too, matching real WPF's PropertyPath-based
    // TextSearch.TextPath rather than a narrower approximation of it. Falls back
    // to the attached property / ToString() chain unchanged when TextPath is
    // unset or the path doesn't resolve, so existing callers (Get/SetText, plain
    // ToString() matching) are unaffected.
    private static string? GetItemText(object? item, string? textPath)
    {
        if (item is null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(textPath) &&
            Data.BindingExpression.EvaluatePath(item, textPath) is { } value &&
            !ReferenceEquals(value, MS.Internal.Data.BindingValue.UnsetValue))
        {
            return value.ToString();
        }

        return (item is DependencyObject d ? GetText(d) : null) ?? item.ToString();
    }
}

// ContentElement: WPF base for non-UIElement logical content (e.g. Hyperlink).
// DataGrid only tests for it as a focus fallback; the path is unreachable in
// the shim since Keyboard.FocusedElement returns null for non-UI elements.
public abstract class ContentElement
{
    public bool MoveFocus(Input.TraversalRequest request) => false;
}
