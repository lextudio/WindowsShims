using System.Windows.Controls;

namespace System.Windows.Input;

// KeyboardNavigationMode and TraversalRequest/FocusNavigationDirection are
// already declared in their own shim files (KeyboardNavigationMode.cs,
// TextEditorInputShims.cs / TraversalRequest.cs linked from WPF source).

public class KeyboardNavigation
{
    public static readonly DependencyProperty DirectionalNavigationProperty =
        DependencyProperty.Register("KeyboardNavigation_DirectionalNavigation", typeof(KeyboardNavigationMode),
            typeof(KeyboardNavigation), new PropertyMetadata(KeyboardNavigationMode.Continue));

    public static readonly DependencyProperty TabNavigationProperty =
        DependencyProperty.Register("KeyboardNavigation_TabNavigation", typeof(KeyboardNavigationMode),
            typeof(KeyboardNavigation), new PropertyMetadata(KeyboardNavigationMode.Continue));

    public static readonly DependencyProperty ControlTabNavigationProperty =
        DependencyProperty.Register("KeyboardNavigation_ControlTabNavigation", typeof(KeyboardNavigationMode),
            typeof(KeyboardNavigation), new PropertyMetadata(KeyboardNavigationMode.Continue));

    public static readonly DependencyProperty AcceptsReturnProperty =
        DependencyProperty.RegisterAttached("KeyboardNavigation_AcceptsReturn", typeof(bool),
            typeof(KeyboardNavigation), new PropertyMetadata(false));

    // IsTabStop attached property (WPF-internal, used by DataGridComboBoxColumn).
    public static readonly DependencyProperty IsTabStopProperty =
        DependencyProperty.RegisterAttached("IsTabStop", typeof(bool),
            typeof(KeyboardNavigation), new PropertyMetadata(true));

    public static bool IsKeyboardMostRecentInputDevice() => false;

    // Event fired when focus enters the main focus scope (used by Selector).
    public event EventHandler? FocusEnterMainFocusScope;
    public static DependencyObject? GetVisualRoot(DependencyObject element) => null;
    public static void ShowFocusVisual() { }
    public static KeyboardNavigationMode GetDirectionalNavigation(DependencyObject element)
        => (KeyboardNavigationMode)(element.GetValue(DirectionalNavigationProperty) ?? KeyboardNavigationMode.Continue);
    public static bool GetIsTabStop(DependencyObject element)
        => (bool)(element.GetValue(IsTabStopProperty) ?? true);

    // Instance API used by DataGrid and Selector keyboard navigation.
    public static KeyboardNavigation Current { get; } = new KeyboardNavigation();
    public void UpdateActiveElement(DependencyObject container, DependencyObject activeElement) { }
    public DependencyObject? PredictFocusedElement(DependencyObject element,
        FocusNavigationDirection direction,
        bool treeViewNavigation, bool considerDescendants) => null;
    public bool IsAncestorOfEx(DependencyObject ancestor, DependencyObject element) => false;
}
