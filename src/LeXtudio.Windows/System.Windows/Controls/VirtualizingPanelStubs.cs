namespace System.Windows.Controls;

// ScrollUnit: controls whether scroll offsets are in pixels or logical items.
public enum ScrollUnit
{
    Pixel = 0,
    Item  = 1,
}

// VirtualizingPanel: abstract base for WPF virtualizing panels. Extends our
// Panel shim so casts like `panel as VirtualizingStackPanel` compile and work.
public abstract class VirtualizingPanel : Panel
{
    public static readonly DependencyProperty IsVirtualizingProperty =
        DependencyProperty.RegisterAttached("IsVirtualizing", typeof(bool),
            typeof(VirtualizingPanel), new PropertyMetadata(true));

    public static readonly DependencyProperty VirtualizationModeProperty =
        DependencyProperty.RegisterAttached("VirtualizationMode", typeof(VirtualizationMode),
            typeof(VirtualizingPanel), new PropertyMetadata(VirtualizationMode.Standard));

    internal static readonly DependencyProperty ShouldCacheContainerSizeProperty =
        DependencyProperty.RegisterAttached(
            "ShouldCacheContainerSize",
            typeof(bool),
            typeof(VirtualizingPanel),
            new FrameworkPropertyMetadata(true));

    public static ScrollUnit GetScrollUnit(UIElement element) => ScrollUnit.Item;

    public static bool GetIsVirtualizing(DependencyObject element)
        => (bool)element.GetValue(IsVirtualizingProperty);

    public static void SetIsVirtualizing(DependencyObject element, bool value)
        => element.SetValue(IsVirtualizingProperty, value);
}

// VirtualizingStackPanel: virtualizes items in a stack layout. DataGrid casts
// its items host to this type to check scroll state; returns null in our shim.
public class VirtualizingStackPanel : VirtualizingPanel
{
    internal bool IgnoreMaxDesiredSize { get; set; }
}
