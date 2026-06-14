namespace System.Windows.Automation.Peers;

public sealed class DataGridRowHeaderAutomationPeer : AutomationPeer
{
    public DataGridRowHeaderAutomationPeer(Controls.Primitives.DataGridRowHeader owner)
    {
        Owner = owner;
    }

    public Controls.Primitives.DataGridRowHeader Owner { get; }
}
