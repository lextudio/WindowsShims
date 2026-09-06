#if WINUI_BRIDGE
namespace System.Windows.Controls
{
    // IScrollInfo is System.Windows.Controls.Primitives.IScrollInfo, linked from upstream
    // now that the fixed-document rung needs the real interface (with MakeVisible,
    // SetHorizontalOffset, CanVerticallyScroll and friends). TextEditorSelection's
    // `textview as IScrollInfo` casts resolve to it via `using System.Windows.Controls.Primitives;`.

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

        // ValidateValueCallback used by ToolBarTray.OrientationProperty
        public static bool IsValidOrientation(object o)
        {
            if (o is Orientation value)
                return value == Orientation.Horizontal || value == Orientation.Vertical;
            return false;
        }
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
