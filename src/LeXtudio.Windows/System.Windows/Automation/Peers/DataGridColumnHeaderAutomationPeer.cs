using System.Windows.Controls.Primitives;

namespace System.Windows.Automation.Peers;

public sealed class DataGridColumnHeaderAutomationPeer : AutomationPeer
{
    public DataGridColumnHeaderAutomationPeer(DataGridColumnHeader owner)
    {
        Owner = owner;
    }

    public DataGridColumnHeader Owner { get; }
}
