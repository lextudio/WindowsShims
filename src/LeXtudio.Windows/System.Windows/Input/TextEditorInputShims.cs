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
        public static Cursor Arrow { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
        public static Cursor IBeam { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.IBeam);
        public static Cursor Wait { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.Wait);
        public static Cursor Cross { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.Cross);
        public static Cursor Hand { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        public static Cursor Help { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.Help);
        public static Cursor No { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.UniversalNo);
        public static Cursor AppStarting { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.Wait);
        public static Cursor Pen { get; } = new();
        public static Cursor SizeAll { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.SizeAll);
        public static Cursor SizeNESW { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.SizeNortheastSouthwest);
        public static Cursor SizeNS { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth);
        public static Cursor SizeNWSE { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.SizeNorthwestSoutheast);
        public static Cursor SizeWE { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);
        public static Cursor UpArrow { get; } = new(Microsoft.UI.Input.InputSystemCursorShape.UpArrow);
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
