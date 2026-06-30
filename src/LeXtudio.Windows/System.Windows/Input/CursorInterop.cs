using System.Reflection;

namespace System.Windows.Input;

internal static class CursorInterop
{
    private static readonly PropertyInfo? ProtectedCursorProperty =
        typeof(Microsoft.UI.Xaml.UIElement).GetProperty(
            "ProtectedCursor",
            BindingFlags.Instance | BindingFlags.NonPublic);

    internal static void SetCursor(Microsoft.UI.Xaml.UIElement element, Cursor? cursor)
    {
        var inputCursor = cursor?.SystemCursorShape is { } shape
            ? Microsoft.UI.Input.InputSystemCursor.Create(shape)
            : null;
        ProtectedCursorProperty?.SetValue(element, inputCursor);
    }
}
