using System.Windows.Data;
using System.Windows;

namespace System.Windows.Controls;

public abstract partial class DataGridBoundColumn : DataGridColumn
{
    private BindingBase? _binding;

    public virtual BindingBase? Binding
    {
        get => _binding;
        set
        {
            if (!ReferenceEquals(_binding, value))
            {
                var oldBinding = _binding;
                _binding = value;
                OnBindingChanged(oldBinding, _binding);
            }
        }
    }

    public static readonly DependencyProperty ElementStyleProperty =
        DependencyProperty.Register(
            nameof(ElementStyle),
            typeof(Style),
            typeof(DataGridBoundColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static readonly DependencyProperty EditingElementStyleProperty =
        DependencyProperty.Register(
            nameof(EditingElementStyle),
            typeof(Style),
            typeof(DataGridBoundColumn),
            new FrameworkPropertyMetadata(null, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public Style? ElementStyle
    {
        get => (Style?)GetValue(ElementStyleProperty);
        set => SetValue(ElementStyleProperty, value);
    }

    public Style? EditingElementStyle
    {
        get => (Style?)GetValue(EditingElementStyleProperty);
        set => SetValue(EditingElementStyleProperty, value);
    }

    public override BindingBase? ClipboardContentBinding
    {
        get => base.ClipboardContentBinding ?? Binding;
        set => base.ClipboardContentBinding = value;
    }

    internal void ApplyBinding(DependencyObject target, DependencyProperty property)
    {
        if (Binding is { } binding)
        {
            BindingOperations.SetBinding(target, property, binding);
        }
        else
        {
            BindingOperations.ClearBinding(target, property);
        }
    }

    internal void ApplyStyle(bool isEditing, bool defaultToElementStyle, FrameworkElement element)
    {
        var style = PickStyle(isEditing, defaultToElementStyle);
        if (style is not null)
        {
            element.Style = style;
        }
    }

    protected virtual void OnBindingChanged(BindingBase? oldBinding, BindingBase? newBinding)
        => NotifyPropertyChanged(nameof(Binding));

    protected override bool OnCoerceIsReadOnly(bool baseValue)
        => DataGridHelper.IsOneWay(Binding) || base.OnCoerceIsReadOnly(baseValue);

    protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
    {
        if (element is DataGridCell cell)
        {
            var isEditing = cell.IsEditing;
            if (string.Equals(propertyName, nameof(Binding), StringComparison.Ordinal) ||
                (string.Equals(propertyName, nameof(ElementStyle), StringComparison.Ordinal) && !isEditing) ||
                (string.Equals(propertyName, nameof(EditingElementStyle), StringComparison.Ordinal) && isEditing))
            {
                cell.BuildVisualTree();
                return;
            }
        }

        base.RefreshCellContent(element, propertyName);
    }

    private Style? PickStyle(bool isEditing, bool defaultToElementStyle)
    {
        var style = isEditing ? EditingElementStyle : ElementStyle;
        return style is null && isEditing && defaultToElementStyle ? ElementStyle : style;
    }
}

internal static class DataGridHelper
{
    public static bool IsOneWay(BindingBase? binding)
        => binding is Binding { Mode: BindingMode.OneWay or BindingMode.OneTime };

    public static string? GetPathFromBinding(Binding? binding)
        => binding?.Path?.Path;

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
