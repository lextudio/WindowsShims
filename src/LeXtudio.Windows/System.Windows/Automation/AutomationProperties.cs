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

    // Name/HelpText forward to WinUI's own attached properties: this shim shadows
    // Microsoft.UI.Xaml.Automation.AutomationProperties for files that have
    // `using System.Windows.Automation;`, so the WinUI members must be re-exposed.
    public static readonly DependencyProperty NameProperty =
        Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty;

    public static readonly DependencyProperty HelpTextProperty =
        Microsoft.UI.Xaml.Automation.AutomationProperties.HelpTextProperty;

    public static string GetName(DependencyObject element)
        => Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(element);

    public static void SetName(DependencyObject element, string value)
        => Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(element, value);

    public static string GetHelpText(DependencyObject element)
        => Microsoft.UI.Xaml.Automation.AutomationProperties.GetHelpText(element);

    public static void SetHelpText(DependencyObject element, string value)
        => Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(element, value);
}

public static class SelectionItemPatternIdentifiers
{
    public static AutomationProperty IsSelectedProperty { get; } =
        new(Microsoft.UI.Xaml.Automation.SelectionItemPatternIdentifiers.IsSelectedProperty);
}
