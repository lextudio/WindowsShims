namespace System.Windows.Automation.Peers
{
    public class AutomationPeer
    {
        public static bool ListenerExists(AutomationEvents events) => false;

        public virtual void RaiseAutomationEvent(AutomationEvents events)
        {
        }
    }
}
