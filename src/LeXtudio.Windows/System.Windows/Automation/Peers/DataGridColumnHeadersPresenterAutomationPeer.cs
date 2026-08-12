using System.Linq;
using System.Windows.Controls.Primitives;
using Microsoft.UI.Xaml.Automation.Peers;

namespace System.Windows.Automation.Peers;

public sealed class DataGridColumnHeadersPresenterAutomationPeer : UIElementAutomationPeer
{
    public DataGridColumnHeadersPresenterAutomationPeer(DataGridColumnHeadersPresenter owner)
        : base(owner)
    {
    }

    public new DataGridColumnHeadersPresenter Owner => (DataGridColumnHeadersPresenter)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    internal System.Collections.Generic.IEnumerable<DataGridColumnHeaderAutomationPeer> RealizedColumnHeaderPeers() =>
        Owner.Items.OfType<DataGridColumnHeader>()
            .Select(header => FromElement(header))
            .OfType<DataGridColumnHeaderAutomationPeer>();
}