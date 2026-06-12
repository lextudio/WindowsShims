namespace System.Windows.Automation
{
    /// <summary>Identity stub for automation property identifiers.</summary>
    public class AutomationProperty
    {
    }

    public static class ValuePatternIdentifiers
    {
        public static AutomationProperty ValueProperty { get; } = new();
    }
}

namespace System.Windows.Automation.Peers
{
    // Automation peers are not bridged on Uno: no listeners ever exist, no
    // peers are created, and raise calls are no-ops. WPF callers gate their
    // automation work on ListenerExists/FromElement, so these stubs make
    // those paths honestly unreachable.
    public class AutomationPeer
    {
        public static bool ListenerExists(AutomationEvents events) => false;

        public static AutomationPeer? FromElement(Microsoft.UI.Xaml.UIElement element) => null;

        public virtual void RaiseAutomationEvent(AutomationEvents events)
        {
        }

        public void RaisePropertyChangedEvent(AutomationProperty property, object? oldValue, object? newValue)
        {
        }
    }

    public class UIElementAutomationPeer : AutomationPeer
    {
        public static new AutomationPeer? FromElement(Microsoft.UI.Xaml.UIElement element) => null;
    }
}
