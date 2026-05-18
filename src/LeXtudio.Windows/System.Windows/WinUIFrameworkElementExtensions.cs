namespace System.Windows;

public static class WinUIFrameworkElementExtensions
{
    extension(Microsoft.UI.Xaml.FrameworkElement self)
    {
        public bool IsEnabled => true;

        public bool IsFocused => false;

        public bool IsKeyboardFocused => false;

        // WPF property consulted while a logical-tree walk is in progress.
        // We don't model the walk, so it's always false; callers gate stale-tree
        // guards behind this and the false return reads as "not in a walk".
        public bool IsLogicalChildrenIterationInProgress => false;

        // WPF's FrameworkElement.TemplatedParent is public; WinUI/Uno keeps it
        // internal. Upstream WPF source files (e.g. TextSelection.GetParentElement)
        // read it as a parent-walk shortcut and already fall back to
        // VisualTreeHelper.GetParent when null, so returning null here keeps
        // the walk semantics intact without reflecting into Uno internals.
        public DependencyObject? TemplatedParent => null;
    }

    extension(Microsoft.UI.Xaml.UIElement self)
    {
        public bool IsKeyboardFocusWithin => false;
    }

    // BitmapEffectProperty is obsolete in WinUI; backing field + static extension so
    // upstream code can reference UIElement.BitmapEffectProperty.
    private static readonly DependencyProperty s_bitmapEffectProperty =
        DependencyProperty.Register(
            "BitmapEffect",
            typeof(object),
            typeof(Microsoft.UI.Xaml.UIElement),
            new Microsoft.UI.Xaml.PropertyMetadata(null));

    extension(Microsoft.UI.Xaml.UIElement)
    {
        public static DependencyProperty BitmapEffectProperty => s_bitmapEffectProperty;
    }
}
