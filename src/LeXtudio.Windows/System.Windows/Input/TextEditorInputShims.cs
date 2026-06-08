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

    public sealed class PopupControlService
    {
        public static PopupControlService Current { get; } = new();
        public void RaiseToolTipOpeningEvent(Microsoft.UI.Xaml.DependencyObject o) { }
        public void RaiseToolTipClosingEvent(Microsoft.UI.Xaml.DependencyObject o) { }
        internal void DismissToolTipsForOwner(Microsoft.UI.Xaml.DependencyObject o) { }
    }
}

