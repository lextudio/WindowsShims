namespace System.Windows.Automation.Peers;

public sealed class DataGridCellAutomationPeer : AutomationPeer
{
    public DataGridCellAutomationPeer(Controls.DataGridCell owner)
    {
        Owner = owner;
    }

    public Controls.DataGridCell Owner { get; }
}
