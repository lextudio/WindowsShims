namespace System.Windows.Automation.Peers;

public sealed class DataGridDetailsPresenterAutomationPeer : AutomationPeer
{
    public DataGridDetailsPresenterAutomationPeer(Controls.Primitives.DataGridDetailsPresenter owner)
    {
        Owner = owner;
    }

    public Controls.Primitives.DataGridDetailsPresenter Owner { get; }
}
