// Shim types referenced by upstream TextEditor.cs and its sibling helpers.
// All UI-event delivery is left as no-op — TextEditor's command routing isn't
// wired into a real Uno input pipeline yet; these exist to satisfy compilation
// and let the TextEditor controller participate in the upstream type surface.

namespace System.Windows.Input
{
    public delegate void CanExecuteRoutedEventHandler(object sender, CanExecuteRoutedEventArgs e);

    public delegate void ExecutedRoutedEventHandler(object sender, ExecutedRoutedEventArgs e);

    public delegate void KeyboardFocusChangedEventHandler(object sender, KeyboardFocusChangedEventArgs e);

    public static class Cursors
    {
        // Existing `Cursor` shim (defined in EarlyBatchEditorShims) is a marker
        // class with no real implementation; we just return new instances.
        public static Cursor Arrow { get; } = new();
        public static Cursor IBeam { get; } = new();
        public static Cursor Wait { get; } = new();
        public static Cursor Cross { get; } = new();
        public static Cursor Hand { get; } = new();
        public static Cursor Help { get; } = new();
        public static Cursor No { get; } = new();
        public static Cursor AppStarting { get; } = new();
        public static Cursor Pen { get; } = new();
        public static Cursor SizeAll { get; } = new();
        public static Cursor SizeNESW { get; } = new();
        public static Cursor SizeNS { get; } = new();
        public static Cursor SizeNWSE { get; } = new();
        public static Cursor SizeWE { get; } = new();
        public static Cursor UpArrow { get; } = new();
        public static Cursor None { get; } = new();
        public static Cursor ScrollNS { get; } = new();
        public static Cursor ScrollWE { get; } = new();
        public static Cursor ScrollAll { get; } = new();
        public static Cursor ScrollN { get; } = new();
        public static Cursor ScrollS { get; } = new();
        public static Cursor ScrollE { get; } = new();
        public static Cursor ScrollW { get; } = new();
        public static Cursor ScrollNE { get; } = new();
        public static Cursor ScrollNW { get; } = new();
        public static Cursor ScrollSE { get; } = new();
        public static Cursor ScrollSW { get; } = new();
        public static Cursor ArrowCD { get; } = new();
    }

    // Non-static so the selector spine can use the WPF KeyboardNavigation.Current
    // instance surface; static members keep their existing call sites.
    public sealed class KeyboardNavigation
    {
        private KeyboardNavigation()
        {
        }

        public static readonly Microsoft.UI.Xaml.DependencyProperty TabNavigationProperty =
            Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                "TabNavigation", typeof(int), typeof(KeyboardNavigation),
                new Microsoft.UI.Xaml.PropertyMetadata(0));

        public static readonly Microsoft.UI.Xaml.DependencyProperty AcceptsReturnProperty =
            Microsoft.UI.Xaml.DependencyProperty.RegisterAttached(
                "AcceptsReturn", typeof(bool), typeof(KeyboardNavigation),
                new Microsoft.UI.Xaml.PropertyMetadata(false));

        internal static KeyboardNavigation Current { get; } = new();

        // Focus-scope tracking is not wired on Uno; subscribers never fire.
        internal event EventHandler? FocusEnterMainFocusScope
        {
            add { }
            remove { }
        }

        internal void UpdateActiveElement(
            Microsoft.UI.Xaml.DependencyObject scope,
            Microsoft.UI.Xaml.DependencyObject activeElement)
        {
        }

        internal static Microsoft.UI.Xaml.DependencyObject? GetVisualRoot(
            Microsoft.UI.Xaml.DependencyObject element) => null;
    }

    public sealed class PopupControlService
    {
        public static PopupControlService Current { get; } = new();
        public void RaiseToolTipOpeningEvent(Microsoft.UI.Xaml.DependencyObject o) { }
        public void RaiseToolTipClosingEvent(Microsoft.UI.Xaml.DependencyObject o) { }
        internal void DismissToolTipsForOwner(Microsoft.UI.Xaml.DependencyObject o) { }
    }
}

