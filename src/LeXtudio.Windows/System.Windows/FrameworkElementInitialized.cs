// WPF's FrameworkElement.Initialized, which WinUI has no counterpart for.
//
// WPF raises Initialized from FrameworkElement.EndInit() once the parser has finished the
// element. Uno's XAML loader has no such hook, so LeXtudio.Windows reports IsInitialized
// as always true (see WinUIFrameworkElementExtensions) — an element handed back by
// XamlReader.Load is already fully parsed. Callers therefore take the already-initialized
// branch and never wait on this event. It exists so the deferred-loading code in the
// fixed-document sources compiles unchanged, and so a host that does drive a staged parse
// has somewhere to raise from.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Windows
{
    public static class FrameworkElementInitialized
    {
        private static readonly ConditionalWeakTable<object, List<EventHandler>> Handlers = new();

        public static void Add(object element, EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentNullException.ThrowIfNull(handler);

            var list = Handlers.GetOrCreateValue(element);
            lock (list)
            {
                list.Add(handler);
            }
        }

        public static void Remove(object element, EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(element);

            if (!Handlers.TryGetValue(element, out var list))
            {
                return;
            }

            lock (list)
            {
                list.Remove(handler);
            }
        }

        /// <summary>Raises the handlers registered for <paramref name="element"/> and clears them.</summary>
        public static void Raise(object element)
        {
            if (!Handlers.TryGetValue(element, out var list))
            {
                return;
            }

            EventHandler[] snapshot;
            lock (list)
            {
                snapshot = list.ToArray();
                list.Clear();
            }

            foreach (var handler in snapshot)
            {
                handler(element, EventArgs.Empty);
            }
        }
    }
}
