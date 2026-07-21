// System-services shims needed by upstream TextEditor.cs.
// Behavior is no-op / default; this lets TextEditor compile and participate
// in the upstream type surface without wiring into a real Uno input/services
// pipeline.

using System.Runtime.InteropServices;

namespace System.Windows
{
    public static class SystemParameters
    {
        // Session 122 (follow-up): real per-OS double-click interval instead of a
        // fixed guess, queried once and cached (this isn't expected to change
        // while the process is running). Windows: user32!GetDoubleClickTime
        // (milliseconds, directly). macOS: AppKit's NSEvent.doubleClickInterval —
        // there's no C API for this, only the Objective-C class property, so it's
        // read via the Objective-C runtime directly (objc_getClass/sel_registerName
        // + objc_msgSend), the same mechanism any Objective-C caller uses; no
        // AppKit binding library needed for one property read. x86_64's ABI
        // requires the floating-point-return entry point (objc_msgSend_fpret) for
        // a function returning a double; arm64's unified calling convention means
        // the ordinary objc_msgSend works for every return shape, so which symbol
        // gets bound depends on process architecture. Both DllImports below are
        // declared unconditionally (fine on every OS — they only resolve, and
        // fail, if actually invoked) and only called behind a runtime OS check;
        // any failure (missing library, unexpected native error) falls back to
        // the previous fixed 500ms guess rather than throwing.
        private static readonly int _doubleClickTime = QueryDoubleClickTime();

        private static int QueryDoubleClickTime()
        {
            const int fallbackMs = 500;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var ms = (int)NativeMethods.GetDoubleClickTime();
                    return ms > 0 ? ms : fallbackMs;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // AppKit isn't guaranteed loaded into every process that links
                    // this shim (verified live: objc_getClass("NSEvent") returns
                    // NULL, and the property read silently comes back 0, without
                    // this) — load it explicitly first. Cheap and idempotent if
                    // already loaded (e.g. the real Uno/Skia desktop app, which
                    // already uses AppKit for native window chrome).
                    ObjC.dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", ObjC.RTLD_LAZY);
                    var nsEventClass = ObjC.objc_getClass("NSEvent");
                    var selector = ObjC.sel_registerName("doubleClickInterval");
                    double seconds = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                        ? ObjC.objc_msgSend_double(nsEventClass, selector)
                        : ObjC.objc_msgSend_fpret(nsEventClass, selector);
                    var ms = (int)Math.Round(seconds * 1000.0);
                    return ms > 0 ? ms : fallbackMs;
                }
            }
            catch
            {
                // Native call unavailable or failed for any reason — keep the guess.
            }

            return fallbackMs;
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern uint GetDoubleClickTime();
        }

        private static class ObjC
        {
            public const int RTLD_LAZY = 1;

            [DllImport("/usr/lib/system/libdyld.dylib")]
            public static extern IntPtr dlopen(string path, int mode);

            [DllImport("/usr/lib/libobjc.dylib")]
            public static extern IntPtr objc_getClass(string name);

            [DllImport("/usr/lib/libobjc.dylib")]
            public static extern IntPtr sel_registerName(string name);

            // arm64: the unified objc_msgSend entry point returns a double correctly
            // for a no-argument class-property getter like this one.
            [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
            public static extern double objc_msgSend_double(IntPtr receiver, IntPtr selector);

            // x86_64: floating-point returns must go through the _fpret variant.
            [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_fpret")]
            public static extern double objc_msgSend_fpret(IntPtr receiver, IntPtr selector);
        }

        public static int DoubleClickTime => _doubleClickTime;
        public static double DoubleClickDeltaX => 4.0;
        public static double DoubleClickDeltaY => 4.0;
        public static int MouseHoverTime => 400;
        public static double MouseHoverWidth => 4.0;
        public static double MouseHoverHeight => 4.0;
        public static double CaretWidth => 1.0;
        public static int CaretBlinkTime => 530;
        public static bool MouseWheelTextSelectionEnabled => true;
        public static bool MouseHoverTimeEnabled => true;
        public static int MenuShowDelay => 400;
        public static bool MouseVanish => false;
        public static double MinimumHorizontalDragDistance => 4.0;
        public static double MinimumVerticalDragDistance => 4.0;

        // Screen-metric surface used by AvalonDock floating-window placement.
        // WPF reads these from Win32; on Skia we return sensible defaults (the
        // real placement on macOS is handled by the NSWindow path in UnoDock).
        public static double PrimaryScreenWidth => 1920.0;
        public static double PrimaryScreenHeight => 1080.0;
        public static double VirtualScreenLeft => 0.0;
        public static double VirtualScreenTop => 0.0;
        public static double VirtualScreenWidth => 1920.0;
        public static double VirtualScreenHeight => 1080.0;
    }

    public static class SafeSystemMetrics
    {
        public static int VirtualScreenWidth => 1920;
        public static int VirtualScreenHeight => 1080;
        public static bool IsImmEnabled => false;
    }
}

