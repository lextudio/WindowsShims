using System.Runtime.CompilerServices;

namespace System.Windows;

/// <summary>
/// Extends Microsoft.UI.Xaml.DependencyObject with WPF-specific APIs that have no WinUI equivalent.
/// Handler storage uses ConditionalWeakTable so instances are not kept alive by this class.
/// </summary>
public static class WinUIDependencyObjectExtensions
{
    private static readonly ConditionalWeakTable<Microsoft.UI.Xaml.DependencyObject, HandlerBag> _handlerBags = new();
    private static readonly ConditionalWeakTable<Microsoft.UI.Xaml.DependencyObject, MouseState> _mouseState = new();

    extension(Microsoft.UI.Xaml.DependencyObject self)
    {
        // ── Dispatcher ────────────────────────────────────────────────
        public Dispatcher Dispatcher => Dispatcher.CurrentDispatcher;

        // ── Thread-access check (no-op: WinUI enforces this internally) ──
        public void VerifyAccess() { }

        public bool CheckAccess() => true;

        // WPF SetCurrentValue writes without changing the value source; Uno
        // has no such layer, so it degrades to a local SetValue.
        public void SetCurrentValueInternal(Microsoft.UI.Xaml.DependencyProperty dp, object? value)
            => self.SetValue(dp, value);

        // ── Coerce (no property-engine coercion in this bridge) ───────
        public void CoerceValue(Microsoft.UI.Xaml.DependencyProperty property) { }

        // WPF DependencyObject.SetValue accepting a DependencyPropertyKey (read-only DP write path).
        public void SetValue(System.Windows.DependencyPropertyKey key, object? value)
            => self.SetValue(key.DependencyProperty, value);

        // WPF DependencyObject.ClearValue accepting a DependencyPropertyKey.
        public void ClearValue(System.Windows.DependencyPropertyKey key)
            => self.ClearValue(key.DependencyProperty);

        public LocalValueEnumerator GetLocalValueEnumerator() => new();

        // WPF DependencyObject.GetValueSource — we only differentiate "Default"
        // from "Local" via ReadLocalValue. Inheritance and styles aren't tracked.
        public BaseValueSourceInternal GetValueSource(
            Microsoft.UI.Xaml.DependencyProperty dp,
            object? metadata,
            out bool hasModifiers)
        {
            hasModifiers = false;
            var local = self.ReadLocalValue(dp);
            return local == Microsoft.UI.Xaml.DependencyProperty.UnsetValue
                ? BaseValueSourceInternal.Default
                : BaseValueSourceInternal.Local;
        }

        // ── WPF-style routed event dispatch ───────────────────────────
        public void AddHandler(RoutedEvent routedEvent, Delegate handler)
        {
            var bag = _handlerBags.GetOrCreateValue(self);
            bag.Add(routedEvent, handler);
        }

        public void RemoveHandler(RoutedEvent routedEvent, Delegate handler)
        {
            if (_handlerBags.TryGetValue(self, out var bag))
                bag.Remove(routedEvent, handler);
        }

        public void RaiseEvent(RoutedEventArgs args)
        {
            if (args.RoutedEvent is null) return;
            if (_handlerBags.TryGetValue(self, out var bag))
                bag.Raise(self, args);
        }

        // ── Focus (no-op for content elements; UIElement overrides) ──
        public bool Focus() => true;

        // ── Mouse capture helpers (for content elements only) ─────────
        public bool IsMouseCaptured
        {
            get => _mouseState.TryGetValue(self, out var s) && s.IsMouseCaptured;
        }

        public bool IsMouseOver
        {
            get => _mouseState.TryGetValue(self, out var s) && s.IsMouseOver;
            set => _mouseState.GetOrCreateValue(self).IsMouseOver = value;
        }

        public void CaptureMouse() =>
            _mouseState.GetOrCreateValue(self).IsMouseCaptured = true;

        public void ReleaseMouseCapture() =>
            _mouseState.GetOrCreateValue(self).IsMouseCaptured = false;
    }

    // ── Private storage types ─────────────────────────────────────────

    private sealed class HandlerBag
    {
        private readonly Dictionary<RoutedEvent, List<Delegate>> _map = new();

        public void Add(RoutedEvent evt, Delegate handler)
        {
            if (!_map.TryGetValue(evt, out var list))
                _map[evt] = list = new List<Delegate>();
            list.Add(handler);
        }

        public void Remove(RoutedEvent evt, Delegate handler)
        {
            if (_map.TryGetValue(evt, out var list))
                list.Remove(handler);
        }

        public void Raise(object sender, RoutedEventArgs args)
        {
            if (!_map.TryGetValue(args.RoutedEvent!, out var list)) return;
            foreach (var d in list.ToArray())
                d.DynamicInvoke(sender, args);
        }
    }

    private sealed class MouseState
    {
        public bool IsMouseCaptured;
        public bool IsMouseOver;
    }
}
