using System.Windows.Controls;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace System.Windows.Automation.Peers
{
    // DataGrid automation stubs: FromElement always returns null, so these
    // raise/find members are never reached at runtime; they exist for the
    // linked control root to compile.
    public class DataGridAutomationPeer : UIElementAutomationPeer
    {
        public DataGridAutomationPeer(DataGrid owner)
        {
        }

        internal void RaiseAutomationRowInvokeEvents(DataGridRow row)
        {
        }

        internal void RaiseAutomationCellInvokeEvents(DataGridColumn column, DataGridRow row)
        {
        }

        internal void RaiseAutomationCellSelectedEvent(SelectedCellsChangedEventArgs e)
        {
        }

        internal void RaiseAutomationSelectionEvents(SelectionChangedEventArgs e)
        {
        }

        internal AutomationPeer? FindOrCreateItemAutomationPeer(object? item) => null;
    }

    public class DataGridItemAutomationPeer : AutomationPeer
    {
        internal DataGridCellItemAutomationPeer? GetOrCreateCellItemPeer(DataGridColumn column) => null;
    }

    public class DataGridCellItemAutomationPeer : AutomationPeer
    {
    }
}
