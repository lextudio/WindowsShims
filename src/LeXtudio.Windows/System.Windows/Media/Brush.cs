// Make Brush available in System.Windows.Media namespace as an alias to Microsoft.UI.Xaml.Media.Brush
// This satisfies "using Brush = System.Windows.Media.Brush;" in WPF source files

namespace System.Windows.Media
{
    // Create Brush as a simple type alias using inheritance
    // This makes System.Windows.Media.Brush resolve to Microsoft.UI.Xaml.Media.Brush
    public abstract class Brush : Microsoft.UI.Xaml.Media.Brush { }
}
