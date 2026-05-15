namespace System.Windows.Automation.Peers
{
    public sealed class HyperlinkAutomationPeer : AutomationPeer
    {
        private readonly object _owner;

        public HyperlinkAutomationPeer(object owner)
        {
            _owner = owner;
        }
    }
}
