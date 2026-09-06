// Support shims for the linked upstream fixed-document (XPS) sources.
//
// These namespaces live in WPF assemblies that LeXtudio.Windows does not carry
// (PresentationFramework's app-model layer and the ETW tracing layer). Only the
// members the linked fixed-document files actually call are provided.

namespace MS.Utility
{
    /// <summary>
    /// No-op stand-in for WPF's ETW tracing helper. The linked fixed-document sources only
    /// emit begin/end markers through it; there is no cross-platform ETW provider to target.
    /// </summary>
    internal static class EventTrace
    {
        internal enum Keyword
        {
            KeywordGeneral = 0,
            KeywordXPS = 1,
        }

        internal enum Event
        {
            WClientDRXGetPageBegin,
            WClientDRXGetPageEnd,
            WClientDRXEnsureOMBegin,
            WClientDRXEnsureOMEnd,
        }

        internal static bool IsEnabled(Keyword keyword, object? level = null) => false;

        internal static void EasyTraceEvent(Keyword keyword, Event eventId)
        {
            // Intentionally empty: tracing is not wired on non-Windows targets.
        }
    }
}
