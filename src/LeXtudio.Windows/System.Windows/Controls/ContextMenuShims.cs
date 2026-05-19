namespace System.Windows.Controls.Primitives
{
    public enum PlacementMode
    {
        Absolute,
        Relative,
        Bottom,
        Center,
        Right,
        AbsolutePoint,
        RelativePoint,
        Mouse,
        MousePoint,
        Left,
        Top,
        Custom,
    }
}

namespace System.Windows.Controls
{
    public delegate void ContextMenuEventHandler(object sender, ContextMenuEventArgs e);

    public class ContextMenuEventArgs : System.Windows.RoutedEventArgs
    {
        public Microsoft.UI.Xaml.UIElement? TargetElement { get; init; }
        public double HorizontalOffset { get; set; }
        public double VerticalOffset { get; set; }
        public bool Handled { get; set; }
        public double CursorLeft { get; set; }
    }

    public class ContextMenu : ItemsControl
    {
        public bool IsOpen { get; set; }
        public System.Windows.Controls.Primitives.PlacementMode Placement { get; set; }
        public Microsoft.UI.Xaml.UIElement? PlacementTarget { get; set; }
        public double HorizontalOffset { get; set; }
        public double VerticalOffset { get; set; }
        public event System.Windows.RoutedEventHandler? Closed;
        public event System.Windows.RoutedEventHandler? Opened;
        protected virtual void OnClosed(System.Windows.RoutedEventArgs e) => Closed?.Invoke(this, e);
        protected virtual void OnOpened(System.Windows.RoutedEventArgs e) => Opened?.Invoke(this, e);
    }

    public class MenuItem : ItemsControl
    {
        public object? Command { get; set; }
        public object? CommandParameter { get; set; }
        public Microsoft.UI.Xaml.UIElement? CommandTarget { get; set; }
        public object? Header { get; set; }
        public string? InputGestureText { get; set; }
        public bool IsCheckable { get; set; }
        public bool IsChecked { get; set; }
        public new bool IsEnabled { get; set; } = true;
        public event System.Windows.RoutedEventHandler? Click;

        internal virtual void OnClickCore(bool userInitiated) => Click?.Invoke(this, new System.Windows.RoutedEventArgs());
        internal virtual void OnClickImpl(bool userInitiated) { }
    }

    public class Separator : ItemsControl
    {
    }

    public class ItemsControl : Microsoft.UI.Xaml.FrameworkElement
    {
        public ItemCollection Items { get; } = new ItemCollection();
        public void SetResourceReference(DependencyProperty dp, object resourceKey) { }
    }

    public class ItemCollection : System.Collections.Generic.List<object>
    {
    }

    public static class ContextMenuService
    {
        public static ContextMenu? GetContextMenu(Microsoft.UI.Xaml.DependencyObject element) => null;
        public static void SetContextMenu(Microsoft.UI.Xaml.DependencyObject element, ContextMenu? value) { }
    }

    public delegate void ScrollChangedEventHandler(object sender, ScrollChangedEventArgs e);

    public class ScrollChangedEventArgs : System.Windows.RoutedEventArgs
    {
        public double HorizontalChange { get; init; }
        public double VerticalChange { get; init; }
        public double HorizontalOffset { get; init; }
        public double VerticalOffset { get; init; }
        public object? OriginalSource { get; init; }
    }
}

namespace System.Windows.Controls
{
    // Extension members for WinUI's ScrollViewer to provide WPF-compatible statics.
    public static class ScrollViewerWpfExtensions
    {
        private static readonly System.Windows.RoutedEvent s_scrollChangedEvent = new();

        extension(Microsoft.UI.Xaml.Controls.ScrollViewer)
        {
            public static System.Windows.RoutedEvent ScrollChangedEvent => s_scrollChangedEvent;
            public static double _scrollLineDelta => 16.0;
        }
    }
}