namespace System.Windows.Markup
{
    // Stub: WPF XAML type-mapping infrastructure; passed through TextRangeSerialization but never called.
    public class XamlTypeMapper
    {
        public static readonly XamlTypeMapper Default = new XamlTypeMapper();
    }

    // Stub: provides the default XamlTypeMapper for XAML serialization.
    public static class XmlParserDefaults
    {
        public static XamlTypeMapper DefaultMapper { get; } = XamlTypeMapper.Default;
    }

    public sealed class XmlLanguage
    {
        public string IetfLanguageTag { get; }

        private XmlLanguage(string lang) { IetfLanguageTag = lang ?? string.Empty; }

        public static XmlLanguage GetLanguage(string ietfLanguageTag)
            => new XmlLanguage(ietfLanguageTag);

        public static XmlLanguage Empty { get; } = new XmlLanguage(string.Empty);

        public System.Globalization.CultureInfo GetSpecificCulture()
        {
            try { return System.Globalization.CultureInfo.GetCultureInfo(IetfLanguageTag); }
            catch { return System.Globalization.CultureInfo.InvariantCulture; }
        }

        public override string ToString() => IetfLanguageTag;
    }
}

namespace MS.Internal.Documents
{
    // Stub TSF host: text-services framework integration isn't modeled.
    internal sealed class TextServicesHost
    {
        public static TextServicesHost? Current => null;

        public void RegisterWinEventSink(System.Windows.Documents.TextStore? textStore) { }
        public void UnregisterWinEventSink(System.Windows.Documents.TextStore? textStore) { }
    }

    internal static class TextServicesLoader
    {
        public static bool ServicesInstalled => false;
        internal static MS.Win32.UnsafeNativeMethods.ITfThreadMgr? Load() => null;
    }
}

namespace MS.Win32
{
    internal static partial class UnsafeNativeMethods
    {
        // Marker interface only; no methods. TextEditor references this type
        // for TSF (ITfThreadMgr) but never dispatches through it in the shim.
        public interface ITfThreadMgr
        {
        }

        // Stub for TSF candidate list used by TextEditorContextMenu reconversion.
        public interface ITfCandidateList
        {
        }
    }
}

// Stub namespace; TextEditorContextMenu.cs and TextEditorTyping.cs import these.
namespace System.Windows.Interop
{
    public interface IWin32Window
    {
        IntPtr Handle { get; }
    }

    public class WindowInteropHelper
    {
        public WindowInteropHelper(Window window) { }
        public IntPtr Handle => IntPtr.Zero;
    }
}

namespace MS.Internal.Interop
{
}

namespace System.Windows.Media.Media3D
{
    // Minimal stub; TextEditorMouse uses Visual3D in a hit-test type-check guard.
    public abstract partial class Visual3D
#if WINDOWS_APP_SDK
        : Microsoft.UI.Xaml.DependencyObject
#endif
    {
        public bool IsDescendantOf(object ancestor) => false;
    }
}

namespace MS.Internal.Documents
{
    // Stub for WPF's TextDocumentView — used only for a type-check in
    // TextRangeEditTables.TableBorderHitTest; on HAS_UNO the check always
    // returns false so no real implementation is needed.
    internal sealed class TextDocumentView
    {
        internal MS.Internal.PtsHost.CellInfo? GetCellInfoFromPoint(Windows.Foundation.Point point, System.Windows.Documents.Table? tableFilter) => null;
    }
}

namespace MS.Internal.PtsHost
{
    // Stub for WPF's CellInfo — fields accessed only when TextDocumentView
    // is the active ITextView, which never happens on HAS_UNO.
    internal sealed class CellInfo
    {
        internal Windows.Foundation.Rect TableArea => default;
        internal Windows.Foundation.Rect CellArea => default;
        internal System.Windows.Documents.TableCell? Cell => null;
        internal double TableAutofitWidth => 0;
        internal double[]? TableColumnWidths => null;
    }
}

// ColumnResizeAdorner stub removed in Session 25; upstream ColumnResizeAdorner.cs is now active.
