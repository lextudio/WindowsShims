// System.Windows.Media.Brush provides the WPF-compatible API surface.
// This base class allows all WPF document code that references Brush to compile.
// Actual instances use Microsoft.UI.Xaml.Media.Brush types via inheritance.

namespace System.Windows.Media
{
    /// <summary>
    /// WPF-compatible base class for brushes.
    /// Inherits from Microsoft.UI.Xaml.Media.Brush to provide seamless interop.
    /// </summary>
    public abstract class Brush : Microsoft.UI.Xaml.Media.Brush
    {
        // No additional members needed—all brush functionality is in the WinUI base class.
        // This class exists solely for API compatibility with WPF code.
    }
}
