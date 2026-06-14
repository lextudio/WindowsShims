namespace System.Windows.Automation;

public enum IsOffscreenBehavior
{
    Default = 0,
    Onscreen = 1,
    Offscreen = 2,
    FromClip = 3,
}

public static class AutomationProperties
{
    public static readonly DependencyProperty IsOffscreenBehaviorProperty =
        DependencyProperty.RegisterAttached(
            "IsOffscreenBehavior",
            typeof(IsOffscreenBehavior),
            typeof(AutomationProperties),
            new PropertyMetadata(IsOffscreenBehavior.Default));
}

public static class SelectionItemPatternIdentifiers
{
    public static AutomationProperty IsSelectedProperty { get; } = new();
}
