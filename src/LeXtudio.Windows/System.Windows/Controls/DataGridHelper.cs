using System.Windows.Data;

namespace System.Windows.Controls;

// Helper surface used by the linked upstream DataGrid column code and the Uno
// render path. Members the upstream files depend on (GetPathFromBinding,
// IsOneWay) keep their WPF behavior; the rest are lightweight shims for the
// notification-target plumbing the upstream DataGrid expects.
internal static class DataGridHelper
{
    public static bool IsOneWay(BindingBase? binding)
        => binding is Binding { Mode: BindingMode.OneWay or BindingMode.OneTime };

    public static string? GetPathFromBinding(Binding? binding)
        => binding?.Path?.Path;

    // Session 59: helpers the linked DataGridTextColumn/DataGridCheckBoxColumn
    // bodies call. SyncColumnProperty mirrors WPF exactly (copy the column's
    // value to the generated element, or clear it). FlowDirection caching is
    // RTL plumbing not needed by the shim, so it is a no-op.
    internal static void SyncColumnProperty(
        DependencyObject column, DependencyObject content,
        DependencyProperty contentProperty, DependencyProperty columnProperty)
    {
        if (IsDefaultValue(column, columnProperty))
        {
            content.ClearValue(contentProperty);
        }
        else
        {
            content.SetValue(contentProperty, column.GetValue(columnProperty));
        }
    }

    internal static void CacheFlowDirection(FrameworkElement? element, DataGridCell? cell) { }

    internal static void RestoreFlowDirection(FrameworkElement? element, DataGridCell? cell) { }

    private const char _escapeChar = '\u001b';

    public static bool HasNonEscapeCharacters(Input.TextCompositionEventArgs? textArgs)
    {
        if (textArgs is not null)
        {
            var text = textArgs.Text ?? string.Empty;
            for (int i = 0, count = text.Length; i < count; i++)
            {
                if (text[i] != _escapeChar)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsImeProcessed(KeyEventArgs? keyArgs)
        => keyArgs is not null && keyArgs.Key == Input.Key.ImeProcessed;

    internal static T? FindVisualParent<T>(UIElement element) where T : UIElement
    {
        DependencyObject? current = element;
        while (current is not null)
        {
            if (current is T target)
                return target;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    internal static bool IsDefaultValue(DependencyObject d, DependencyProperty dp)
        => d.ReadLocalValue(dp) == DependencyProperty.UnsetValue;

    internal static bool IsPropertyTransferEnabled(DependencyObject d, DependencyProperty dp) => true;

    internal static void TransferProperty(DependencyObject d, DependencyProperty dp) { }

    internal static object? GetCoercedTransferPropertyValue(
        DependencyObject? baseObject,
        object? baseValue,
        DependencyProperty baseProperty,
        DependencyObject? parentObject,
        DependencyProperty parentProperty)
        => baseValue;

    internal static double CoerceToMinMax(double value, double minValue, double maxValue)
    {
        if (double.IsNaN(value))
        {
            value = 0;
        }

        return Math.Max(minValue, Math.Min(maxValue, value));
    }

    internal static void UpdateTarget(FrameworkElement element) { }

    internal static void UpdateTarget(FrameworkElement element, DependencyProperty property) { }

    internal static bool ValidateWithoutUpdate(FrameworkElement element) => true;

    internal static double GetParentCellsPanelHorizontalOffset(DependencyObject element) => 0.0;
    internal static double GetParentCellsPanelHorizontalOffset(IProvideDataGridColumn element) => 0.0;

    internal static bool ShouldNotifyDataGrid(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.DataGrid) != 0;

    internal static bool ShouldNotifyColumns(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.Columns) != 0;

    internal static bool ShouldNotifyColumnCollection(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.ColumnCollection) != 0;

    internal static bool ShouldNotifyColumnHeaders(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.ColumnHeaders) != 0;

    internal static bool ShouldNotifyColumnHeadersPresenter(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.ColumnHeadersPresenter) != 0;

    internal static bool ShouldNotifyRowSubtree(DataGridNotificationTarget target)
        => (target & (DataGridNotificationTarget.Rows | DataGridNotificationTarget.RowHeaders |
                      DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.Cells |
                      DataGridNotificationTarget.DetailsPresenter)) != 0;
}
