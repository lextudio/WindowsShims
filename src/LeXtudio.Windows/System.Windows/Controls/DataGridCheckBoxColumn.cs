using System.Reflection;
using WinUICheckBox = Microsoft.UI.Xaml.Controls.CheckBox;

namespace System.Windows.Controls;

public partial class DataGridCheckBoxColumn : DataGridBoundColumn
{
    private static Style? _defaultElementStyle;
    private static Style? _defaultEditingElementStyle;

    public static readonly DependencyProperty IsThreeStateProperty =
        DependencyProperty.Register(
            nameof(IsThreeState),
            typeof(bool),
            typeof(DataGridCheckBoxColumn),
            new FrameworkPropertyMetadata(false, (PropertyChangedCallback)NotifyPropertyChangeForRefreshContent));

    public static Style DefaultElementStyle
        => _defaultElementStyle ??= new Style(typeof(WinUICheckBox));

    public static Style DefaultEditingElementStyle
        => _defaultEditingElementStyle ??= new Style(typeof(WinUICheckBox));

    public bool IsThreeState
    {
        get => (bool)GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        => GenerateCheckBox(isEditing: false, cell);

    protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        => GenerateCheckBox(isEditing: true, cell);

    protected internal override void RefreshCellContent(FrameworkElement element, string propertyName)
    {
        if (element is DataGridCell { Content: WinUICheckBox checkBox } &&
            string.Equals(propertyName, nameof(IsThreeState), StringComparison.Ordinal))
        {
            checkBox.IsThreeState = IsThreeState;
            return;
        }

        base.RefreshCellContent(element, propertyName);
    }

    protected override object? PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
    {
        if (editingElement is WinUICheckBox checkBox)
        {
            checkBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
            return checkBox.IsChecked;
        }

        return false;
    }

    private WinUICheckBox GenerateCheckBox(bool isEditing, DataGridCell cell)
    {
        var checkBox = new WinUICheckBox { IsThreeState = IsThreeState };
        ApplyStyle(isEditing, defaultToElementStyle: true, checkBox);

        var item = cell.RowDataItem;
        var path = BindingPath;
        var prop = item is not null && path is { Length: > 0 } ? item.GetType().GetProperty(path) : null;

        if (item is null || prop is null)
        {
            // No resolvable source — fall back to the (display-only) binding.
            ApplyBinding(checkBox, WinUICheckBox.IsCheckedProperty);
            return checkBox;
        }

        checkBox.IsChecked = prop.GetValue(item) as bool?;

        // The checkbox edits in place; toggling writes back unless read-only.
        var readOnly = cell.DataGridOwner?.IsCellEffectivelyReadOnly(this) ?? false;
        var writable = !readOnly && prop.CanWrite;
        checkBox.IsEnabled = writable;
        if (writable)
        {
            checkBox.Checked += (_, _) => WriteBack(prop, item, true);
            checkBox.Unchecked += (_, _) => WriteBack(prop, item, false);
            checkBox.Indeterminate += (_, _) => WriteBack(prop, item, null);
        }

        return checkBox;
    }

    private static void WriteBack(PropertyInfo prop, object item, bool? value)
    {
        try
        {
            object? toSet = Nullable.GetUnderlyingType(prop.PropertyType) is not null
                ? value
                : value ?? false;
            prop.SetValue(item, toSet);
        }
        catch (Exception)
        {
            // Ignore write failures (type mismatch / setter throw).
        }
    }
}
