using Microsoft.UI.Xaml.Automation.Peers;

namespace System.Windows.Automation.Peers;

public sealed class DataGridDetailsPresenterAutomationPeer : UIElementAutomationPeer
{
    public DataGridDetailsPresenterAutomationPeer(Controls.Primitives.DataGridDetailsPresenter owner)
        : base(owner)
    {
    }

    public new Controls.Primitives.DataGridDetailsPresenter Owner => (Controls.Primitives.DataGridDetailsPresenter)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
}