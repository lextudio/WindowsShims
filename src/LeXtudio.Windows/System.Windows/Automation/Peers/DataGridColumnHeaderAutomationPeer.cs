using System.Windows.Controls.Primitives;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace System.Windows.Automation.Peers;

public sealed class DataGridColumnHeaderAutomationPeer : UIElementAutomationPeer, IInvokeProvider
{
    public DataGridColumnHeaderAutomationPeer(DataGridColumnHeader owner)
        : base(owner)
    {
    }

    public new DataGridColumnHeader Owner => (DataGridColumnHeader)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;

    protected override string GetNameCore() =>
        Owner.Content?.ToString() ?? Owner.Column?.Header?.ToString() ?? string.Empty;

    protected override object? GetPatternCore(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);

    public void Invoke() => Owner.Invoke();
}