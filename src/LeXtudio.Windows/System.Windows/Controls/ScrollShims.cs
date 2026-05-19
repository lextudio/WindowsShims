#if WINUI_BRIDGE
namespace System.Windows.Controls
{
    // IScrollInfo is used by TextEditorSelection for horizontal-offset queries.
    public interface IScrollInfo
    {
        double HorizontalOffset { get; }
        double VerticalOffset { get; }
        double ViewportWidth { get; }
        double ViewportHeight { get; }
        double ExtentWidth { get; }
        double ExtentHeight { get; }
    }

    // ScrollBar.PageDownCommand / PageUpCommand used by TextEditorSelection page-key handling.
    public static class ScrollBar
    {
        public static readonly System.Windows.Input.RoutedCommand PageDownCommand =
            new("PageDown", typeof(ScrollBar));
        public static readonly System.Windows.Input.RoutedCommand PageUpCommand =
            new("PageUp", typeof(ScrollBar));
        public static readonly System.Windows.Input.RoutedCommand LineDownCommand =
            new("LineDown", typeof(ScrollBar));
        public static readonly System.Windows.Input.RoutedCommand LineUpCommand =
            new("LineUp", typeof(ScrollBar));
    }
}

namespace System.Windows.Documents
{
    // Marker type; TextEditorSelection checks `textview is TextBoxView` to detect plain TextBox views.
    internal class TextBoxView
    {
    }
}
#endif
