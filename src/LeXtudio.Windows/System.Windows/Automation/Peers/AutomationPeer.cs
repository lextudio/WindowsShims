using Microsoft.UI.Xaml;

namespace System.Windows.Automation
{
    /// <summary>Identity handle for a well-known automation property, mapped to its Uno/WinUI singleton.</summary>
    public class AutomationProperty
    {
        internal Microsoft.UI.Xaml.Automation.AutomationProperty? UnoProperty { get; }

        internal AutomationProperty()
        {
        }

        internal AutomationProperty(Microsoft.UI.Xaml.Automation.AutomationProperty unoProperty)
        {
            UnoProperty = unoProperty;
        }
    }

    public static class ValuePatternIdentifiers
    {
        public static AutomationProperty ValueProperty { get; } =
            new(Microsoft.UI.Xaml.Automation.ValuePatternIdentifiers.ValueProperty);
    }
}

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// WPF-shaped automation peer base that bridges onto Uno's native automation.
    /// Uno 6.6 ships a full Skia accessibility stack: peers are created per element
    /// through UIElement.OnCreateAutomationPeer, the tree is built from visual
    /// children, and events/property changes route through AutomationPeerListener
    /// to a per-window SkiaAccessibilityBase (e.g. MacOSAccessibility for VoiceOver).
    /// The WPF call sites in the linked source are gated on ListenerExists /
    /// FromElement, so this base rewiring activates them all.
    ///
    /// The base is Uno's FrameworkElementAutomationPeer (rather than the raw
    /// AutomationPeer) for two reasons: (1) SkiaAccessibilityBase.TryGetPeerOwner
    /// only resolves owners for FrameworkElementAutomationPeer instances, which is
    /// what makes native event routing work for element-backed peers; (2) the
    /// linked upstream code declares its OnCreateAutomationPeer overrides with the
    /// WPF peer return type, so every peer must stay a System.Windows...
    /// AutomationPeer (covariant override). Non-element peers (item peers) simply
    /// use the parameterless constructor and route their raises through element
    /// peers.
    ///
    /// The AutomationEvents and Microsoft.UI.Xaml.Automation.Peers.AutomationEvents
    /// enums have identical member values (both mirror UIA event IDs 0..17), so
    /// forwarding is a plain cast.
    /// </summary>
    public class AutomationPeer : Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
    {
        // Uno's FrameworkElementAutomationPeer exposes an owner-less ctor; WinAppSDK's
        // only takes an owner, so the item-peer path chains through it with no owner.
        public AutomationPeer()
#if WINDOWS_APP_SDK
            : base((Microsoft.UI.Xaml.FrameworkElement)null)
#endif
        {
        }

        public AutomationPeer(Microsoft.UI.Xaml.FrameworkElement owner)
            : base(owner)
        {
        }

        // WinAppSDK's UIA statics fail with an HRESULT when no Xaml/UIA context is up
        // (e.g. off the UI thread). WPF call sites treat this as a cheap "is anyone
        // listening" probe, so no context means no listener rather than a throw.
        public static bool ListenerExists(AutomationEvents events)
        {
            try
            {
                return Microsoft.UI.Xaml.Automation.Peers.AutomationPeer.ListenerExists(
                    (Microsoft.UI.Xaml.Automation.Peers.AutomationEvents)events);
            }
            catch (Runtime.InteropServices.COMException)
            {
                return false;
            }
        }

        // WPF answers "no peer" for a null element; WinAppSDK's projections reject it
        // with E_INVALIDARG, so the null case is handled before crossing the boundary.
        public static new AutomationPeer? FromElement(UIElement element) =>
            element is null
                ? null
                : Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(element)
                    as AutomationPeer;

        public static new AutomationPeer? CreatePeerForElement(UIElement element) =>
            element is null
                ? null
                : Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(element)
                    as AutomationPeer;

        public void RaiseAutomationEvent(AutomationEvents events) =>
            base.RaiseAutomationEvent(
                (Microsoft.UI.Xaml.Automation.Peers.AutomationEvents)events);

        public void RaisePropertyChangedEvent(AutomationProperty property, object? oldValue, object? newValue)
        {
            if (property.UnoProperty is { } unoProperty)
            {
                base.RaisePropertyChangedEvent(unoProperty, oldValue, newValue);
            }
        }
    }

    /// <summary>
    /// WPF UIElementAutomationPeer shape: element-backed peer with an owner.
    /// </summary>
    public class UIElementAutomationPeer : AutomationPeer
    {
        public UIElementAutomationPeer(Microsoft.UI.Xaml.FrameworkElement owner)
            : base(owner)
        {
        }
    }
}