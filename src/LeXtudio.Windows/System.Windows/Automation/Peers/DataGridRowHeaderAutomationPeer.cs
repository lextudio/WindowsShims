using Microsoft.UI.Xaml.Automation.Peers;

namespace System.Windows.Automation.Peers;

public sealed class DataGridRowHeaderAutomationPeer : UIElementAutomationPeer
{
    public DataGridRowHeaderAutomationPeer(Controls.Primitives.DataGridRowHeader owner)
        : base(owner)
    {
    }

    public new Controls.Primitives.DataGridRowHeader Owner => (Controls.Primitives.DataGridRowHeader)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;

    protected override string GetNameCore() => string.Empty;
}