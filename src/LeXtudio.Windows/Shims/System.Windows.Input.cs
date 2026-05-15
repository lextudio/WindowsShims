namespace System.Windows.Input
{
    public class CanExecuteChangedEventManager
    {
        public static void AddHandler(ICommand command, EventHandler handler) { }
        public static void RemoveHandler(ICommand command, EventHandler handler) { }
    }

    public class MouseEventArgs : EventArgs
    {
        public bool Handled { get; set; }
        public bool UserInitiated { get; set; } = true;
    }

    public class MouseButtonEventArgs : MouseEventArgs
    {
        public int ClickCount { get; set; }
        public MouseButtonState ButtonState { get; set; } = MouseButtonState.Released;
    }

    public class QueryCursorEventArgs : MouseEventArgs
    {
        public new bool Handled { get; set; }
        public object? Cursor { get; set; }
    }

    public delegate void QueryCursorEventHandler(object sender, QueryCursorEventArgs e);
    public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);
    public delegate void MouseEventHandler(object sender, MouseEventArgs e);
    public delegate void KeyEventHandler(object sender, System.Windows.KeyEventArgs e);

    public static class Mouse
    {
        public static readonly System.Windows.RoutedEvent QueryCursorEvent = new();
        public static readonly System.Windows.RoutedEvent PreviewMouseLeftButtonDownEvent = new();
        public static readonly System.Windows.RoutedEvent MouseLeftButtonDownEvent = new();

        public static void Capture(IInputElement element)
        {
            if (element is System.Windows.DependencyObject dependencyObject)
            {
                dependencyObject.CaptureMouse();
            }
        }
    }

    public interface IInputElement
    {
        bool Focus();
        void RaiseEvent(System.Windows.RoutedEventArgs e);
        void AddHandler(System.Windows.RoutedEvent routedEvent, Delegate handler);
        void RemoveHandler(System.Windows.RoutedEvent routedEvent, Delegate handler);
        bool IsMouseCaptured { get; }
        bool IsMouseOver { get; }
        void ReleaseMouseCapture();
    }

    public enum MouseButtonState
    {
        Released,
        Pressed
    }

    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        Control = 1,
        Shift = 2
    }

    public static class Keyboard
    {
        public static ModifierKeys Modifiers => ModifierKeys.None;
    }

    public enum Key
    {
        Enter
    }
}

namespace System.Windows
{
    public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

    public class RoutedEventArgs : EventArgs
    {
        public RoutedEventArgs()
        {
        }

        public RoutedEventArgs(RoutedEvent routedEvent, object? source = null)
        {
            RoutedEvent = routedEvent;
            Source = source;
        }

        public RoutedEvent? RoutedEvent { get; set; }
        public object? Source { get; set; }
    }

    public class RoutedEvent
    {
        public RoutedEvent AddOwner(Type ownerType) => this;
    }

    public interface IUriContext
    {
        Uri? BaseUri { get; set; }
    }

    public static class EventManager
    {
        public static void RegisterClassHandler(Type classType, RoutedEvent routedEvent, Delegate handler) { }
        public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, Type handlerType, Type ownerType) => new();
    }

    public enum RoutingStrategy
    {
        Bubble,
        Tunnel,
        Direct
    }

    namespace Controls
    {
        public interface ICommandSource
        {
        }
    }

    namespace Navigation
    {
        public class RequestNavigateEventArgs : RoutedEventArgs
        {
            public RequestNavigateEventArgs()
            {
            }

            public RequestNavigateEventArgs(Uri? uri, string? target)
            {
                Uri = uri;
                Target = target;
            }

            public Uri? Uri { get; set; }
            public object? Target { get; set; }
            public bool Handled { get; set; }
        }

        public delegate void RequestNavigateEventHandler(object sender, RequestNavigateEventArgs e);
    }
}

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Field)]
    public class CustomCategoryAttribute : Attribute
    {
        public CustomCategoryAttribute(string category) { }
    }
}

namespace System.Windows.Markup
{
    public enum LocalizationCategory
    {
        None,
        Hyperlink,
        NeverLocalize
    }

    public enum Modifiability
    {
        Modifiable,
        Unmodifiable
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class LocalizabilityAttribute : Attribute
    {
        public LocalizabilityAttribute(LocalizationCategory category) { }

        public Modifiability Modifiability { get; set; }
    }
}

namespace System.Windows.Automation.Peers
{
    public enum AutomationEvents
    {
        InvokePatternOnInvoked
    }

    public class AutomationPeer
    {
        public static bool ListenerExists(AutomationEvents events) => false;

        public virtual void RaiseAutomationEvent(AutomationEvents events)
        {
        }
    }

    public static class ContentElementAutomationPeer
    {
        public static AutomationPeer? CreatePeerForElement(object element) => null;
    }

    public sealed class HyperlinkAutomationPeer(System.Windows.Documents.Hyperlink owner) : AutomationPeer
    {
    }
}

namespace System.Windows.Shapes
{
    public class Path : System.Windows.DependencyObject
    {
    }

    public class Glyphs : System.Windows.DependencyObject
    {
    }
}

namespace System.Windows.Threading
{
}

namespace MS.Internal.AppModel
{
}

namespace MS.Internal.PresentationFramework
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CommonDependencyPropertyAttribute : Attribute
    {
    }
}

namespace System.Windows
{
    public class DependencyObjectType
    {
        public static DependencyObjectType FromSystemTypeInternal(Type systemType) => new();
    }

    public class KeyEventArgs : EventArgs
    {
        public bool Handled { get; set; }
        public System.Windows.Input.Key Key { get; set; }
        public bool UserInitiated { get; set; } = true;
    }
}
