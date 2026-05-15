namespace System.Windows.Automation.Peers;

public sealed class TableCellAutomationPeer : AutomationPeer
{
    public TableCellAutomationPeer(object owner)
    {
    }

    // Notify automation clients of span changes. Shim is a no-op.
    internal void OnColumnSpanChanged(int oldValue, int newValue) { }
    internal void OnRowSpanChanged(int oldValue, int newValue) { }
}
