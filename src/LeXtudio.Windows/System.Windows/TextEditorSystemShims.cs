// System-services shims needed by upstream TextEditor.cs.
// Behavior is no-op / default; this lets TextEditor compile and participate
// in the upstream type surface without wiring into a real Uno input/services
// pipeline.

namespace System.Windows
{
    public static class SystemParameters
    {
        public static int DoubleClickTime => 500;
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
