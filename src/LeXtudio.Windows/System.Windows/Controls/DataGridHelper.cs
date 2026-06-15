using System.Windows.Data;
using System.Windows.Controls.Primitives;

namespace System.Windows.Controls;

// Local partial of DataGridHelper. The GridLines + Notification-Propagation
// regions now come from the linked upstream DataGridHelper_upstream.cs (session
// 102); this partial keeps the Uno shims for members still coupled to the WPF
// property-transfer/coercion engine, binding-expression internals, and RTL
// flow-direction caching (TransferProperty/OnColumnWidthChanged carry real
// Uno-specific render behavior, so they intentionally stay local).
internal static partial class DataGridHelper
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


    // WPF DataGridHelper.FindParent<T> — walks the visual ancestor chain looking
    // for an element of type T. Upstream DataGridRowHeader.ParentRow relies on it
    // to discover its owning DataGridRow. Mirrors FindVisualParent but starts from
    // the element's parent and accepts any DependencyObject start.
    internal static T? FindParent<T>(DependencyObject element) where T : class
    {
        DependencyObject? current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
        while (current is not null)
        {
            if (current is T target)
                return target;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }


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
        else if (d is DataGridRowHeader rowHeader)
        {
            var headerRow = rowHeader.EffectiveRow;
            if (dp == ContentControl.ContentProperty)
            {
                // Pull the per-row Header content down; fall back to the row's
                // glyph (set by DataGridRow.RefreshRowHeaderGlyph) when unset.
                if (headerRow?.Header is { } rowHeaderContent)
                {
                    rowHeader.Content = rowHeaderContent;
                }
            }
            else if (dp == FrameworkElement.WidthProperty)
            {
                rowHeader.Width = headerRow?.DataGridOwner?.RowHeaderShimWidth ?? double.NaN;
            }
            // Style / ContentTemplate / ContentTemplateSelector transfer is a
            // no-op in the shim: there is no WPF default row-header template to
            // coerce against, and styles are applied from XAML resources.
        }
        else if (d is DataGridDetailsPresenter details)
        {
            var detailsRow = details.EffectiveRow;
            // The upstream presenter is a ContentPresenter, so ContentTemplateProperty
            // here is WinUI ContentPresenter.ContentTemplateProperty (not ContentControl's).
            if (dp == Microsoft.UI.Xaml.Controls.ContentPresenter.ContentTemplateProperty)
            {
                details.ContentTemplate = detailsRow?.DetailsTemplate
                    ?? detailsRow?.DataGridOwner?.RowDetailsTemplate;
            }
            // ContentTemplateSelector transfer is a no-op: the shim resolves the
            // details template directly above.
        }
    }

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
}
