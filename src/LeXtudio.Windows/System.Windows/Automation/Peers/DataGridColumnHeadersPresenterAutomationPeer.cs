namespace System.Windows.Automation.Peers;

public sealed class DataGridColumnHeadersPresenterAutomationPeer : AutomationPeer
{
    public DataGridColumnHeadersPresenterAutomationPeer(Controls.Primitives.DataGridColumnHeadersPresenter owner)
    {
        Owner = owner;
    }

    public Controls.Primitives.DataGridColumnHeadersPresenter Owner { get; }
}
