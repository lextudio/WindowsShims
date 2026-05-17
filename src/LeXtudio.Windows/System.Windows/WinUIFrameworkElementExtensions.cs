namespace System.Windows;

public static class WinUIFrameworkElementExtensions
{
    extension(Microsoft.UI.Xaml.FrameworkElement self)
    {
        public bool IsEnabled => true;

        public bool IsFocused => false;

        public bool IsKeyboardFocused => false;
    }

    extension(Microsoft.UI.Xaml.UIElement self)
    {
        public bool IsKeyboardFocusWithin => false;
    }
}
