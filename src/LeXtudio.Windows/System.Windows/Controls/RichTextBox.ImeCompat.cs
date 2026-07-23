#if HAS_UNO
using System.Reflection;
using Microsoft.UI.Xaml;

namespace System.Windows.Controls;

// Platform-specific IME compat extensions for RichTextBox's CoreTextEditContext.
// Follows the same pattern as UnoEdit's CoreTextCompatExtensions.cs.
partial class RichTextBox
{
    private bool AttachImeToWindow(CoreTextEditContext context, Window? window)
    {
#if WINDOWS_APP_SDK
        return true;
#else
        var (windowHandle, displayHandle) = ResolveNativeWindowHandles(window);
        if (windowHandle == 0)
            return false;
        return context.AttachToWindowHandle(windowHandle, displayHandle);
#endif
    }

    private bool ProcessImeKeyEvent(CoreTextEditContext context, int virtualKey, bool shift, bool ctrl)
    {
#if WINDOWS_APP_SDK
        // Windows App SDK handles IME key routing natively through its input pipeline.
        return false;
#else
        return context.ProcessKeyEvent(virtualKey, shift, ctrl);
#endif
    }

    private void NotifyImeCaretRect(CoreTextEditContext context, Rect caretRect)
    {
#if !WINDOWS_APP_SDK
        context.NotifyCaretRectChanged(caretRect.X, caretRect.Y, caretRect.Width, caretRect.Height);
#endif
    }

#if WINDOWS_APP_SDK
    private static (nint windowHandle, nint displayHandle) ResolveNativeWindowHandles(Window? window)
    {
        if (window is null)
            return (0, 0);

        try
        {
            nint hwnd = (nint)WinRT.Interop.WindowNative.GetWindowHandle(window);
            return (hwnd, 0);
        }
        catch
        {
            return (0, 0);
        }
    }
#else
    private static (nint windowHandle, nint displayHandle) ResolveNativeWindowHandles(Window? window)
    {
        if (window is null)
            return (System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle, 0);

        try
        {
            var windowHelperType = Type.GetType("Uno.UI.Xaml.WindowHelper, Uno.UI");
            var getNativeWindow = windowHelperType?.GetMethod("GetNativeWindow", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object? nativeWindow = getNativeWindow?.Invoke(null, [window]);

            if (nativeWindow is null)
                return (0, 0);

            nint handle = 0;
            foreach (var name in new[] { "Hwnd", "HWnd", "Handle", "WindowHandle", "NativeHandle", "Pointer", "hwnd", "_hwnd" })
            {
                var prop = nativeWindow.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var value = prop?.GetValue(nativeWindow);
                handle = value switch
                {
                    nint n => n,
                    long l => (nint)l,
                    int i => (nint)i,
                    _ => 0,
                };
                if (handle != 0)
                    break;
            }

            return (handle, 0);
        }
        catch
        {
            return (0, 0);
        }
    }
#endif
}
#endif
