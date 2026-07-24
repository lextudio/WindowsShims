#if DEBUG
using System.Runtime.InteropServices;

namespace DataGrid.IntegrationTestHost;

internal static class MacOSWindowHelper
{
    private const string ObjC = "/System/Library/Frameworks/Foundation.framework/Foundation";
    private const string CG = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [DllImport(ObjC)] private static extern IntPtr objc_getClass(string name);
    [DllImport(ObjC)] private static extern IntPtr sel_registerName(string name);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern IntPtr msgSend(IntPtr self, IntPtr op);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern ulong msgSend_retNUInt(IntPtr self, IntPtr op);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern bool msgSend_retBool(IntPtr self, IntPtr op);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern long msgSend_nuint_retLong(IntPtr self, IntPtr op, nuint arg);
    // NOTE: on arm64 (Apple Silicon) there is NO objc_msgSend_stret — struct returns go through
    // plain objc_msgSend (the P/Invoke marshaller handles the x8 indirect-return register and the
    // by-value CGRect argument in v0-v3). Using objc_msgSend_stret here on arm64 silently returned
    // zeroed structs, which made GetWindowContentOrigin always report (0,0). This project targets
    // arm64 macOS, so objc_msgSend is correct here; x86_64 would need _stret for a 32-byte CGRect.
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern CGRect msgSend_retCGRect(IntPtr self, IntPtr op);
    [DllImport(ObjC, EntryPoint = "objc_msgSend")] private static extern CGRect msgSend_CGRect_retCGRect(IntPtr self, IntPtr op, CGRect arg);

    [DllImport(CG)] private static extern uint CGMainDisplayID();
    [DllImport(CG)] private static extern nuint CGDisplayPixelsHigh(uint display);

    private static readonly IntPtr _clsNSScreen = objc_getClass("NSScreen");
    private static readonly IntPtr _selMainScreen = sel_registerName("mainScreen");
    private static readonly IntPtr _selFrame = sel_registerName("frame");

    private struct CGPoint { public double X, Y; }
    private struct CGSize { public double Width, Height; }
    private struct CGRect { public CGPoint Origin; public CGSize Size; }

    private static readonly IntPtr _clsNSApp = objc_getClass("NSApplication");
    private static readonly IntPtr _selSharedApp = sel_registerName("sharedApplication");
    private static readonly IntPtr _selWindows = sel_registerName("windows");
    private static readonly IntPtr _selCount = sel_registerName("count");
    private static readonly IntPtr _selObjectAtIndex = sel_registerName("objectAtIndex:");
    private static readonly IntPtr _selStyleMask = sel_registerName("styleMask");
    private static readonly IntPtr _selIsVisible = sel_registerName("isVisible");
    private static readonly IntPtr _selContentView = sel_registerName("contentView");
    private static readonly IntPtr _selBounds = sel_registerName("bounds");
    private static readonly IntPtr _selConvertRectToScreen = sel_registerName("convertRectToScreen:");

    private const ulong NSWindowStyleMaskTitled = 1;

    /// <summary>Finds the app's main titled NSWindow handle.</summary>
    internal static IntPtr GetMainNSWindow()
    {
        try
        {
            var app = msgSend(_clsNSApp, _selSharedApp);
            if (app == IntPtr.Zero) return IntPtr.Zero;
            var wins = msgSend(app, _selWindows);
            if (wins == IntPtr.Zero) return IntPtr.Zero;
            var count = (long)msgSend_retNUInt(wins, _selCount);
            for (long i = 0; i < count; i++)
            {
                var w = (IntPtr)msgSend_nuint_retLong(wins, _selObjectAtIndex, (nuint)i);
                if (w == IntPtr.Zero) continue;
                var style = msgSend_retNUInt(w, _selStyleMask);
                if ((style & NSWindowStyleMaskTitled) == 0) continue;
                if (!msgSend_retBool(w, _selIsVisible)) continue;
                return w;
            }
        }
        catch { }
        return IntPtr.Zero;
    }

    /// <summary>Returns (originX, originY) of the window's CONTENT area in Quartz global points (top-left origin, Y-down).</summary>
    internal static (double X, double Y) GetWindowContentOrigin(IntPtr nsWindow)
    {
        if (nsWindow == IntPtr.Zero) return (0, 0);
        try
        {
            var contentView = msgSend(nsWindow, _selContentView);
            if (contentView == IntPtr.Zero) return (0, 0);

            var bounds = msgSend_retCGRect(contentView, _selBounds);
            var inScreen = msgSend_CGRect_retCGRect(nsWindow, _selConvertRectToScreen, bounds);

            // Y-flip Cocoa (bottom-left origin) → Quartz (top-left origin) must use the main
            // screen height in POINTS, matching convertRectToScreen:'s point-based output.
            // CGDisplayPixelsHigh returns PIXELS, which on a Retina display is 2x the points and
            // would corrupt the flip; NSScreen.mainScreen.frame.size.height is in points.
            var mainScreen = msgSend(_clsNSScreen, _selMainScreen);
            double screenH = mainScreen != IntPtr.Zero
                ? msgSend_retCGRect(mainScreen, _selFrame).Size.Height
                : CGDisplayPixelsHigh(CGMainDisplayID());

            var quartzY = screenH - (inScreen.Origin.Y + inScreen.Size.Height);
            try { System.IO.File.AppendAllText("/tmp/datagrid-drag.log",
                $"[origin] contentBounds=({bounds.Origin.X},{bounds.Origin.Y} {bounds.Size.Width}x{bounds.Size.Height}) " +
                $"inScreen=({inScreen.Origin.X},{inScreen.Origin.Y} {inScreen.Size.Width}x{inScreen.Size.Height}) " +
                $"screenH={screenH} quartzY={quartzY}\n"); } catch { }
            return (inScreen.Origin.X, quartzY);
        }
        catch
        {
            return (0, 0);
        }
    }
}
#endif
