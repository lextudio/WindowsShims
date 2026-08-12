using System.Linq;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace System.Windows.Automation.Peers;

public sealed class DataGridCellAutomationPeer : UIElementAutomationPeer, IValueProvider, ISelectionItemProvider
{
    public DataGridCellAutomationPeer(Controls.DataGridCell owner)
        : base(owner)
    {
    }

    public new Controls.DataGridCell Owner => (Controls.DataGridCell)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

    protected override string GetNameCore() => CellAutomationHelper.GetCellText(Owner);

    protected override object? GetPatternCore(PatternInterface patternInterface) =>
        patternInterface switch
        {
            PatternInterface.Value => this,
            PatternInterface.SelectionItem => this,
            PatternInterface.GridItem => this,
            _ => base.GetPatternCore(patternInterface),
        };

    // ── IValueProvider ──────────────────────────────────────────────────────

    public bool IsReadOnly => Owner.IsReadOnly;

    public string Value => CellAutomationHelper.GetCellText(Owner);

    public void SetValue(string value) =>
        throw new NotSupportedException("Setting the cell value through automation is not supported by this shim.");

    // ── ISelectionItemProvider ──────────────────────────────────────────────

    public bool IsSelected => Owner.IsSelected;

    public IRawElementProviderSimple? SelectionContainer =>
        Owner.DataGridOwner is { } grid ? ProviderFromPeer(AutomationPeer.FromElement(grid)) : null;

    public void AddToSelection() => Select();

    public void RemoveFromSelection()
    {
        if (Owner.DataGridOwner is { } grid && ReferenceEquals(grid.CurrentCell.Item, Owner.DataContext))
        {
            grid.CurrentCell = new System.Windows.Controls.DataGridCellInfo();
        }
        else
        {
            Select();
        }
    }

    public void Select()
    {
        if (Owner.DataGridOwner is { } grid && Owner.DataContext is { } item)
        {
            grid.CurrentCell = new System.Windows.Controls.DataGridCellInfo(item, Owner.Column, grid);
        }
    }

    // ── IGridItemProvider ───────────────────────────────────────────────────

    public int Row => Owner.DataGridOwner?.ItemContainerGenerator.IndexFromContainer(Owner) ?? -1;

    public int Column => Owner.Column?.DisplayIndex ?? -1;

    public int RowSpan => 1;

    public int ColumnSpan => 1;

    public IRawElementProviderSimple? ContainingGrid =>
        Owner.DataGridOwner is { } grid ? ProviderFromPeer(AutomationPeer.FromElement(grid)) : null;
}

/// <summary>Text resolution helpers shared by the DataGrid peer family.</summary>
internal static class CellAutomationHelper
{
    internal static string GetName(Microsoft.UI.Xaml.DependencyObject owner) => string.Empty;

    internal static string GetRowName(Controls.DataGridRow row) => string.Empty;

    internal static string GetCellText(Controls.DataGridCell cell) =>
        cell is null ? string.Empty : ScanText(cell);

    private static string ScanText(Microsoft.UI.Xaml.DependencyObject root)
    {
        if (root is Microsoft.UI.Xaml.Controls.TextBlock textBlock)
        {
            return textBlock.Text ?? string.Empty;
        }

        if (root is Microsoft.UI.Xaml.Controls.Panel panel)
        {
            foreach (var child in panel.Children)
            {
                var text = ScanText(child);
                if (text.Length > 0)
                {
                    return text;
                }
            }
        }

        if (root is Microsoft.UI.Xaml.Controls.ContentControl contentControl)
        {
            return ScanText(contentControl.Content as Microsoft.UI.Xaml.DependencyObject);
        }

        return string.Empty;
    }
}