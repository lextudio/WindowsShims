// Minimal NavigationService surface for the fixed-document (XPS) family.
//
// Upstream NavigationService.cs is 4400+ lines of app-model code (journal, PageFunction,
// WebBrowser hosting, BindStream) and cannot be linked into a control library, so only the
// pieces the linked fixed-document sources need are provided here: the attached
// NavigationService property FixedDocument overrides metadata on, the FragmentNavigation
// event FixedHyperLink subscribes to, and Content/CurrentSource.
// FragmentNavigationEventArgs itself IS linked from upstream.

namespace System.Windows.Navigation
{
    public delegate void FragmentNavigationEventHandler(object sender, FragmentNavigationEventArgs e);

    /// <summary>
    /// Compatibility stand-in for WPF's NavigationService. No navigation is performed;
    /// the type exists so fixed-document content can register fragment handling.
    /// </summary>
    public partial class NavigationService : Microsoft.UI.Xaml.DependencyObject
    {
        public static readonly Microsoft.UI.Xaml.DependencyProperty NavigationServiceProperty =
            Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                "NavigationService",
                typeof(NavigationService),
                typeof(NavigationService),
                new System.Windows.FrameworkPropertyMetadata(null));

        public static NavigationService? GetNavigationService(Microsoft.UI.Xaml.DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (NavigationService?)element.GetValue(NavigationServiceProperty);
        }

        public static void SetNavigationService(Microsoft.UI.Xaml.DependencyObject element, NavigationService? value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(NavigationServiceProperty, value);
        }

        /// <summary>Raised when a navigation targets a fragment (#name) of the current content.</summary>
        public event FragmentNavigationEventHandler? FragmentNavigation;

        /// <summary>The object currently hosted by this navigator.</summary>
        public object? Content { get; set; }

        /// <summary>Uri of the content currently loaded, used to resolve relative uris.</summary>
        public Uri? CurrentSource { get; set; }

    }
}
