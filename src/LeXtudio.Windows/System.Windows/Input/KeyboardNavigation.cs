namespace System.Windows.Input;

public enum KeyboardNavigationMode
{
    Continue,
    Once,
    Cycle,
    None,
    Contained,
    Local,
}

public enum FocusNavigationDirection
{
    Next,
    Previous,
    First,
    Last,
    Left,
    Right,
    Up,
    Down,
}

public class TraversalRequest
{
    public FocusNavigationDirection FocusNavigationDirection { get; }
    public TraversalRequest(FocusNavigationDirection direction) => FocusNavigationDirection = direction;
}

public static class KeyboardNavigation
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

    // WPF tracks whether keyboard was the last input device; on HAS_UNO always return false
    // (safe: focus will be set by pointer, not moved by keyboard traversal on overflow open).
    public static bool IsKeyboardMostRecentInputDevice() => false;
}
