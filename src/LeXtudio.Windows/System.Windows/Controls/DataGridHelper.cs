using System.Windows.Data;
using System.Windows.Controls.Primitives;

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

    internal static void OnColumnWidthChanged(IProvideDataGridColumn owner, DependencyPropertyChangedEventArgs e)
    {
        if (owner is DataGridColumnHeader header)
        {
            header.Width = header.Column?.DataGridOwner?.ShimColumnWidth(header.Column) ?? double.NaN;
        }
        else if (owner is DataGridCell cell)
        {
            cell.Width = cell.Column?.DataGridOwner?.ShimColumnWidth(cell.Column) ?? double.NaN;
        }
    }

    internal static void TransferProperty(DependencyObject d, DependencyProperty dp)
    {
        if (d is DataGridColumnHeader header)
        {
            if (dp == DataGridColumnHeader.ContentProperty)
            {
                header.Content = header.Column?.DataGridOwner?.HeaderContent(header.Column) ?? header.Column?.Header;
            }
            else if (dp == DataGridColumnHeader.StyleProperty)
            {
                header.ApplyShimColumnHeaderStyle();
            }
        }
        else if (d is DataGridRow row)
        {
            if (dp == DataGridRow.BackgroundProperty)
            {
                row.ApplyShimRowBackground();
            }
            else if (dp == DataGridRow.StyleProperty)
            {
                row.ApplyShimRowStyle();
            }
            else if (dp == DataGridRow.DetailsVisibilityProperty && row.DataGridOwner is { } owner)
            {
                row.DetailsVisibility = owner.RowDetailsVisibilityMode switch
                {
                    DataGridRowDetailsVisibilityMode.Visible => Visibility.Visible,
                    DataGridRowDetailsVisibilityMode.VisibleWhenSelected => row.IsSelected ? Visibility.Visible : Visibility.Collapsed,
                    _ => Visibility.Collapsed,
                };
            }
        }
        else if (d is DataGridCell cell)
        {
            if (dp == DataGridCell.IsReadOnlyProperty)
            {
                cell.IsReadOnly = cell.DataGridOwner?.IsCellEffectivelyReadOnly(cell.Column) ?? false;
            }
            else if (dp == FrameworkElement.StyleProperty)
            {
                cell.ApplyShimCellStyle();
            }
        }
    }

    internal static object? GetCoercedTransferPropertyValue(
        DependencyObject? baseObject,
        object? baseValue,
        DependencyProperty baseProperty,
        DependencyObject? parentObject,
        DependencyProperty parentProperty)
        => baseValue;

    // 7-arg overload used by style coercion (column + grid both contribute).
    internal static object? GetCoercedTransferPropertyValue(
        DependencyObject? baseObject,
        object? baseValue,
        DependencyProperty baseProperty,
        DependencyObject? firstParent,
        DependencyProperty firstParentProperty,
        DependencyObject? secondParent,
        DependencyProperty secondParentProperty)
        => baseValue;

    // WPF DataGridHelper.IsGridLineVisible — whether the given orientation of
    // grid lines is active on the owner DataGrid.
    internal static bool IsGridLineVisible(DataGrid? dataGrid, bool isHorizontal)
    {
        if (dataGrid == null) return false;
        var v = dataGrid.GridLinesVisibility;
        return isHorizontal
            ? v is DataGridGridLinesVisibility.Horizontal or DataGridGridLinesVisibility.All
            : v is DataGridGridLinesVisibility.Vertical   or DataGridGridLinesVisibility.All;
    }

    // WPF DataGridHelper.SubtractFromSize — shrinks a Size by a thickness along
    // one axis to reserve space for a grid line.
    internal static Size SubtractFromSize(Size size, double thickness, bool height)
        => height
            ? new Size(size.Width, Math.Max(0, size.Height - thickness))
            : new Size(Math.Max(0, size.Width - thickness), size.Height);

    // WPF DataGridHelper.GetFrozenClipForCell — returns a clipping geometry for
    // cells that overlap the frozen/scrolling boundary. Returns null (no clip)
    // in the shim because frozen-column clipping is not yet implemented.
    internal static Geometry? GetFrozenClipForCell(IProvideDataGridColumn cell) => null;

    // WPF DataGridHelper.BindingExpressionBelongsToElement — whether a binding
    // expression targets an element of type T. Returns false in the shim.
    internal static bool BindingExpressionBelongsToElement<T>(
        System.Windows.Data.BindingExpressionBase expr, T element)
        where T : DependencyObject
        => false;

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

    internal static bool ShouldNotifyRows(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.Rows) != 0;

    internal static bool ShouldNotifyRowHeaders(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.RowHeaders) != 0;

    internal static bool ShouldNotifyCells(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.Cells) != 0;

    internal static bool ShouldNotifyCellsPresenter(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.CellsPresenter) != 0;

    internal static bool ShouldNotifyDetailsPresenter(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.DetailsPresenter) != 0;

    internal static bool ShouldRefreshCellContent(DataGridNotificationTarget target)
        => (target & DataGridNotificationTarget.RefreshCellContent) != 0;

    internal static bool ShouldNotifyRowSubtree(DataGridNotificationTarget target)
        => (target & (DataGridNotificationTarget.Rows | DataGridNotificationTarget.RowHeaders |
                      DataGridNotificationTarget.CellsPresenter | DataGridNotificationTarget.Cells |
                      DataGridNotificationTarget.RefreshCellContent |
                      DataGridNotificationTarget.DetailsPresenter)) != 0;
}
